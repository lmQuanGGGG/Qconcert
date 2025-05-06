using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Qconcert.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Qconcert.Services;
using System.Net.Sockets;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Qconcert.ViewModels;
using Microsoft.AspNetCore.Identity;

namespace Qconcert.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class AdminHomeController : Controller
    {
        private readonly ILogger<AdminHomeController> _logger;
        private readonly TicketBoxDb1Context _context;
        private readonly TicketService _ticketService;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminHomeController(
            ILogger<AdminHomeController> logger,
            TicketBoxDb1Context context,
            TicketService ticketService,
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _logger = logger;
            _context = context;
            _ticketService = ticketService;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<IActionResult> Search(string query)
        {
            var events = await _context.Events
                .Include(e => e.Category)
                .Where(e =>
                    (e.Name != null && e.Name.Contains(query)) ||
                    (e.Description != null && e.Description.Contains(query)))
                .ToListAsync();
            return View(events);
        }

        // Hiển thị danh sách các sự kiện chờ duyệt
        public async Task<IActionResult> PendingEvents()
        {
            var pendingEvents = await _context.Events
                                              .Include(e => e.Category)
                                              .Where(e => !e.IsApproved)
                                              .ToListAsync();
            return View(pendingEvents);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveEvent(int id)
        {
            // Nếu có Query Filter thì dùng IgnoreQueryFilters
            var eventToApprove = await _context.Events
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(e => e.Id == id);
            if (eventToApprove == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy sự kiện.";
                return RedirectToAction("PendingEvents");
            }

            eventToApprove.IsApproved = true;
            await _context.SaveChangesAsync();
            // Tạo thông báo cho người dùng
            var userNotification = new Notification
            {
                Message = $"Sự kiện '{@eventToApprove.Name}' của bạn đã được duyệt.",
                UserId = eventToApprove.CreatedBy, // ID người tạo sự kiện
                EventId = eventToApprove.Id
            };
            _context.Notifications.Add(userNotification);

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Sự kiện đã được duyệt.";
            return RedirectToAction("PendingEvents");
        }

        [HttpGet]
        public IActionResult RegisterEmployee()
        {
            return View();
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

        // GET: Home/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                TempData["ErrorMessage"] = "ID sự kiện không hợp lệ.";
                return RedirectToAction("PendingEvents");
            }

            var @event = await _context.Events
                .Include(e => e.Category) // Bao gồm danh mục
                .Include(e => e.Tickets) // Bao gồm danh sách vé
                .FirstOrDefaultAsync(e => e.Id == id);

            if (@event == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy sự kiện.";
                return RedirectToAction("PendingEvents");
            }

            // Tính giá vé thấp nhất
            ViewData["LowestPrice"] = @event.Tickets?.Min(t => t.Price) ?? 0;

            return View(@event);
        }

        public async Task<IActionResult> Index(string period = "day")
        {
            DateTime startDate;
            DateTime endDate = DateTime.Now;

            // Xác định khoảng thời gian dựa trên tham số "period"
            switch (period.ToLower())
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

            // Tính doanh thu từ gói VIP
            var vipRevenue = await _context.PromotionPackages
                .Where(p => p.Type == PromotionType.VIP && p.IsPaid && p.PaymentDate >= startDate && p.PaymentDate <= endDate)
                .SumAsync(p => p.TotalCost);

            // Tính doanh thu từ gói Thường
            var normalRevenue = await _context.PromotionPackages
                .Where(p => p.Type == PromotionType.Normal && p.IsPaid && p.PaymentDate >= startDate && p.PaymentDate <= endDate)
                .SumAsync(p => p.TotalCost);

            // Tính tổng doanh thu
            var totalRevenue = vipRevenue + normalRevenue;

            // Tạo ViewModel để truyền dữ liệu sang view
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DisableEmployee(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                TempData["ErrorMessage"] = "ID nhân viên không hợp lệ.";
                return RedirectToAction("EmployeeList");
            }

            var employee = await _userManager.FindByIdAsync(id);
            if (employee == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy tài khoản nhân viên.";
                return RedirectToAction("EmployeeList");
            }

            // Vô hiệu hóa tài khoản nhân viên
            employee.LockoutEnd = DateTimeOffset.Now.AddYears(10); // Vô hiệu hóa trong 100 năm
            var result = await _userManager.UpdateAsync(employee);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Vô hiệu hóa tài khoản nhân viên thành công.";
            }
            else
            {
                TempData["ErrorMessage"] = "Vô hiệu hóa tài khoản nhân viên không thành công.";
            }

            return RedirectToAction("EmployeeList");
        }
        public async Task<IActionResult> EmployeeList()
        {
            var employees = await _userManager.GetUsersInRoleAsync("Employee");
            return View(employees);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnableEmployee(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                TempData["ErrorMessage"] = "ID nhân viên không hợp lệ.";
                return RedirectToAction("EmployeeList");
            }

            var employee = await _userManager.FindByIdAsync(id);
            if (employee == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy tài khoản nhân viên.";
                return RedirectToAction("EmployeeList");
            }

            // Mở khóa tài khoản nhân viên
            employee.LockoutEnd = null; // Xóa trạng thái vô hiệu hóa
            var result = await _userManager.UpdateAsync(employee);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Mở khóa tài khoản nhân viên thành công.";
            }
            else
            {
                TempData["ErrorMessage"] = "Mở khóa tài khoản nhân viên không thành công.";
            }

            return RedirectToAction("EmployeeList");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEmployee(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                TempData["ErrorMessage"] = "ID nhân viên không hợp lệ.";
                return RedirectToAction("EmployeeList");
            }

            var employee = await _userManager.FindByIdAsync(id);
            if (employee == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy tài khoản nhân viên.";
                return RedirectToAction("EmployeeList");
            }

            // Xóa tài khoản nhân viên
            var result = await _userManager.DeleteAsync(employee);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Xóa tài khoản nhân viên thành công.";
            }
            else
            {
                TempData["ErrorMessage"] = "Xóa tài khoản nhân viên không thành công.";
            }

            return RedirectToAction("EmployeeList");
        }

        [HttpGet]
        public async Task<IActionResult> SearchEmployee(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                TempData["ErrorMessage"] = "Vui lòng nhập email để tìm kiếm.";
                return RedirectToAction("EmployeeList");
            }

            var users = await _userManager.Users
                .Where(u => u.Email.Contains(email))
                .ToListAsync();

            var employees = new List<IdentityUser>();
            foreach (var user in users)
            {
                if (await _userManager.IsInRoleAsync(user, "Employee"))
                {
                    employees.Add(user);
                }
            }

            if (!employees.Any())
            {
                TempData["ErrorMessage"] = "Không tìm thấy nhân viên với email đã nhập.";
                return RedirectToAction("EmployeeList");
            }

            return View("EmployeeList", employees);
        }



        // GET: Home/Create
        public IActionResult Create()
        {
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name").ToList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Description,EventInfo,Date,CategoryId,Capacity,Province,District,Ward,AddressDetail,Image9x16,Image16x9,OrganizerName,OrganizerInfo,OrganizerLogo")] Event @event, IFormFile Image9x16, IFormFile Image16x9, IFormFile OrganizerLogo, int numberOfTickets, decimal ticketPrice, DateTime startTime, DateTime endTime, string ticketType)
        {
            try
            {
                if (Image9x16 != null && Image9x16.Length > 0)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        await Image9x16.CopyToAsync(memoryStream);
                        @event.Image9x16 = memoryStream.ToArray();
                    }
                }

                if (Image16x9 != null && Image16x9.Length > 0)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        await Image16x9.CopyToAsync(memoryStream);
                        @event.Image16x9 = memoryStream.ToArray();
                    }
                }

                if (OrganizerLogo != null && OrganizerLogo.Length > 0)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        await OrganizerLogo.CopyToAsync(memoryStream);
                        @event.OrganizerLogo = memoryStream.ToArray();
                    }
                }

                _context.Add(@event);
                await _context.SaveChangesAsync();

                // Chuyển hướng đến trang thêm vé
                return RedirectToAction("Create", "Ticket", new { eventId = @event.Id, numberOfTickets, ticketPrice, startTime, endTime, ticketType });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lưu sự kiện không thành công: " + ex.Message;
            }

            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", @event.CategoryId);
            return View(@event);
        }


        // Hàm chuyển đổi IFormFile thành mảng byte
        private async Task<byte[]> ConvertToBytes(IFormFile file)
        {
            using (var memoryStream = new MemoryStream())
            {
                await file.CopyToAsync(memoryStream);
                return memoryStream.ToArray();
            }
        }

        // GET: Home/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var @event = await _context.Events
                .Include(e => e.Category)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (@event == null)
            {
                return NotFound();
            }

            return View(@event);
        }

        // POST: Home/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var @event = await _context.Events.FindAsync(id);
            if (@event != null)
            {
                _context.Events.Remove(@event);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Xóa sự kiện thành công.";
            }
            return RedirectToAction(nameof(Index));
        }

        private bool EventExists(int id)
        {
            return _context.Events.Any(e => e.Id == id);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
     
      
    }
}
