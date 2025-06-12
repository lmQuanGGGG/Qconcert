namespace Qconcert.Models
{
    public enum PromotionType
    {
        Normal, // ảnh
        VIP     // video
    }

    public enum PromotionStatus
{
    Pending,   // Đang chờ xử lý
    Approved,  // Đã duyệt và hiển thị
    Rejected,  // Bị từ chối
    Paid,      // Đã thanh toán nhưng chưa duyệt
    Expired    // Gói khuyến mãi đã hết hạn
}

    public class PromotionPackage
{
    public int Id { get; set; }
    public int EventId { get; set; }
        public string UserId { get; set; }
        public PromotionType Type { get; set; }
    public DateTime RequestedStartDate { get; set; }
    public DateTime? ActualStartDate { get; set; }
    public int DurationInDays { get; set; }
    public string MediaPath { get; set; }

    public bool IsInQueue { get; set; } = false;
    public bool IsPaid { get; set; } = false;

    public decimal TotalCost { get; set; } // Tổng chi phí
    public string TransactionId { get; set; } // Mã giao dịch thanh toán
    public DateTime? PaymentDate { get; set; } // Ngày thanh toán

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public PromotionStatus Status { get; set; } = PromotionStatus.Pending;

    public Event Event { get; set; }
}
}
