using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Qconcert.Models;
using System;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;
using Qconcert.ViewModels;
namespace Qconcert.Controllers
{
    public class PromotionController : Controller
    {
        private readonly TicketBoxDb1Context _context;

        public PromotionController(TicketBoxDb1Context context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.Events = await _context.Events.Where(e => e.IsApproved).ToListAsync();
            return View();
        }

        public IActionResult Success()
        {
            return View();
        }
        public async Task<IActionResult> Register(int id)
        {
            var eventItem = await _context.Events.FindAsync(id);
            if (eventItem == null)
            {
                return NotFound("Sự kiện không tồn tại.");
            }

            return View(eventItem); // Trả về model sự kiện cho view
        }

        
        [HttpPost]
        public async Task<IActionResult> Register(int eventId, string type, DateTime requestedStartDate, int durationInDays, IFormFile mediaFile)
        {
            var eventItem = await _context.Events.FindAsync(eventId);
            if (eventItem == null)
            {
                return NotFound("Sự kiện không tồn tại.");
            }
            /*// Kiểm tra xem sự kiện đã có gói quảng cáo nào chưa đang chờ hoặc đang hiển thị
            var existingPromotion = await _context.PromotionPackages
                .Where(p => p.EventId == eventId)
                .Where(p => p.Status == PromotionStatus.Pending || p.Status == PromotionStatus.Approved)
                .FirstOrDefaultAsync();

            if (existingPromotion != null)
            {
                ModelState.AddModelError(string.Empty, "Sự kiện này đã đăng ký gói quảng cáo và đang chờ hoặc đang hiển thị.");
                return View(eventItem);
            }*/
            // NEW: Cho phép mỗi sự kiện đăng ký tối đa 2 lần, bất kể trạng thái
            var promotionCount = await _context.PromotionPackages
                .Where(p => p.EventId == eventId)
                .CountAsync();

            if (promotionCount >= 2)
            {
                ModelState.AddModelError(string.Empty, "Sự kiện này đã đăng ký tối đa 2 lần quảng bá.");
                return View(eventItem);
            }


            if (mediaFile == null || mediaFile.Length == 0)
            {
                ModelState.AddModelError("mediaFile", "Vui lòng tải lên tệp media hợp lệ.");
                return View(eventItem);
            }

            // Parse type (Normal / VIP) safely (case-insensitive)
            if (!Enum.TryParse<PromotionType>(type, ignoreCase: true, out var parsedType))
            {
                ModelState.AddModelError("type", "Loại gói không hợp lệ.");
                return View(eventItem);
            }

            // Kiểm tra định dạng file theo loại gói
            var extension = Path.GetExtension(mediaFile.FileName).ToLowerInvariant();
            if (parsedType == PromotionType.VIP && extension != ".mp4")
            {
                ModelState.AddModelError("mediaFile", "Gói VIP chỉ chấp nhận file .mp4.");
                return View(eventItem);
            }
            else if (parsedType == PromotionType.Normal && extension != ".jpg" && extension != ".jpeg" && extension != ".png")
            {
                ModelState.AddModelError("mediaFile", "Gói Thường chỉ chấp nhận file .jpg hoặc .png.");
                return View(eventItem);
            }

            // Lưu tệp media
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
            Directory.CreateDirectory(uploadsFolder); // đảm bảo thư mục tồn tại

            var fileName = Guid.NewGuid().ToString() + extension;
            var filePath = Path.Combine(uploadsFolder, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await mediaFile.CopyToAsync(stream);
            }

            // Tính tổng tiền
            decimal pricePerDay = parsedType == PromotionType.VIP ? 200_000 : 100_000;
            decimal totalCost = pricePerDay * durationInDays;
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Kiểm tra số lượng sự kiện VIP đang hiển thị
            var requestedEndDate = requestedStartDate.AddDays(durationInDays);
            var activeVipCount = await _context.PromotionPackages
                .Where(p => p.Type == PromotionType.VIP && p.Status == PromotionStatus.Approved)
                .Where(p => p.ActualStartDate.HasValue && p.ActualStartDate.Value <= requestedEndDate)
                .Where(p => p.ActualStartDate.Value.AddDays(p.DurationInDays) >= requestedStartDate)
                .CountAsync();

            // Tạo gói khuyến mãi
            var promotionPackage = new PromotionPackage
            {
                EventId = eventId,
                UserId = userId,
                Type = parsedType,
                RequestedStartDate = requestedStartDate,
                DurationInDays = durationInDays,
                MediaPath = "/uploads/" + fileName,
                TotalCost = totalCost,
                TransactionId = Guid.NewGuid().ToString(),
                Status = activeVipCount < 4 ? PromotionStatus.Approved : PromotionStatus.Pending,
                IsInQueue = activeVipCount >= 4
            };

            if (promotionPackage.Status == PromotionStatus.Approved)
            {
                promotionPackage.ActualStartDate = requestedStartDate;

                // Nếu gói khuyến mãi phải chờ, bù ngày hiển thị
                if (promotionPackage.IsInQueue)
                {
                    var daysInQueue = (DateTime.Now - requestedStartDate).Days;
                    promotionPackage.DurationInDays += daysInQueue; // Bù số ngày đã chờ
                }
            }

            _context.PromotionPackages.Add(promotionPackage);
            await _context.SaveChangesAsync();

            // Redirect sang PayOSController để tạo link thanh toán
            return RedirectToAction("CreatePayment", "PayOS", new { promotionPackageId = promotionPackage.Id });
        }
        /* [HttpPost]
         public async Task<IActionResult> Register(int eventId, string type, DateTime requestedStartDate, int durationInDays, IFormFile mediaFile)
         {
             var eventItem = await _context.Events.FindAsync(eventId);
             if (eventItem == null)
             {
                 return NotFound("Sự kiện không tồn tại.");
             }

             if (mediaFile == null || mediaFile.Length == 0)
             {
                 ModelState.AddModelError("mediaFile", "Vui lòng tải lên tệp media hợp lệ.");
                 return View(eventItem);
             }

             // Parse type (Normal / VIP) safely (case-insensitive)
             if (!Enum.TryParse<PromotionType>(type, ignoreCase: true, out var parsedType))
             {
                 ModelState.AddModelError("type", "Loại gói không hợp lệ.");
                 return View(eventItem);
             }

             // Kiểm tra định dạng file theo loại gói
             var extension = Path.GetExtension(mediaFile.FileName).ToLowerInvariant();
             if (parsedType == PromotionType.VIP && extension != ".mp4")
             {
                 ModelState.AddModelError("mediaFile", "Gói VIP chỉ chấp nhận file .mp4.");
                 return View(eventItem);
             }
             else if (parsedType == PromotionType.Normal && extension != ".jpg" && extension != ".jpeg" && extension != ".png")
             {
                 ModelState.AddModelError("mediaFile", "Gói Thường chỉ chấp nhận file .jpg hoặc .png.");
                 return View(eventItem);
             }

             // Lưu tệp media
             var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
             Directory.CreateDirectory(uploadsFolder); // đảm bảo thư mục tồn tại

             var fileName = Guid.NewGuid().ToString() + extension;
             var filePath = Path.Combine(uploadsFolder, fileName);
             using (var stream = new FileStream(filePath, FileMode.Create))
             {
                 await mediaFile.CopyToAsync(stream);
             }

             // Tính tổng tiền
             decimal pricePerDay = parsedType == PromotionType.VIP ? 200_000 : 100_000;
             decimal totalCost = pricePerDay * durationInDays;
             var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
             // Tạo gói khuyến mãi
             var promotionPackage = new PromotionPackage
             {
                 EventId = eventId,
                 UserId = userId,
                 Type = parsedType,
                 RequestedStartDate = requestedStartDate,
                 DurationInDays = durationInDays,
                 MediaPath = "/uploads/" + fileName,
                 Status = PromotionStatus.Pending,
                 TransactionId = Guid.NewGuid().ToString(),
                 TotalCost = totalCost
             };

             _context.PromotionPackages.Add(promotionPackage);
             await _context.SaveChangesAsync();

             // Redirect sang PayOSController để tạo link thanh toán
             return RedirectToAction("CreatePayment", "PayOS", new { promotionPackageId = promotionPackage.Id });
         }*/
        private async Task CheckAndActivatePendingPromotions()
        {
            // Lấy danh sách các gói VIP đang chờ hiển thị
            var pendingPromotions = await _context.PromotionPackages
                .Where(p => p.Type == PromotionType.VIP && p.Status == PromotionStatus.Pending && p.IsInQueue)
                .OrderBy(p => p.CreatedAt) // Ưu tiên gói được tạo sớm nhất
                .ToListAsync();

            foreach (var promotion in pendingPromotions)
            {
                // Tính số lượng sự kiện VIP đang hiển thị
                var activeVipCount = await _context.PromotionPackages
                    .Where(p => p.Type == PromotionType.VIP && p.Status == PromotionStatus.Approved)
                    .Where(p => p.ActualStartDate.HasValue && p.ActualStartDate.Value <= DateTime.Now)
                    .Where(p => p.ActualStartDate.Value.AddDays(p.DurationInDays) >= DateTime.Now)
                    .CountAsync();

                // Nếu còn slot trống, kích hoạt sự kiện này
                if (activeVipCount < 4)
                {
                    promotion.Status = PromotionStatus.Approved;
                    promotion.ActualStartDate = DateTime.Now; // Bắt đầu hiển thị ngay
                    promotion.IsInQueue = false;

                    _context.PromotionPackages.Update(promotion);
                }
            }

            // ===== NORMAL =====
            var pendingNormalPromotions = await _context.PromotionPackages
                .Where(p => p.Type == PromotionType.Normal && p.Status == PromotionStatus.Pending && p.IsInQueue)
                .OrderBy(p => p.CreatedAt)
                .ToListAsync();

            foreach (var promotion in pendingNormalPromotions)
            {
                var activeNormalCount = await _context.PromotionPackages
                    .Where(p => p.Type == PromotionType.Normal && p.Status == PromotionStatus.Approved)
                    .Where(p => p.ActualStartDate.HasValue && p.ActualStartDate.Value <= DateTime.Now)
                    .Where(p => p.ActualStartDate.Value.AddDays(p.DurationInDays) >= DateTime.Now)
                    .CountAsync();

                if (activeNormalCount < 12)
                {
                    promotion.Status = PromotionStatus.Approved;
                    promotion.ActualStartDate = DateTime.Now; // Bắt đầu hiển thị ngay
                    promotion.IsInQueue = false;

                    _context.PromotionPackages.Update(promotion);
                }
            }

            // Gọi SaveChangesAsync một lần duy nhất sau khi cập nhật tất cả các khuyến mãi
            await _context.SaveChangesAsync();
        }
        public async Task<IActionResult> RevenueStatistics(string period = "day")
        {
            period = period?.ToLower() ?? "day"; // Gán giá trị mặc định nếu period là null

            DateTime startDate;
            DateTime endDate = DateTime.Now;

            switch (period)
            {
                case "day":
                    startDate = DateTime.Today;
                    break;
                case "week":
                    startDate = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek);
                    break;
                case "month":
                    startDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                    break;
                case "quarter":
                    int currentQuarter = (DateTime.Today.Month - 1) / 3 + 1;
                    startDate = new DateTime(DateTime.Today.Year, (currentQuarter - 1) * 3 + 1, 1);
                    break;
                case "year":
                    startDate = new DateTime(DateTime.Today.Year, 1, 1);
                    break;
                default:
                    startDate = DateTime.Today;
                    break;
            }

            // Tính toán doanh thu
            var vipRevenue = await _context.PromotionPackages
                .Where(p => p.Type == PromotionType.VIP && p.IsPaid && p.PaymentDate >= startDate && p.PaymentDate <= endDate)
                .SumAsync(p => p.TotalCost);

            var normalRevenue = await _context.PromotionPackages
                .Where(p => p.Type == PromotionType.Normal && p.IsPaid && p.PaymentDate >= startDate && p.PaymentDate <= endDate)
                .SumAsync(p => p.TotalCost);

            var totalRevenue = vipRevenue + normalRevenue;

            var revenueStatistics = new RevenueStatisticsViewModel
            {
                VipRevenue = vipRevenue,
                NormalRevenue = normalRevenue,
                TotalRevenue = totalRevenue,
                StartDate = startDate,
                EndDate = endDate,
                Period = period
            };

            return View(revenueStatistics);
        }
    }
}
