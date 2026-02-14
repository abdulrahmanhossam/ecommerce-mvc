using ECommerceProject.Data.Context;
using ECommerceProject.Data.Interfaces;
using ECommerceProject.Models.Entities;

namespace ECommerceProject.Data.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;

        public IRepository<Category> Categories { get; private set; }
        public IRepository<Product> Products { get; private set; }
        public IRepository<ProductVariant> ProductVariants { get; private set; }
        public IRepository<Order> Orders { get; private set; }
        public IRepository<OrderItem> OrderItems { get; private set; }
        public IRepository<ShoppingCart> ShoppingCarts { get; private set; }
        public IRepository<Payment> Payments { get; private set; }
        public IRepository<ApplicationUser> Users { get; private set; }

        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context;

            Categories = new Repository<Category>(_context);
            Products = new Repository<Product>(_context);
            ProductVariants = new Repository<ProductVariant>(_context);
            Orders = new Repository<Order>(_context);
            OrderItems = new Repository<OrderItem>(_context);
            ShoppingCarts = new Repository<ShoppingCart>(_context);
            Payments = new Repository<Payment>(_context);
            Users = new Repository<ApplicationUser>(_context);
        }

        public async Task<int> SaveAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}