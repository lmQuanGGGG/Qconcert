using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Qconcert.Models;
using System.Security.Claims;

namespace Qconcert.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Chỉ cho phép người dùng đã đăng nhập truy cập
    public class NotificationsController : ControllerBase
    {
        private readonly TicketBoxDb1Context _context;

        public NotificationsController(TicketBoxDb1Context context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetNotifications()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier); // Lấy ID người dùng hiện tại
            var isAdmin = User.IsInRole("Admin"); // Kiểm tra xem người dùng có phải Admin không

            var notifications = await _context.Notifications
                .Where(n =>
                    (isAdmin && n.UserId == "1") || // Admin lấy thông báo có UserId == null
                    (!isAdmin && n.UserId == userId) // Người dùng lấy thông báo của họ
                )
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new
                {
                    n.Id,
                    n.Message,
                    n.IsRead,
                    n.CreatedAt // Trả về trực tiếp kiểu DateTime
                })
                .ToListAsync();

            return Ok(notifications);
        }


        // Đánh dấu thông báo là đã đọc
        [HttpPost("{id}/mark-as-read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification == null)
                return NotFound();

            notification.IsRead = true;
            await _context.SaveChangesAsync();

            return Ok();
        }
    }
}