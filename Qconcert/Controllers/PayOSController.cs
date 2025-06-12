using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.UI.Services;
using Qconcert.Models;
using Qconcert.Services;
using Net.payOS;
using QRCoder;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Net.payOS.Types;
using iText.Kernel.Pdf;
using Microsoft.AspNetCore.Authorization;
using iText.IO.Image;
using iText.Layout.Properties;
using iText.Layout.Element;
using iText.Layout;
using MailKit.Security;
using MimeKit;
using MailKit.Net.Smtp;
using System.Globalization;
using System.Text;
using System.Security.Claims;
using MimeKit.Utils;

namespace Qconcert.Controllers
{
    public class PayOSController : Controller
    {
        private readonly TicketBoxDb1Context _context;
        private readonly IEmailSender _emailSender;

        private readonly VietQRService _vietQRService;
        private readonly PayOS _payOS;
        private readonly ILogger<PayOSController> _logger;

        public PayOSController(TicketBoxDb1Context context,
            IEmailSender emailSender,

            VietQRService vietQRService,
            IConfiguration configuration,
            ILogger<PayOSController> logger)
        {
            _context = context;
            _emailSender = emailSender;

            _vietQRService = vietQRService;
            _logger = logger;

            // Khởi tạo PayOS
            _payOS = new PayOS(
                "63257b6b-f334-4a2b-be90-ba0e48a7574f",  // ClientId
                "3478ac9b-4539-40d7-b3f1-79819c884caa",  // ApiKey
                "7b52c060c96c0e0cb4069e24a1058e08f9f0998e6030661ca3a40a454d026032"  // ChecksumKey
            );
        }

