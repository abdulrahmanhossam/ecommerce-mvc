using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ECommerceProject.Models.Entities
{
    public class ProductVariant
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }

        [StringLength(50)]
        public string? Size { get; set; } // S, M, L, XL

        [StringLength(50)]
        public string? Color { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal AdditionalPrice { get; set; } = 0; // سعر إضافي للمقاس/اللون

        public int Stock { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        // Navigation Property
        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; } = null!;
    }
}