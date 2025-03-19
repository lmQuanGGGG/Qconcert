using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Qconcert.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;


namespace Qconcert.Controllers
{
    // Controller cho các phương thức CRUD của Event
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly TicketBoxDb1Context _context;

        public HomeController(ILogger<HomeController> logger, TicketBoxDb1Context context)
        {
            _logger = logger;
            _context = context;
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

        // POST: Home/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Description,EventInfo,Date,CategoryId,Price,Capacity,Province,District,Ward,AddressDetail,Image9x16,Image16x9,OrganizerName,OrganizerInfo,OrganizerLogo")] Event @event, IFormFile Image9x16, IFormFile Image16x9, IFormFile OrganizerLogo)
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
                TempData["SuccessMessage"] = "Lưu sự kiện thành công.";
                return RedirectToAction(nameof(Index));
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
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,EventInfo,Date,CategoryId,Price,Capacity,Province,District,Ward,AddressDetail,Image9x16,Image16x9,OrganizerName,OrganizerInfo,OrganizerLogo")] Event @event, IFormFile Image9x16, IFormFile Image16x9, IFormFile OrganizerLogo)
        {
            if (id != @event.Id)
            {
                return NotFound();
            }

            var existingEvent = await _context.Events.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);
            if (existingEvent == null)
            {
                return NotFound();
            }

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
                else
                {
                    @event.Image9x16 = existingEvent.Image9x16;
                }

                if (Image16x9 != null && Image16x9.Length > 0)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        await Image16x9.CopyToAsync(memoryStream);
                        @event.Image16x9 = memoryStream.ToArray();
                    }
                }
                else
                {
                    @event.Image16x9 = existingEvent.Image16x9;
                }

                if (OrganizerLogo != null && OrganizerLogo.Length > 0)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        await OrganizerLogo.CopyToAsync(memoryStream);
                        @event.OrganizerLogo = memoryStream.ToArray();
                    }
                }
                else
                {
                    @event.OrganizerLogo = existingEvent.OrganizerLogo;
                }

                _context.Update(@event);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Cập nhật sự kiện thành công.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!EventExists(@event.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Cập nhật sự kiện không thành công: " + ex.Message;
            }

            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", @event.CategoryId);
            return View(@event);
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
