using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Qconcert.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

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

        [Authorize]
        public IActionResult Revenue()
        {
            return View();
        }
    }

    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class StatisticsController : ControllerBase
    {
        private readonly TicketBoxDb1Context _context;

        public StatisticsController(TicketBoxDb1Context context)
        {
            _context = context;
        }

        [HttpGet("revenue-by-event")]
        public async Task<IActionResult> GetRevenueByEvent(int? eventId, string timeRange = "30days")
        {
            // Lấy UserId của người dùng hiện tại
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("Không thể xác định người dùng.");
            }

            // Xác định khoảng thời gian
            DateTime startDate = timeRange switch
            {
                "24hours" => DateTime.Now.AddHours(-24),
                "1year" => DateTime.Now.AddYears(-1),
                _ => DateTime.Now.AddDays(-30) // Mặc định là 30 ngày
            };

            // Truy vấn doanh thu
            var query = _context.OrderDetails
                .Where(od => od.Order.PaymentStatus == "Thanh toán thành công" &&
                             od.Order.PaymentDate >= startDate &&
                             od.Ticket.Event.CreatedBy == userId &&
                             od.Ticket.Event.IsApproved == true);

            if (eventId.HasValue)
            {
                query = query.Where(od => od.Ticket.EventId == eventId.Value);
            }

            // Nhóm dữ liệu theo khoảng thời gian
            var revenueData = timeRange switch
            {
                "24hours" => await query
                    .GroupBy(od => od.Order.PaymentDate.Value.Hour) // Nhóm theo giờ
                    .Select(g => new
                    {
                        Time = g.Key + ":00", // Hiển thị giờ
                        TotalRevenue = g.Sum(od => od.Price * od.Quantity),
                        TicketsSold = g.Sum(od => od.Quantity)
                    })
                    .ToListAsync(),
                "1year" => await query
                    .GroupBy(od => od.Order.PaymentDate.Value.Month) // Nhóm theo tháng
                    .Select(g => new
                    {
                        Time = "Tháng " + g.Key, // Hiển thị tháng
                        TotalRevenue = g.Sum(od => od.Price * od.Quantity),
                        TicketsSold = g.Sum(od => od.Quantity)
                    })
                    .ToListAsync(),
                _ => await query
                    .GroupBy(od => od.Order.PaymentDate.Value.Date) // Nhóm theo ngày
                    .Select(g => new
                    {
                        Time = g.Key.ToString("dd/MM/yyyy"), // Hiển thị ngày
                        TotalRevenue = g.Sum(od => od.Price * od.Quantity),
                        TicketsSold = g.Sum(od => od.Quantity)
                    })
                    .ToListAsync()
            };

            return Ok(revenueData);
        }

        [HttpGet("tickets-sold-by-event")]
        public async Task<IActionResult> GetTicketsSoldByEvent(int eventId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("Không thể xác định người dùng.");
            }

            var eventEntity = await _context.Events
                .Include(e => e.Tickets)
                    .ThenInclude(t => t.OrderDetails)
                        .ThenInclude(od => od.Order) // Include Order luôn
                .FirstOrDefaultAsync(e => e.Id == eventId && e.CreatedBy == userId);

            if (eventEntity == null)
            {
                return NotFound("Sự kiện không tồn tại hoặc bạn không có quyền truy cập.");
            }

            var tickets = eventEntity.Tickets
                 .GroupBy(t => new { t.LoaiVe, t.Price })
                 .Select(g => new
            {
                  TicketType = g.Key.LoaiVe,
                  Price = g.Key.Price,
                  TotalTickets = g.FirstOrDefault().SoLuongGhe, // Lấy giá trị tổng số vé từ cột trong DB
                  SoldTickets = g.Sum(t => t.OrderDetails != null
                            ? t.OrderDetails
                                .Where(od => od.Order != null && od.Order.PaymentStatus == "Thanh toán thành công")
                                .Sum(od => od.Quantity)
                            : 0)
    })
    .ToList();


            var result = tickets.Select(t => new
            {
                t.TicketType,
                Price = t.Price,
                Sold = t.SoldTickets,
                Total = t.TotalTickets,
                SoldPercentage = t.TotalTickets > 0 ? (double)t.SoldTickets / t.TotalTickets * 100 : 0
            });

            return Ok(result);
        }



        [HttpGet("api/events")]
        public async Task<IActionResult> GetEvents()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("Không thể xác định người dùng.");
            }

            var events = await _context.Events
                .Where(e => e.CreatedBy == userId && e.IsApproved == true)
                .Select(e => new { e.Id, e.Name })
                .ToListAsync();

            return Ok(events);
        }
    }
}