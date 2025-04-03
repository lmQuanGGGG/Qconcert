using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Qconcert.Models
{
    public class PaymentInfo
    {
        [Key]
        public int PaymentInfoId { get; set; }

        [Required]
        public int EventId { get; set; }

        [ForeignKey("EventId")]
        public Event Event { get; set; }

        [Required]
        public string AccountHolder { get; set; }

        [Required]
        public string AccountNumber { get; set; }

        [Required]
        public string BankName { get; set; }

        [Required]
        public string Branch { get; set; }
    }
}
