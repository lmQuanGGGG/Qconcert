using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Qconcert.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Qconcert.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Net.Sockets;
using Qconcert.ViewModels;

namespace Qconcert.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly TicketBoxDb1Context _context;
        private readonly TicketService _ticketService;

        public HomeController(ILogger<HomeController> logger, TicketBoxDb1Context context, TicketService ticketService)
        {
            _logger = logger;
            _context = context;
            _ticketService = ticketService;
        }

        public async Task<IActionResult> Search(string query, string category)
        {
            // Lấy danh sách thể loại cho ViewBag
            var categories = await _context.Categories.ToListAsync();
            ViewBag.Categories = categories;

            // Lấy danh sách sự kiện kèm Category và Tickets
            var events = _context.Events
                .Include(e => e.Category)
                .Include(e => e.Tickets)
                .Where(e => e.IsApproved)
                .AsQueryable();

            // Nếu có từ khóa tìm kiếm
            if (!string.IsNullOrEmpty(query))
            {
                events = events.Where(e =>
                    (e.Name != null && e.Name.Contains(query)) ||
                    (e.Description != null && e.Description.Contains(query)) ||
                    (e.Category != null && e.Category.Name.Contains(query)));
            }

            // Nếu có lọc theo thể loại
            if (!string.IsNullOrEmpty(category))
            {
                events = events.Where(e => e.Category.Name == category);
            }

            var eventList = await events.ToListAsync();

            // Tính giá vé thấp nhất
            foreach (var @event in eventList)
            {
                var lowestPrice = (@event.Tickets != null && @event.Tickets.Any())
                    ? @event.Tickets.Min(t => t.Price)
                    : 0;

                ViewData[$"LowestPrice_{@event.Id}"] = lowestPrice;
            }

            return View(eventList);
        }

        
        public async Task<IActionResult> Index()
        {
            // Kiểm tra và kích hoạt các sự kiện VIP "chờ hiển thị"
            await CheckAndActivatePendingPromotions();

            // Lấy danh sách gói khuyến mãi đã thanh toán
            var promotionPackages = await _context.PromotionPackages
                .Include(p => p.Event)
                .ThenInclude(e => e.Category)
                .Include(p => p.Event.Tickets)
                .Where(p => p.IsPaid) // Chỉ lấy gói đã thanh toán
                .ToListAsync();

            // Sự kiện VIP
            var vipEvents = promotionPackages
                .Where(p => p.Type == PromotionType.VIP && p.Status == PromotionStatus.Approved)
                .Where(p => p.ActualStartDate.HasValue &&
                            p.ActualStartDate.Value <= DateTime.Now &&
                            p.ActualStartDate.Value.AddDays(p.DurationInDays) >= DateTime.Now) // Chỉ lấy gói đang hiển thị
                .OrderBy(p => p.ActualStartDate) // Ưu tiên gói được hiển thị sớm nhất
                .Take(4) // Giới hạn 4 sự kiện
                .Select(p => new VipPromotionViewModel
                {
                    Event = p.Event,
                    MediaPath = p.MediaPath
                })
                .ToList();

            // Sự kiện thường
            var regularEvents = promotionPackages
                .Where(p => p.Type == PromotionType.Normal && p.Status == PromotionStatus.Approved)
                .Select(p => new RegularPromotionViewModel
                {
                    Event = p.Event,
                    MediaPath = p.MediaPath
                })
                .ToList();

            // Các sự kiện không có gói khuyến mãi
            var allEvents = await _context.Events
                .Include(e => e.Category)
                .Include(e => e.Tickets)
                .Where(e => e.IsApproved) // Chỉ lấy sự kiện đã duyệt
                .ToListAsync();

            var categorizedEvents = allEvents
                .GroupBy(e => e.Category)
                .ToList();

            // Tạo ViewModel
            var viewModel = new HomeIndexViewModel
            {
                VipEvents = vipEvents,
                RegularEvents = regularEvents,
                CategorizedEvents = categorizedEvents
            };

            return View(viewModel);
        }

        public async Task CheckAndActivatePendingPromotions()
        {
            // ===== CẬP NHẬT GÓI HẾT HẠN (VIP + Normal) =====
            var expiredPromotions = await _context.PromotionPackages
                .Where(p => p.Status == PromotionStatus.Approved)
                .Where(p => p.ActualStartDate.HasValue)
                .Where(p => p.ActualStartDate.Value.AddDays(p.DurationInDays) < DateTime.Now)
                .ToListAsync();

            foreach (var expired in expiredPromotions)
            {
                expired.Status = PromotionStatus.Expired;
                _context.PromotionPackages.Update(expired);
            }

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

        [Authorize]
        public async Task<IActionResult> TicketHistory()
        {
            var userId = User.FindFirstValue(ClaimTypes.Email);
            if (userId == null)
            {
                return Unauthorized();
            }

            // Lấy danh sách đơn hàng kèm chi tiết vé và sự kiện
            var orders = await _context.Orders
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Ticket)
                        .ThenInclude(t => t.Event)
                .ToListAsync();

            return View(orders);
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




        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var @event = await _context.Events
                .Include(e => e.Category)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (@event == null) return NotFound();
            await _context.Events
        .Include(e => e.Tickets) // Bao gồm danh sách vé
        .FirstOrDefaultAsync(e => e.Id == id);

            if (@event == null)
            {
                return NotFound();
            }

            // Tính giá vé thấp nhất
            ViewData["LowestPrice"] = @event.Tickets?.Min(t => t.Price) ?? 0;

            return View(@event);
        }

        public async Task<IActionResult> SearchFiltered(string category, string dateFilter, string location)
        {
            var events = _context.Events
                .Include(e => e.Category)
                .Include(e => e.Tickets)
                .Where(e => e.IsApproved);

            if (!string.IsNullOrEmpty(category))
            {
                events = events.Where(e => e.Category.Name == category);
            }

            if (!string.IsNullOrEmpty(dateFilter))
            {
                DateTime today = DateTime.Today;

                if (dateFilter == "today")
                {
                    events = events.Where(e => e.Date.Date == today);
                }
                else if (dateFilter == "this-week")
                {
                    var startOfWeek = today.AddDays(-(int)today.DayOfWeek + (int)DayOfWeek.Monday);
                    var endOfWeek = startOfWeek.AddDays(6);
                    events = events.Where(e => e.Date.Date >= startOfWeek && e.Date.Date <= endOfWeek);
                }
                else if (dateFilter.StartsWith("custom:"))
                {
                    var parts = dateFilter.Substring(7).Split(',');
                    if (DateTime.TryParse(parts[0], out DateTime startDate) &&
                        DateTime.TryParse(parts[1], out DateTime endDate))
                    {
                        events = events.Where(e => e.Date.Date >= startDate.Date && e.Date.Date <= endDate.Date);
                    }
                }
            }

            if (!string.IsNullOrEmpty(location))
            {
                events = events.Where(e => e.Province.Contains(location));
            }

            var filteredEvents = await events.ToListAsync();

            foreach (var @event in filteredEvents)
            {
                ViewData[$"LowestPrice_{@event.Id}"] = @event.Tickets?.Min(t => t.Price) ?? 0;
            }

            return PartialView("_EventListPartial", filteredEvents);
        }



        [Authorize]
        public IActionResult Create()
        {
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name").ToList();
            return View();
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Description,EventInfo,Date,CategoryId,Capacity,Province,District,Ward,AddressDetail,Image9x16,Image16x9,OrganizerName,OrganizerInfo,OrganizerLogo")] Event @event, IFormFile Image9x16, IFormFile Image16x9, IFormFile OrganizerLogo, int numberOfTickets, decimal ticketPrice, DateTime startTime, DateTime endTime, string ticketType)
        {
            
            // Lấy ID của người dùng hiện tại
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Unauthorized();
            }
          

            @event.CreatedBy = userId; // Gán CreatedBy cho người tạo
            @event.IsApproved = false; // Mặc định chưa được duyệt

            // Xử lý ảnh nếu có
            if (Image9x16 != null)
            {
                using var ms = new MemoryStream();
                await Image9x16.CopyToAsync(ms);
                @event.Image9x16 = ms.ToArray();
            }

            if (Image16x9 != null)
            {
                using var ms = new MemoryStream();
                await Image16x9.CopyToAsync(ms);
                @event.Image16x9 = ms.ToArray();
            }

            if (OrganizerLogo != null)
            {
                using var ms = new MemoryStream();
                await OrganizerLogo.CopyToAsync(ms);
                @event.OrganizerLogo = ms.ToArray();
            }
            _context.Add(@event);
            await _context.SaveChangesAsync();
            // Tạo thông báo cho Admin
            var userNotification = new Notification
            {
                UserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                Message = $"Người dùng vừa tạo sự kiện '{@event.Name}' và đang chờ duyệt.",
                EventId = @event.Id
            };
            _context.Notifications.Add(userNotification);
            // Tạo thông báo cho Admin
            // Tạo thông báo cho Admin khi người dùng tạo sự kiện
            var adminNotification = new Notification
            {
                Message = $"Người dùng vừa tạo sự kiện '{@event.Name}' và đang chờ duyệt.",
                EventId = @event.Id,
                UserId = "1"
            };
            _context.Notifications.Add(adminNotification);
            await _context.SaveChangesAsync();
            // Chuyển hướng đến trang thêm vé
            return RedirectToAction("Create", "Ticket", new { eventId = @event.Id, numberOfTickets, ticketPrice, startTime, endTime, ticketType });
        }

        [Authorize]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var @event = await _context.Events.FindAsync(id);
            if (@event == null) return NotFound();

            // Kiểm tra quyền chỉnh sửa: Chỉ người tạo sự kiện mới được sửa
            if (!User.IsInRole("Admin") && @event.CreatedBy != User.FindFirstValue(ClaimTypes.NameIdentifier))
            {
                return Forbid(); // Trả về lỗi 403 nếu không có quyền
            }

            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", @event.CategoryId);
            return View(@event);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,EventInfo,Date,CategoryId,Capacity,Province,District,Ward,AddressDetail,OrganizerName,OrganizerInfo")] Event @event, IFormFile? Image9x16, IFormFile? Image16x9, IFormFile? OrganizerLogo)
        {
            if (id != @event.Id) return NotFound();

            var existingEvent = await _context.Events.FindAsync(id);
            if (existingEvent == null) return NotFound();

            // Kiểm tra quyền chỉnh sửa
            if (!User.IsInRole("Admin") && existingEvent.CreatedBy != User.FindFirstValue(ClaimTypes.NameIdentifier))
            {
                return Forbid();
            }

            // Cập nhật thông tin sự kiện
            existingEvent.Name = @event.Name;
            existingEvent.Description = @event.Description;
            existingEvent.EventInfo = @event.EventInfo;
            existingEvent.Date = @event.Date;
            existingEvent.CategoryId = @event.CategoryId;
            existingEvent.Capacity = @event.Capacity;
            existingEvent.Province = @event.Province;
            existingEvent.District = @event.District;
            existingEvent.Ward = @event.Ward;
            existingEvent.AddressDetail = @event.AddressDetail;
            existingEvent.OrganizerName = @event.OrganizerName;
            existingEvent.OrganizerInfo = @event.OrganizerInfo;

            // Xử lý ảnh: Nếu có ảnh mới thì cập nhật, nếu không thì giữ ảnh cũ
            if (Image9x16 != null)
            {
                using (var ms = new MemoryStream())
                {
                    await Image9x16.CopyToAsync(ms);
                    existingEvent.Image9x16 = ms.ToArray();
                }
            }

            if (Image16x9 != null)
            {
                using (var ms = new MemoryStream())
                {
                    await Image16x9.CopyToAsync(ms);
                    existingEvent.Image16x9 = ms.ToArray();
                }
            }

            if (OrganizerLogo != null)
            {
                using (var ms = new MemoryStream())
                {
                    await OrganizerLogo.CopyToAsync(ms);
                    existingEvent.OrganizerLogo = ms.ToArray();
                }
            }

            try
            {
                _context.Update(existingEvent);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Cập nhật sự kiện thành công!";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Events.Any(e => e.Id == id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return RedirectToAction("Create", "Ticket", new { eventId = @event.Id });
        }


        [Authorize]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var @event = await _context.Events.Include(e => e.Category).FirstOrDefaultAsync(m => m.Id == id);
            if (@event == null) return NotFound();

            // Kiểm tra quyền chỉnh sửa: Chỉ người tạo sự kiện mới được sửa
            if (!User.IsInRole("Admin") && @event.CreatedBy != User.FindFirstValue(ClaimTypes.NameIdentifier))
            {
                return Forbid(); // Trả về lỗi 403 nếu không có quyền
            }

            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", @event.CategoryId);
            return View(@event);
        }

        [Authorize]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var @event = await _context.Events.FindAsync(id);
            if (@event != null)
            {
                _context.Events.Remove(@event);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        [Authorize]
        public IActionResult Dashboard()
        {
            // Lấy ID của người dùng hiện tại
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Unauthorized(); // Trả về lỗi 401 nếu không tìm thấy ID người dùng
            }

            // Lọc sự kiện theo người dùng hiện tại
            var events = _context.Events
                                 .Include(e => e.Category)
                                 .Where(e => e.CreatedBy == userId) // Chỉ lấy sự kiện do người dùng này tạo
                                 .Select(e => new
                                 {
                                     e.Id,
                                     e.Name,
                                     e.Image16x9,
                                     e.Date,
                                     Status = e.IsApproved ? "Đã duyệt" : "Chờ duyệt"
                                 })
                                 .ToList();

            return View(events);
        }


    }
}
