using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Qconcert.Models
{
    public class OrderDetail
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int OrderId { get; set; }

        [ForeignKey("OrderId")]
        public Order Order { get; set; }

        [Required]
        public int TicketId { get; set; }

        [ForeignKey("TicketId")]
        public Ticket Ticket { get; set; }

        [Required]
        public int Quantity { get; set; }

        public decimal Price { get; set; }
    }
}
