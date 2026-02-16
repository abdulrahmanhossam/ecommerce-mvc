using System.ComponentModel.DataAnnotations;
using ECommerceProject.Models.Enums;

namespace ECommerceProject.Models.ViewModels
{
    public class CheckoutViewModel
    {
        [Required(ErrorMessage = "Full name is required")]
        [StringLength(100)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required")]
        [Phone]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Address is required")]
        [StringLength(500)]
        [Display(Name = "Shipping Address")]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "City is required")]
        [StringLength(100)]
        [Display(Name = "City")]
        public string City { get; set; } = string.Empty;

        [Required(ErrorMessage = "Country is required")]
        [StringLength(100)]
        [Display(Name = "Country")]
        public string Country { get; set; } = string.Empty;

        [Required(ErrorMessage = "Payment method is required")]
        [Display(Name = "Payment Method")]
        public PaymentMethod PaymentMethod { get; set; }

        [StringLength(1000)]
        [Display(Name = "Order Notes")]
        public string? Notes { get; set; }

        [StringLength(50)]
        [Display(Name = "Promo Code")]
        public string? PromoCode { get; set; }
    }
}