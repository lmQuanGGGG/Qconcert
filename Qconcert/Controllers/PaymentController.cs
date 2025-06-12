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

    public class PaymentController : Controller
    {
        private readonly TicketBoxDb1Context _context;
        private readonly IEmailSender _emailSender;

        private readonly VietQRService _vietQRService;
        private readonly PayOS _payOS;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(
            TicketBoxDb1Context context,
            IEmailSender emailSender,

            VietQRService vietQRService,
            IConfiguration configuration,
            ILogger<PaymentController> logger)
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

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CreatePaymentInfo(PaymentInfo paymentInfo)
        {


            try
            {
                // Kiểm tra xem PaymentInfo đã tồn tại chưa
                var existingPaymentInfo = await _context.PaymentInfos
                    .FirstOrDefaultAsync(p => p.EventId == paymentInfo.EventId);

                if (existingPaymentInfo != null)
                {
                    // Cập nhật thông tin nếu đã tồn tại
                    existingPaymentInfo.BankName = paymentInfo.BankName;
                    existingPaymentInfo.AccountNumber = paymentInfo.AccountNumber;
                    existingPaymentInfo.AccountHolder = paymentInfo.AccountHolder;
                    existingPaymentInfo.Branch = paymentInfo.Branch;

                    _context.PaymentInfos.Update(existingPaymentInfo);
                }
                else
                {
                    // Thêm mới nếu chưa tồn tại
                    _context.PaymentInfos.Add(paymentInfo);
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Thông tin thanh toán đã được lưu thành công!";
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lưu thông tin thanh toán");
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi lưu thông tin thanh toán.";
                return View(paymentInfo);
            }
        }
        [Authorize]
        [HttpGet]
        public IActionResult CreatePaymentInfo(int eventId)
        {
            var existingPaymentInfo = _context.PaymentInfos.FirstOrDefault(p => p.EventId == eventId);
            if (existingPaymentInfo != null)
            {
                return View(existingPaymentInfo);
            }
            var paymentInfo = new PaymentInfo { EventId = eventId };
            return View(paymentInfo);
        }


        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CreatePayOSPayment(int orderId)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Ticket)
                    .FirstOrDefaultAsync(o => o.Id == orderId);

                if (order == null)
                    return Json(new { success = false, message = "Không tìm thấy đơn hàng" });
                // Tạo orderCode duy nhất
                var timestamp = DateTimeOffset.Now.ToUnixTimeSeconds();
                var orderCode = long.Parse($"{timestamp}{orderId % 1000:D3}");

                var items = order.OrderDetails.Select(od => new Net.payOS.Types.ItemData(
                    name: od.Ticket.LoaiVe,
                    quantity: od.Quantity,
                    price: (int)od.Price
                )).ToList();

                var baseUrl = $"{Request.Scheme}://{Request.Host}";

                var paymentData = new PaymentData(
                    orderCode: orderCode,
                    amount: (int)order.TotalAmount,
                    description: $"Thanh toán vé - #{orderId}",
                    items: items,
                    returnUrl: $"{baseUrl}/Payment/PayOSSuccess",
                    cancelUrl: $"{baseUrl}/Payment/PayOSCancel"
                );

                _logger.LogInformation($"Creating PayOS payment for order {orderId} with code {orderCode}");
                var response = await _payOS.createPaymentLink(paymentData);

                // Lưu orderCode vào TransactionId
                order.PaymentMethod = "PayOS";
                order.TransactionId = orderCode.ToString();
                await _context.SaveChangesAsync();

                return Json(new { success = true, paymentUrl = response.checkoutUrl });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating PayOS payment");
                return Json(new { success = false, message = "Lỗi tạo thanh toán: " + ex.Message });
            }
        }

        [HttpPost("payos-webhook")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> PayOSWebhook([FromBody] dynamic webhookData)
        {
            Response.Headers.Add("Access-Control-Allow-Origin", "*"); // Cho phép tất cả các nguồn
            try
            {
                _logger.LogInformation("Received PayOS webhook");

                long orderCode = webhookData.orderCode;
                string status = webhookData.status;

                var order = await _context.Orders.FirstOrDefaultAsync(o => o.TransactionId == orderCode.ToString());
                if (order == null)
                    return NotFound("Order not found");

                if (status == "PAID")
                {
                    order.PaymentStatus = "Thanh toán thành công";
                    order.PaymentDate = DateTime.Now;

                    await _context.SaveChangesAsync();
                    await SendConfirmationEmail(order);

                    return Ok(new { message = "Payment processed successfully" });
                }

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing PayOS webhook");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        public async Task<IActionResult> PayOSSuccess(string orderCode)
        {
            if (string.IsNullOrEmpty(orderCode))
            {
                TempData["ErrorMessage"] = "Không có mã đơn hàng.";
                return RedirectToAction("Index", "Home");
            }

            // Chuyển orderCode về kiểu long
            if (!long.TryParse(orderCode, out long orderCodeLong))
            {
                TempData["ErrorMessage"] = "Mã đơn hàng không hợp lệ.";
                return RedirectToAction("Index", "Home");
            }

            // Load lại đơn hàng và các liên quan
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Ticket)
                .FirstOrDefaultAsync(o => o.TransactionId == orderCodeLong.ToString());

            if (order == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn hàng.";
                return RedirectToAction("Index", "Home");
            }

            // Nếu đơn hàng chưa được thanh toán, xử lý thanh toán
            if (order.PaymentStatus != "Thanh toán thành công")
            {
                order.PaymentStatus = "Thanh toán thành công";
                order.PaymentDate = DateTime.Now;

                // Giảm số lượng vé còn lại
                foreach (var detail in order.OrderDetails)
                {
                    var ticket = detail.Ticket;
                    if (ticket != null)
                    {
                        ticket.SoLuongConLai -= detail.Quantity;

                        // Kiểm tra nếu số lượng vé còn lại âm
                        if (ticket.SoLuongConLai < 0)
                        {
                            TempData["ErrorMessage"] = $"Vé {ticket.LoaiVe} đã hết. Đơn hàng không thể hoàn tất.";
                            return RedirectToAction("Index", "Home");
                        }


                        // Tạo mã QR cho vé với token bảo mật
                        var qrToken = Guid.NewGuid().ToString();
                        var qrCodeText = $"OrderId: {order.Id}, TicketId: {ticket.Id}, Quantity: {detail.Quantity}, Token: {qrToken}";
                        var qrCodeUrl = GenerateQrCode(qrCodeText, qrToken);

                        // Lưu URL của mã QR và token vào cơ sở dữ liệu
                        detail.QrCodeUrl = qrCodeUrl;
                        detail.QrCodeToken = qrToken;
                        detail.IsUsed = false; // Đặt trạng thái mã QR là chưa sử dụng
                    }
                }

                var userId = GetUserId();
                // Thêm thông báo cho người dùng
                var notification = new Notification
                {
                    UserId = userId,
                    Message = $"Đơn hàng #{order.Id} đã được thanh toán thành công. Vui lòng kiểm tra email để nhận vé.",
                    IsRead = false,
                    CreatedAt = DateTime.Now,
                    EventId = @order.OrderDetails.FirstOrDefault()?.Ticket.EventId // Lấy EventId từ vé đầu tiên trong đơn hàng
                };
                _context.Notifications.Add(notification);

                await _context.SaveChangesAsync();
                await SendConfirmationEmail(order);
            }

            TempData["SuccessMessage"] = "Thanh toán thành công! Vui lòng kiểm tra email của bạn.";
            return RedirectToAction("Success");
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

        public IActionResult PayOSCancel()
        {
            TempData["ErrorMessage"] = "Thanh toán đã bị hủy.";
            return RedirectToAction("Index", "Home");
        }


        private string GenerateQrCode(string text, string qrToken)
        {
            using var qrGenerator = new QRCodeGenerator();
            var qrData = qrGenerator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
            var qrCode = new PngByteQRCode(qrData);
            var qrCodeBytes = qrCode.GetGraphic(20);

            var fileName = $"qr_{qrToken}.png";
            var filePath = Path.Combine("wwwroot", "qrcodes", fileName);

            // Ensure the directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));

            // Save the QR code image to the file
            System.IO.File.WriteAllBytes(filePath, qrCodeBytes);

            // Return the URL of the QR code image
            return $"/qrcodes/{fileName}";
        }


        [Authorize]
        [HttpPost]
        public async Task SendConfirmationEmail(Order order)
        {
            var eventInfo = await _context.Events
    .FirstOrDefaultAsync(e => e.Id == order.OrderDetails.First().Ticket.EventId);

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Qconcert", "lmquang2004@gmail.com"));
            message.To.Add(MailboxAddress.Parse(order.Email));
            message.Subject = "🎉 Cảm ơn bạn đã đặt vé tại Qconcert!";

            var builder = new BodyBuilder();
            var html = new StringBuilder();

            html.Append("<h2>🎉 Cảm ơn bạn đã đặt vé tại Qconcert!</h2>");
            html.Append($"<p>Đơn hàng <strong>#{order.Id}</strong> đã được xác nhận thanh toán thành công.</p>");
            html.Append("<h3>🧾 Thông tin đơn hàng</h3>");
            html.Append($"<p><strong>Ngày đặt:</strong> {order.PaymentDate:dd/MM/yyyy HH:mm}<br>");

            html.Append($"<strong>Tổng tiền:</strong> {order.TotalAmount.ToString("N0", new CultureInfo("vi-VN"))} VNĐ</p>");

            html.Append("<h3>📅 Thông tin sự kiện</h3>");
            html.Append($"<p><strong>Sự kiện:</strong> {eventInfo.Name}<br>");
            html.Append($"<strong>Thời gian:</strong> {eventInfo.Date.ToString("dd/MM/yyyy HH:mm")}<br>");
            html.Append($"<strong>Địa điểm:</strong> {eventInfo.AddressDetail}, {eventInfo.Ward}, {eventInfo.District}, {eventInfo.Province}</p>");

            html.Append("<h3>🎫 Vé của bạn</h3>");
            html.Append("<table border='1' cellspacing='0' cellpadding='5'><tr><th>#</th><th>Loại vé</th><th>SL</th><th>Thành tiền</th><th>Mã QR</th></tr>");

            int i = 1;
            foreach (var od in order.OrderDetails)
            {
                var qrImagePath = Path.Combine("wwwroot", od.QrCodeUrl.TrimStart('/'));
                var qrBytes = await System.IO.File.ReadAllBytesAsync(qrImagePath);
                var cid = MimeUtils.GenerateMessageId();

                var imagePart = new MimePart("image", "png")
                {
                    Content = new MimeContent(new MemoryStream(qrBytes)),
                    ContentId = cid,
                    ContentDisposition = new ContentDisposition(ContentDisposition.Inline),
                    ContentTransferEncoding = ContentEncoding.Base64,
                    FileName = $"qr{i}.png"
                };

                builder.LinkedResources.Add(imagePart);

                html.Append($@"
        <tr>
            <td>{i}</td>
            <td>{od.Ticket.LoaiVe}</td>
            <td>{od.Quantity}</td>
            <td>{(od.Price * od.Quantity).ToString("N0", new CultureInfo("vi-VN"))} VNĐ</td>
            <td><img src=""cid:{cid}"" width='100' height='100'/></td>
        </tr>");
                i++;
            }


            html.Append("</table>");
            html.Append("<p>Vui lòng trình mã QR tại cổng check-in để vào sự kiện.</p>");
            html.Append("<p style='font-size:smaller;color:gray'>Lưu ý: Đây là email tự động, vui lòng không phản hồi.</p>");
            html.Append("<h3>Chính sách hoàn/hủy</h3>");
            html.Append("<p>Vé đã mua không được hoàn hoặc hủy trong mọi trường hợp, trừ khi sự kiện bị hủy do lý do bất khả kháng từ phía ban tổ chức.</p>");
            html.Append("<h3>Lưu ý</h3>");
            html.Append("<p>Người mua chịu trách nhiệm bảo mật thông tin mã vé.<br>");
            html.Append("Khi mua vé, tức là người mua đã đồng ý với các điều khoản và điều kiện trên.</p>");

            html.Append("<h3>📞 Hỗ trợ khách hàng</h3>");
            html.Append("<p>Email: <a href='mailto:leminhquang2k4@gmail.com'>leminhquang2k4@gmail.com</a><br>");
            html.Append("Hotline: 2411.2004 (Thứ 2 - Chủ Nhật, 08:30 - 23:00)</p>");

            builder.HtmlBody = html.ToString();
            message.Body = builder.ToMessageBody();

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync("smtp.gmail.com", 587, SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync("lmquang2004@gmail.com", "ecbw jcdo bfbb gegp"); // App password
            await smtp.SendAsync(message);
            await smtp.DisconnectAsync(true);
        }


        /*private async Task SendConfirmationEmail(Order order)
        {
            var subject = $"🎟 Xác nhận thanh toán đơn hàng #{order.Id} – Qconcert";

            var ticketDetailsHtml = string.Join("", order.OrderDetails.Select((od, index) => $@"
        <tr style='border-bottom:1px solid #eee;'>
            <td style='padding:10px;'>{index + 1}</td>
            <td style='padding:10px;'>{od.Ticket.LoaiVe}</td>
            <td style='padding:10px;'>{od.Quantity}</td>
            <td style='padding:10px;'>{(od.Price * od.Quantity):N0} VNĐ</td>
            <td style='padding:10px;'><img src='{od.QrCodeUrl}' alt='QR Code' width='100' height='100'/></td>
        </tr>
    "));

            var message = $@"
    <div style='font-family:Arial,sans-serif; max-width:600px; margin:auto; padding:20px; border:1px solid #ddd;'>
        <h2 style='color:#4CAF50;'>🎉 Cảm ơn bạn đã đặt vé tại Qconcert!</h2>
        <p>Đơn hàng <strong>#{order.Id}</strong> đã được xác nhận thanh toán thành công.</p>

        <h3>📄 Thông tin đơn hàng</h3>
        <p><strong>Ngày đặt:</strong> {order.OrderDate:dd/MM/yyyy HH:mm}</p>
        <p><strong>Tổng tiền:</strong> {order.TotalAmount:N0} VNĐ</p>

        <h3>🎫 Vé của bạn</h3>
        <table style='width:100%; border-collapse:collapse; text-align:left;'>
            <thead style='background-color:#f2f2f2;'>
                <tr>
                    <th style='padding:10px;'>#</th>
                    <th style='padding:10px;'>Loại vé</th>
                    <th style='padding:10px;'>SL</th>
                    <th style='padding:10px;'>Thành tiền</th>
                    <th style='padding:10px;'>Mã QR</th>
                </tr>
            </thead>
            <tbody>
                {ticketDetailsHtml}
            </tbody>
        </table>

        <p style='margin-top:20px;'>Vui lòng trình mã QR tại cổng check-in để vào sự kiện.</p>
        <p style='color:gray; font-size:13px;'>Lưu ý: Đây là email tự động, vui lòng không phản hồi.</p>
    </div>";

            await _emailSender.SendEmailAsync(order.Email, subject, message);
        }*/



        /*[Authorize]
        [HttpPost("api/verify-qr-code")]
        public async Task<IActionResult> VerifyQrCode([FromBody] VerifyQrCodeRequest request)
        {
            var qrCodeParts = request.QrCodeText.Split(", ");
            var tokenPart = qrCodeParts.FirstOrDefault(p => p.StartsWith("Token:"));
            if (tokenPart == null)
            {
                return NotFound(new { message = "Mã QR không hợp lệ" });
            }

            var token = tokenPart.Split(": ")[1];
            var orderDetail = await _context.OrderDetails
                .Include(od => od.Order)
                .FirstOrDefaultAsync(od => od.QrCodeToken == token);

            if (orderDetail == null)
            {
                return NotFound(new { message = "Mã QR không hợp lệ" });
            }

            if (orderDetail.IsUsed)
            {
                return BadRequest(new { message = "Mã QR đã được sử dụng" });
            }

            // Đánh dấu mã QR là đã sử dụng
            orderDetail.IsUsed = true;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Mã QR hợp lệ. Mời vào!!" });
        }*/

        public class VerifyQrCodeRequest
        {
            public string QrCodeText { get; set; }
        }

        [Authorize(Roles = "Employee")]
        public IActionResult ScanQrCode()
        {
            return View();
        }


        public IActionResult Success()
        {
            return View();
        }
    }
}
