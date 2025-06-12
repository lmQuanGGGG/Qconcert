namespace Qconcert.Models
{
    public class Notification
    {
        public int Id { get; set; }
        public string Message { get; set; }
        public bool IsRead { get; set; } = false; // Mặc định là chưa đọc
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string UserId { get; set; } // ID người dùng (nếu cần)
        public int? EventId { get; set; } // ID sự kiện liên quan (nếu có)
    }
}