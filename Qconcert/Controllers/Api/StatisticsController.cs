using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using Qconcert.Models;

namespace Qconcert.Controllers.Api
{
    [Route("api/statistics")]
    [ApiController]
    public class StatisticsController : ControllerBase
    {
        private readonly TicketBoxDb1Context _context;

        public StatisticsController(TicketBoxDb1Context context)
        {
            _context = context;
        }

        [HttpGet("ad-revenue")]
        public async Task<IActionResult> GetAdRevenue([FromQuery] string period = "day")
        {
            period = period?.ToLower() ?? "day"; // Gán giá trị mặc định nếu period là null
            DateTime startDate;
            DateTime endDate = DateTime.Now;

            switch (period)
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

            // Lấy dữ liệu doanh thu cho từng ngày trong khoảng thời gian
            var dailyRevenue = await _context.PromotionPackages
                .Where(p => p.IsPaid && p.PaymentDate.HasValue && p.PaymentDate.Value >= startDate && p.PaymentDate.Value <= endDate)
                .GroupBy(p => p.PaymentDate.Value.Date) // Nhóm theo ngày, sử dụng PaymentDate.Value nếu có giá trị
                .Select(g => new
                {
                    time = g.Key.ToString("dd/MM/yyyy"), // Ngày
                    totalRevenue = g.Sum(p => p.TotalCost), // Tổng doanh thu cho ngày đó
                    vipRevenue = g.Where(p => p.Type == PromotionType.VIP).Sum(p => p.TotalCost), // Doanh thu VIP theo ngày
                    normalRevenue = g.Where(p => p.Type == PromotionType.Normal).Sum(p => p.TotalCost) // Doanh thu Thường theo ngày
                })
                .ToListAsync();

            // Tính toán doanh thu tổng cho khoảng thời gian
            var vipRevenue = await _context.PromotionPackages
                .Where(p => p.Type == PromotionType.VIP && p.IsPaid && p.PaymentDate.HasValue && p.PaymentDate.Value >= startDate && p.PaymentDate.Value <= endDate)
                .SumAsync(p => p.TotalCost);

            var normalRevenue = await _context.PromotionPackages
                .Where(p => p.Type == PromotionType.Normal && p.IsPaid && p.PaymentDate.HasValue && p.PaymentDate.Value >= startDate && p.PaymentDate.Value <= endDate)
                .SumAsync(p => p.TotalCost);

            var totalRevenue = vipRevenue + normalRevenue;

            // Trả về kết quả
            var revenueStatistics = new
            {
                period = period,
                startDate = startDate.ToString("dd/MM/yyyy"),
                endDate = endDate.ToString("dd/MM/yyyy"),
                vipRevenue = vipRevenue,
                normalRevenue = normalRevenue,
                totalRevenue = totalRevenue,
                dailyRevenue = dailyRevenue // Thêm dữ liệu doanh thu theo ngày
            };

            return Ok(revenueStatistics);
        }
    }
}
