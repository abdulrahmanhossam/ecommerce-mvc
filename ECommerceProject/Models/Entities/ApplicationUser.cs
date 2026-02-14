using Microsoft.AspNetCore.Identity;

namespace ECommerceProject.Models.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Navigation Properties (العلاقات)
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
        public virtual ICollection<ShoppingCart> ShoppingCarts { get; set; } = new List<ShoppingCart>();
    }
}