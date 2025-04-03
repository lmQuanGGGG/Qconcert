using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Qconcert.Models;

namespace Qconcert.Controllers
{
    public class EventController : Controller
    {
        private readonly TicketBoxDb1Context _context;

        public EventController(TicketBoxDb1Context context)
        {
            _context = context;
        }

       public IActionResult Dashboard()
{
    var events = _context.Events
                         .Include(e => e.Category)
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