        /// <summary>
        /// Tạo yêu cầu thanh toán cho gói khuyến mãi
        /// </summary>
        [Authorize]
        [HttpGet] //  Đổi từ HttpPost sang HttpGet để tương thích với RedirectToAction
        public async Task<IActionResult> CreatePayment(int promotionPackageId)
        {

            try
            {
                var package = await _context.PromotionPackages.FindAsync(promotionPackageId);
                var userId = package.UserId;

                if (package == null)
                {
                    return Json(new { success = false, message = "Gói khuyến mãi không tồn tại." });
                }

                if (package.IsPaid)
                {
                    return Json(new { success = false, message = "Gói khuyến mãi đã được thanh toán." });
                }

                var totalCost = CalculatePromotionCost(package.Type, package.DurationInDays);
                var timestamp = DateTimeOffset.Now.ToUnixTimeSeconds();
                var orderCode = long.Parse($"{timestamp}{promotionPackageId % 1000:D3}");

                var items = new List<ItemData>
        {
            new ItemData(
                name: $"Gói {package.Type} - {package.DurationInDays} ngày",
                quantity: 1,
                price: (int)totalCost
            )
        };

                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                var paymentData = new PaymentData(
                    orderCode: orderCode,
                    amount: (int)totalCost,
                    description: $"PR-{package.Type}-{promotionPackageId}",
                    items: items,
                    returnUrl: $"{baseUrl}/PayOS/PaymentCallback?promotionPackageId={promotionPackageId}&success=true",
                    cancelUrl: $"{baseUrl}/PayOS/PaymentCallback?promotionPackageId={promotionPackageId}&success=false"
                );

                _logger.LogInformation($"Tạo thanh toán PayOS cho gói khuyến mãi {promotionPackageId} với mã {orderCode}");

                var response = await _payOS.createPaymentLink(paymentData);

                package.TotalCost = totalCost;
                package.TransactionId = orderCode.ToString();
                await _context.SaveChangesAsync();

                return Redirect(response.checkoutUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo thanh toán gói khuyến mãi");
                return Json(new { success = false, message = "Lỗi tạo thanh toán: " + ex.Message });
            }
        }

        private async Task<string> GetUserIdAsync(int promotionPackageId)
        {
            // Lấy UserId từ bảng PromotionPackages
            var package = await _context.PromotionPackages.FirstOrDefaultAsync(p => p.Id == promotionPackageId);
            if (package == null || string.IsNullOrEmpty(package.UserId))
            {
                _logger.LogWarning($"Không tìm thấy UserId cho gói khuyến mãi với ID: {promotionPackageId}");
                throw new Exception("Không thể xác định người dùng.");
            }
            return package.UserId;
        }

        /// <summary>
        /// Callback sau khi thanh toán thành công/thất bại
        /// </summary>
        public async Task<IActionResult> PaymentCallback(int promotionPackageId, bool success)
        {
            var package = await _context.PromotionPackages
                .Include(p => p.Event)
                .FirstOrDefaultAsync(p => p.Id == promotionPackageId);

            if (package == null) return NotFound();

            if (success)
            {
                package.IsPaid = true;
                package.PaymentDate = DateTime.Now;

                // Kiểm tra số lượng sự kiện VIP đang hiển thị
                var activeVipCount = await _context.PromotionPackages
                    .Where(p => p.Type == PromotionType.VIP && p.Status == PromotionStatus.Approved)
                    .Where(p => p.ActualStartDate.HasValue && p.ActualStartDate.Value <= DateTime.Now)
                    .Where(p => p.ActualStartDate.Value.AddDays(p.DurationInDays) >= DateTime.Now)
                    .CountAsync();

                if (activeVipCount < 4)
                {
                    // Nếu còn slot trống, kích hoạt ngay
                    package.Status = PromotionStatus.Approved;
                    package.ActualStartDate = DateTime.Now;
                    package.IsInQueue = false;
                }
                else
                {
                    // Nếu đã đủ 4 sự kiện, đặt trạng thái "chờ hiển thị"
                    package.Status = PromotionStatus.Pending;
                    package.IsInQueue = true;
                }

                await _context.SaveChangesAsync();

                // Gửi email xác nhận cho người dùng
                await SendConfirmationEmail(package);

                // Tạo thông báo cho người dùng
                var userId = await GetUserIdAsync(promotionPackageId);
                var userNotification = new Notification
                {
                    UserId = userId,
                    Message = $"Bạn đã thanh toán thành công gói {package.Type} cho sự kiện {package.Event?.Name}.",
                    EventId = package.Event?.Id,
                    CreatedAt = DateTime.Now
                };
                _context.Notifications.Add(userNotification);

                // Tạo thông báo cho admin
                var adminNotification = new Notification
                {
                    UserId = "1", // ID của admin (hoặc null nếu admin không có UserId cụ thể)
                    Message = $"Người dùng đã thanh toán gói {package.Type} cho sự kiện {package.Event?.Name}.",
                    EventId = package.Event?.Id,
                    CreatedAt = DateTime.Now
                };
                _context.Notifications.Add(adminNotification);

                await _context.SaveChangesAsync();
                return RedirectToAction("Success", "Promotion");
            }
            else
            {
                package.Status = PromotionStatus.Paid;
                await _context.SaveChangesAsync();
                return RedirectToAction("Failure", "Promotion");
            }
        }

        private string GetUserId()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogError("UserId không tồn tại trong claims.");
                throw new Exception("Không thể xác định người dùng.");
            }
            return userId;
        }

