using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ECommerceProject.Models.Enums;

namespace ECommerceProject.Models.Entities
{
    public class Payment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int OrderId { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        public DateTime PaymentDate { get; set; } = DateTime.Now;

        [Required]
        public PaymentMethod PaymentMethod { get; set; }

        [Required]
        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

        [StringLength(200)]
        public string? TransactionId { get; set; } // رقم العملية من Stripe/PayPal

        [StringLength(1000)]
        public string? PaymentDetails { get; set; }

        // Navigation Property
        [ForeignKey("OrderId")]
        public virtual Order Order { get; set; } = null!;
    }
}