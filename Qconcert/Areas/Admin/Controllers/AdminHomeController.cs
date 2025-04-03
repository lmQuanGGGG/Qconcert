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

namespace Qconcert.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class AdminHomeController : Controller
    {
        private readonly ILogger<AdminHomeController> _logger;
        private readonly TicketBoxDb1Context _context;
        private readonly TicketService _ticketService;

        public AdminHomeController(ILogger<AdminHomeController> logger, TicketBoxDb1Context context, TicketService ticketService)
        {
            _logger = logger;
            _context = context;
            _ticketService = ticketService;
        }

        public async Task<IActionResult> Search(string query)
        {
            var events = await _context.Events
                                       .Include(e => e.Category)
                                       .Where(e => e.Name.Contains(query) || e.Description.Contains(query))
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
        public async Task<IActionResult> ApproveEvent(int id)
        {
            _logger.LogInformation("ApproveEvent method called with ID {EventId}", id);

            var @event = await _context.Events.FindAsync(id);
            if (@event == null)
            {
                return NotFound();
            }

            @event.IsApproved = true;
            _context.Update(@event);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving event with ID {EventId}", id);
                return StatusCode(500, "Internal server error");
            }

            return RedirectToAction(nameof(PendingEvents));
        }


        // Từ chối sự kiện
        [HttpPost]
        public async Task<IActionResult> RejectEvent(int id)
        {
            var @event = await _context.Events.FindAsync(id);
            if (@event == null)
            {
                return NotFound();
            }

            _context.Events.Remove(@event);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(PendingEvents));
        }


// GET: Home/Index
public async Task<IActionResult> Index()
        {
            var events = await _context.Events.Include(e => e.Category).ToListAsync();
            var groupedEvents = events.GroupBy(e => e.Category).ToList();
            return View(groupedEvents);
        }

        // GET: Home/Details/5
        public async Task<IActionResult> Details(int? id)
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

        // GET: Home/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var @event = await _context.Events.FindAsync(id);
            if (@event == null)
            {
                return NotFound();
            }
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", @event.CategoryId);
            return View(@event);

        }

        // POST: Home/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,EventInfo,Date,CategoryId,Capacity,Province,District,Ward,AddressDetail,OrganizerName,OrganizerInfo")] Event @event, IFormFile Image9x16, IFormFile Image16x9, IFormFile OrganizerLogo)
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

            return RedirectToAction(nameof(Index));
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