        /// <summary>
        /// Nhận webhook từ PayOS để cập nhật trạng thái thanh toán
        /// </summary>
        [HttpPost("payos-webhook")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> HandleWebhook([FromBody] dynamic webhookData)
        {
            try
            {
                long orderCode = webhookData.orderCode;
                string status = webhookData.status;

                var package = await _context.PromotionPackages
                    .FirstOrDefaultAsync(p => p.TransactionId == orderCode.ToString());

                if (package == null)
                    return NotFound("Không tìm thấy gói khuyến mãi tương ứng.");

                if (status == "PAID")
                {
                    package.IsPaid = true;
                    package.PaymentDate = DateTime.Now;

                    // Kiểm tra số lượng sự kiện VIP đang hiển thị
                    var activeVipCount = await _context.PromotionPackages
                        .Where(p => p.Type == PromotionType.VIP && p.Status == PromotionStatus.Approved)
                        .Where(p => p.ActualStartDate.HasValue && p.ActualStartDate.Value <= DateTime.Now)
                        .Where(p => p.ActualStartDate.Value.AddDays(p.DurationInDays) >= DateTime.Now)
                        .CountAsync();

                    if (activeVipCount < 4)
                    {
                        // Nếu còn slot trống, kích hoạt ngay
                        package.Status = PromotionStatus.Approved;
                        package.ActualStartDate = DateTime.Now;
                        package.IsInQueue = false;
                    }
                    else
                    {
                        // Nếu đã đủ 4 sự kiện, đặt trạng thái "chờ hiển thị"
                        package.Status = PromotionStatus.Pending;
                        package.IsInQueue = true;
                    }

                    await _context.SaveChangesAsync();
                    await SendConfirmationEmail(package);
                }

                return Ok(new { message = "Đã xử lý webhook thành công." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi xử lý webhook: {ex.Message}" });
            }
        }

        private decimal CalculatePromotionCost(PromotionType type, int durationInDays)
        {
            var dailyRate = type == PromotionType.VIP ? 200000 : 100000;
            return dailyRate * durationInDays;
        }

        private async Task SendConfirmationEmail(PromotionPackage package)
        {
            var eventName = package.Event?.Name ?? "Sự kiện không xác định";

            // Lấy email từ UserId đã lưu
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == package.UserId);
            var userEmail = user?.Email;

            if (string.IsNullOrEmpty(userEmail))
            {
                _logger.LogWarning("Không thể gửi email xác nhận: Email người dùng không tồn tại.");
                return;
            }

            var builder = new BodyBuilder();
            var html = new StringBuilder();

            html.Append("<h2>🎉 Thanh toán gói quảng bá thành công!</h2>");
            html.Append($"<p>Bạn đã thanh toán gói <strong>{package.Type}</strong> cho sự kiện <strong>{eventName}</strong>.</p>");

            html.Append("<h3>Thông tin gói quảng bá</h3>");
            html.Append("<table border='1' cellspacing='0' cellpadding='5'>");
            html.Append("<tr><th>Loại gói</th><th>Thời lượng</th><th>Ngày đăng ký</th><th>Giá tiền</th></tr>");
            html.Append("<tr>");
            html.Append($"<td>{package.Type}</td>");
            html.Append($"<td>{package.DurationInDays} ngày</td>");
            html.Append($"<td>{package.PaymentDate:dd/MM/yyyy HH:mm}</td>");
            html.Append($"<td>{package.TotalCost.ToString("N0", new CultureInfo("vi-VN"))} VNĐ</td>");
            html.Append("</tr>");
            html.Append("</table>");

            html.Append("<h3> Chính sách</h3>");
            html.Append("<p>Gói quảng bá sẽ được hiển thị ngay sau khi thanh toán thành công và duy trì liên tục trong thời gian đã đăng ký.</p>");
            html.Append("<p>Chi phí đã thanh toán sẽ không được hoàn lại, trừ khi có lỗi hệ thống hoặc sự kiện bị hủy do yếu tố khách quan.</p>");

            html.Append("<h3> Lưu ý</h3>");
            html.Append("<p>Bạn chịu trách nhiệm với nội dung quảng bá và tuân thủ chính sách cộng đồng của Qconcert.</p>");
            html.Append("<p>Hệ thống có quyền từ chối hiển thị nếu nội dung vi phạm quy định.</p>");

            html.Append("<h3>📞 Hỗ trợ khách hàng</h3>");
            html.Append("<p>Email: <a href='mailto:leminhquang2k4@gmail.com'>leminhquang2k4@gmail.com</a><br>");
            html.Append("Hotline: 2411.2004 (Thứ 2 - Chủ Nhật, 08:30 - 23:00)</p>");

            html.Append("<p style='font-size:smaller;color:gray'>Lưu ý: Đây là email tự động, vui lòng không phản hồi.</p>");

            builder.HtmlBody = html.ToString();

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Qconcert", "lmquang2004@gmail.com"));
            message.To.Add(MailboxAddress.Parse(userEmail));
            message.Subject = "✅ Xác nhận thanh toán gói quảng bá";
            message.Body = builder.ToMessageBody();

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync("smtp.gmail.com", 587, SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync("lmquang2004@gmail.com", "ecbw jcdo bfbb gegp"); // App password
            await smtp.SendAsync(message);
            await smtp.DisconnectAsync(true);
        }


        public async Task<IActionResult> Success(int promotionPackageId)
        {
            var promotionPackage = await _context.PromotionPackages.FindAsync(promotionPackageId);
            if (promotionPackage == null)
            {
                return NotFound("Gói khuyến mãi không tồn tại.");
            }

            ViewBag.PromotionPackage = promotionPackage; // Truyền gói khuyến mãi vào ViewBag
            return View();
        }
    }
}
