using System.ComponentModel.DataAnnotations;

namespace ECommerceProject.Models.ViewModels
{
    public class AddReviewViewModel
    {
        public int ProductId { get; set; }

        [Required(ErrorMessage = "Rating is required")]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
        [Display(Name = "Rating")]
        public int Rating { get; set; }

        [StringLength(100)]
        [Display(Name = "Review Title (Optional)")]
        public string? Title { get; set; }

        [Required(ErrorMessage = "Review comment is required")]
        [StringLength(2000, MinimumLength = 10, ErrorMessage = "Review must be between 10 and 2000 characters")]
        [Display(Name = "Your Review")]
        public string Comment { get; set; } = string.Empty;
    }
}