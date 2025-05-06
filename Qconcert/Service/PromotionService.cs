using Qconcert.Models;
using Microsoft.EntityFrameworkCore;

namespace Qconcert.Services
{
    public class PromotionService
    {
        private readonly TicketBoxDb1Context _context;

        public PromotionService(TicketBoxDb1Context context)
        {
            _context = context;
        }

        public async Task CheckAndActivatePendingPromotions()
        {
            // Lấy danh sách các gói VIP đang chờ hiển thị
            var pendingPromotions = await _context.PromotionPackages
                .Where(p => p.Type == PromotionType.VIP && p.Status == PromotionStatus.Pending && p.IsInQueue)
                .OrderBy(p => p.CreatedAt) // Ưu tiên gói được tạo sớm nhất
                .ToListAsync();

            foreach (var promotion in pendingPromotions)
            {
                // Tính thời gian kết thúc dự kiến
                var requestedEndDate = promotion.RequestedStartDate.AddDays(promotion.DurationInDays);

                // Lấy số lượng sự kiện VIP đang hiển thị
                var activeVipCount = await _context.PromotionPackages
                    .Where(p => p.Type == PromotionType.VIP && p.Status == PromotionStatus.Approved)
                    .Where(p => p.ActualStartDate.HasValue && p.ActualStartDate.Value <= DateTime.Now)
                    .Where(p => p.ActualStartDate.Value.AddDays(p.DurationInDays) >= DateTime.Now)
                    .CountAsync();

                // Nếu còn slot trống, bật sự kiện này
                if (activeVipCount < 4)
                {
                    promotion.Status = PromotionStatus.Approved;
                    promotion.ActualStartDate = DateTime.Now; // Bắt đầu hiển thị ngay
                    promotion.IsInQueue = false;

                    _context.PromotionPackages.Update(promotion);
                    await _context.SaveChangesAsync();
                }
            }
        }
    }
}