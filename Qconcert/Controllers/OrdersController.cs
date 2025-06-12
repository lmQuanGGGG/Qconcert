using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Qconcert.Models;

namespace Qconcert.Controllers
{
    public class OrdersController : Controller
    {
        private readonly TicketBoxDb1Context _context;

        public OrdersController(TicketBoxDb1Context context)
        {
            _context = context;
        }
        [Authorize]
        public IActionResult Checkout(int id, List<int> selectedTicketIds)
        {
            // Lấy thông tin sự kiện
            var @event = _context.Events
                .Include(e => e.Tickets)
                .FirstOrDefault(e => e.Id == id && e.IsApproved); // Chỉ lấy sự kiện đã duyệt

            if (@event == null)
            {
                return NotFound("Sự kiện không tồn tại hoặc chưa được duyệt.");
            }

            // Lấy danh sách vé đã chọn
            var selectedTickets = @event.Tickets
                .Where(t => selectedTicketIds.Contains(t.Id))
                .ToList();

            // Truyền thông tin vé đã chọn vào ViewData
            ViewData["SelectedTickets"] = selectedTickets;

            return View(@event);
        }

        [Authorize]
        public IActionResult Confirmation(int id)
        {
            // Lấy thông tin đơn hàng
            var order = _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Ticket)
                .FirstOrDefault(o => o.Id == id);

            if (order == null)
            {
                return NotFound("Không tìm thấy đơn hàng.");
            }

            return View(order);
        }
        [Authorize]
        public IActionResult CartDetails(int id)
        {
            return RedirectToAction("Details", "Cart", new { id });
        }
    }
}