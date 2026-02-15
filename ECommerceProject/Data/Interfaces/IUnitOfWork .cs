using ECommerceProject.Models.Entities;

namespace ECommerceProject.Data.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        IRepository<Category> Categories { get; }
        IRepository<Product> Products { get; }
        IRepository<ProductVariant> ProductVariants { get; }
        IRepository<Order> Orders { get; }
        IRepository<OrderItem> OrderItems { get; }
        IRepository<ShoppingCart> ShoppingCarts { get; }
        IRepository<Payment> Payments { get; }
        IRepository<ApplicationUser> Users { get; }
        IRepository<ProductReview> ProductReviews { get; }

        Task<int> SaveAsync();
    }
}