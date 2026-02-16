using System.ComponentModel.DataAnnotations;
using ECommerceProject.Models.Enums;

namespace ECommerceProject.Models.Entities
{
    public class PromoCode
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Code { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Description { get; set; }

        [Required]
        public DiscountType DiscountType { get; set; }

        [Required]
        public decimal DiscountValue { get; set; }

        public decimal? MinimumPurchase { get; set; }

        public decimal? MaximumDiscount { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public int? UsageLimit { get; set; }

        public int UsageCount { get; set; } = 0;

        public int? UsageLimitPerUser { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Navigation Properties
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}