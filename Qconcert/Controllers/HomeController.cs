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

        public async Task<IActionResult> Search(string query)
        {
            var events = await _context.Events
                                       .Include(e => e.Category)
                                       .Where(e => (e.Name.Contains(query) || e.Description.Contains(query)) && e.IsApproved)
                                       .ToListAsync();
            return View(events);
        }


        public async Task<IActionResult> Index()
        {
            var events = await _context.Events
                                       .Include(e => e.Category)
                                       .Where(e => e.IsApproved) // Lọc chỉ hiển thị sự kiện đã duyệt
                                       .ToListAsync();

            var groupedEvents = events.GroupBy(e => e.Category).ToList();
            return View(groupedEvents);
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
            @event.IsApproved = true; // Mặc định  được duyệt

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
            if (@event.CreatedBy != User.FindFirstValue(ClaimTypes.NameIdentifier))
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
            if (existingEvent.CreatedBy != User.FindFirstValue(ClaimTypes.NameIdentifier))
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
            if (@event.CreatedBy != User.FindFirstValue(ClaimTypes.NameIdentifier))
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
