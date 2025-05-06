using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Qconcert.Models
{
    public class Order
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }
        [Required]
        public string EventId { get; set; }
        [Required]
        public DateTime OrderDate { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        public decimal TotalAmount { get; set; }
        // Trạng thái thanh toán
        [Required]
        public string PaymentStatus { get; set; } = "Chưa thanh toán"; // Giá trị mặc định

        // Phương thức thanh toán
        public string PaymentMethod { get; set; }
         // Thêm các trường mới
        public string TransactionId { get; set; }
        public DateTime? PaymentDate { get; set; }
        public string BankTransferImage { get; set; }
        public string QrCodeUrl { get; set; }
        public ICollection<OrderDetail> OrderDetails { get; set; }
    }
}
