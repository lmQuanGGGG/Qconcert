using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Qconcert.Models;
using System;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;
using Qconcert.ViewModels;
using Microsoft.AspNetCore.Authorization;
namespace Qconcert.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class PromotionControllers : Controller
    {
        private readonly TicketBoxDb1Context _context;

        public PromotionControllers(TicketBoxDb1Context context)
        {
            _context = context;
        }
        public async Task<IActionResult> RevenueStatistics(string period = "day")
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

            // Tính toán doanh thu
            var vipRevenue = await _context.PromotionPackages
                .Where(p => p.Type == PromotionType.VIP && p.IsPaid && p.PaymentDate >= startDate && p.PaymentDate <= endDate)
                .SumAsync(p => p.TotalCost);

            var normalRevenue = await _context.PromotionPackages
                .Where(p => p.Type == PromotionType.Normal && p.IsPaid && p.PaymentDate >= startDate && p.PaymentDate <= endDate)
                .SumAsync(p => p.TotalCost);

            var totalRevenue = vipRevenue + normalRevenue;

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
    }
}
