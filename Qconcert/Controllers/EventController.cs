using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Qconcert.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using iTextSharp.text.pdf;
using iTextSharp.text;

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
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("Không thể xác định người dùng.");
            }

            var isAdmin = User.IsInRole("Admin");

            // Xử lý startDate theo timeRange
            DateTime? startDate = timeRange switch
            {
                "24hours" => DateTime.Now.AddHours(-24),
                "1year" => DateTime.Now.AddYears(-1),
                "all" => null, // Khi chọn "all", không có giới hạn thời gian
                _ => DateTime.Now.AddDays(-30)
            };

            var query = _context.OrderDetails
                .Where(od =>
                    od.Order.PaymentStatus == "Thanh toán thành công" &&
                    (startDate == null || od.Order.PaymentDate >= startDate) && // Điều kiện cho startDate
                    od.Ticket.Event.IsApproved == true &&
                    (isAdmin || od.Ticket.Event.CreatedBy == userId));

            // Nếu có eventId thì thêm điều kiện lọc
            if (eventId.HasValue)
            {
                query = query.Where(od => od.Ticket.EventId == eventId.Value);
            }

            // Group và tính toán doanh thu và số vé bán được
            var revenueData = timeRange switch
            {
                "24hours" => await query
                    .GroupBy(od => od.Order.PaymentDate.Value.Hour)
                    .Select(g => new
                    {
                        Time = g.Key + ":00",
                        TotalRevenue = g.Sum(od => od.Price * od.Quantity),
                        TicketsSold = g.Sum(od => od.Quantity)
                    })
                    .ToListAsync(),

                "1year" => await query
                    .GroupBy(od => od.Order.PaymentDate.Value.Month)
                    .Select(g => new
                    {
                        Time = "Tháng " + g.Key,
                        TotalRevenue = g.Sum(od => od.Price * od.Quantity),
                        TicketsSold = g.Sum(od => od.Quantity)
                    })
                    .ToListAsync(),

                "all" => await query
                    .GroupBy(od => 1) // Group tất cả dữ liệu vào một nhóm duy nhất
                    .Select(g => new
                    {
                        Time = "Tổng cộng", // Không cần nhóm theo thời gian, chỉ là tổng cộng
                        TotalRevenue = g.Sum(od => od.Price * od.Quantity),
                        TicketsSold = g.Sum(od => od.Quantity)
                    })
                    .ToListAsync(),

                _ => await query
                    .GroupBy(od => od.Order.PaymentDate.Value.Date)
                    .Select(g => new
                    {
                        Time = g.Key.ToString("dd/MM/yyyy"),
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

            var isAdmin = User.IsInRole("Admin");

            var eventEntity = await _context.Events
                .Include(e => e.Tickets)
                    .ThenInclude(t => t.OrderDetails)
                        .ThenInclude(od => od.Order)
                .FirstOrDefaultAsync(e => e.Id == eventId && (isAdmin || e.CreatedBy == userId));

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
                    TotalTickets = g.FirstOrDefault().SoLuongGhe,
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
        [HttpGet("full-revenue-summary")]
        public async Task<IActionResult> GetFullRevenueSummary(int eventId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("Không thể xác định người dùng.");
            }

            var isAdmin = User.IsInRole("Admin");

            var eventEntity = await _context.Events
                .Include(e => e.Tickets)
                    .ThenInclude(t => t.OrderDetails)
                        .ThenInclude(od => od.Order)
                .FirstOrDefaultAsync(e => e.Id == eventId && (isAdmin || e.CreatedBy == userId));

            if (eventEntity == null)
            {
                return NotFound("Sự kiện không tồn tại hoặc bạn không có quyền truy cập.");
            }

            // Tổng số vé của tất cả các loại vé
            int totalTickets = eventEntity.Tickets.Sum(t => t.SoLuongGhe);

            // Tổng doanh thu (bao gồm cả đơn chưa thanh toán)
            decimal totalRevenue = eventEntity.Tickets.Sum(t => t.SoLuongGhe * t.Price);


            return Ok(new
            {
                EventId = eventEntity.Id,
                EventName = eventEntity.Name,
                TotalTickets = totalTickets,
                TotalRevenue = totalRevenue
            });
        }


        [HttpGet("api/events")]
        public async Task<IActionResult> GetEvents()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("Không thể xác định người dùng.");
            }

            var isAdmin = User.IsInRole("Admin");

            var events = await _context.Events
                .Where(e => e.IsApproved == true && (isAdmin || e.CreatedBy == userId))
                .Select(e => new { e.Id, e.Name })
                .ToListAsync();

            return Ok(events);
        }
    }

}