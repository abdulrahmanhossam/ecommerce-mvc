namespace ECommerceProject.Models.Entities
{
    public class Wishlist
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public int ProductId { get; set; }
        public DateTime AddedDate { get; set; } = DateTime.Now;

        public virtual ApplicationUser User { get; set; } = null!;
        public virtual Product Product { get; set; } = null!;
    }
}