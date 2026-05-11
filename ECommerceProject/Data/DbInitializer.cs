using Microsoft.AspNetCore.Identity;
using ECommerceProject.Data.Context;
using ECommerceProject.Models.Entities;

namespace ECommerceProject.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        // Create Roles
        if (!await roleManager.RoleExistsAsync("Admin"))
            await roleManager.CreateAsync(new IdentityRole("Admin"));

        if (!await roleManager.RoleExistsAsync("Seller"))
            await roleManager.CreateAsync(new IdentityRole("Seller"));

        if (!await roleManager.RoleExistsAsync("Customer"))
            await roleManager.CreateAsync(new IdentityRole("Customer"));

        // Create Admin User (if not exists - keeps existing admin)
        if (await userManager.FindByEmailAsync("admin@ecommerce.com") == null)
        {
            var adminUser = new ApplicationUser
            {
                UserName = "admin@ecommerce.com",
                Email = "admin@ecommerce.com",
                FullName = "System Administrator",
                EmailConfirmed = true,
                PhoneNumber = "01000000000",
                Address = "Cairo, Egypt",
                City = "Cairo",
                Country = "Egypt"
            };

            var result = await userManager.CreateAsync(adminUser, "Admin@123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }

        // Create Luxury Categories
        if (!context.Categories.Any())
        {
            var categories = new List<Category>
            {
                new Category 
                { 
                    Name = "Luxury Watches", 
                    Description = "Premium timepieces from world-renowned brands",
                    ImageUrl = "/images/products/angela-bailey-jlo7Bf4tUoY-unsplash.jpg"
                },
                new Category 
                { 
                    Name = "Fashion & Style", 
                    Description = "Trendy clothing and accessories",
                    ImageUrl = "/images/products/chico__fotografo-eJFMPXcTtbU-unsplash.jpg"
                },
                new Category 
                { 
                    Name = "Smart Electronics", 
                    Description = "Latest gadgets and technology",
                    ImageUrl = "/images/products/nathan-dumlao-5xyknPxKOq8-unsplash.jpg"
                },
                new Category 
                { 
                    Name = "Accessories", 
                    Description = "Premium accessories for every occasion",
                    ImageUrl = "/images/products/the-drink-break-4sWDjeixAJU-unsplash.jpg"
                },
                new Category 
                { 
                    Name = "Lifestyle", 
                    Description = "Premium lifestyle products",
                    ImageUrl = "/images/products/maxim-hopman-Hin-rzhOdWs-unsplash.jpg"
                },
                new Category 
                { 
                    Name = "Footwear", 
                    Description = "Elegant shoes for all occasions",
                    ImageUrl = "/images/products/amanz-AoV1tpTXoHM-unsplash.jpg"
                }
            };

            context.Categories.AddRange(categories);
            await context.SaveChangesAsync();
        }

        // Create Luxury Products
        if (!context.Products.Any())
        {
            var products = new List<Product>
            {
                // Category 1 - Luxury Watches
                new Product
                {
                    Name = "Elegant Gold Watch",
                    Description = "A stunning gold-plated timepiece with premium craftsmanship. Perfect for formal occasions and luxury events.",
                    Price = 15999,
                    Stock = 15,
                    CategoryId = 1,
                    ImageUrl = "/images/products/angela-bailey-jlo7Bf4tUoY-unsplash.jpg",
                    IsFeatured = true,
                    IsActive = true
                },
                new Product
                {
                    Name = "Classic Silver Timepiece",
                    Description = "Timeless silver watch with minimalist design. Swiss movement for precise timekeeping.",
                    Price = 12500,
                    Stock = 20,
                    CategoryId = 1,
                    ImageUrl = "/images/products/alison-wang-mou0S7ViElQ-unsplash.jpg",
                    IsFeatured = true,
                    IsActive = true
                },
                new Product
                {
                    Name = "Diamond Accent Watch",
                    Description = "Luxurious watch featuring diamond accents and premium leather strap.",
                    Price = 22500,
                    Stock = 8,
                    CategoryId = 1,
                    ImageUrl = "/images/products/bram-van-oost-Yv8bUMDdhBA-unsplash.jpg",
                    IsFeatured = true,
                    IsActive = true
                },
                
                // Category 2 - Fashion & Style
                new Product
                {
                    Name = "Premium Wool Blazer",
                    Description = "Elegant wool blazer with perfect fit. Ideal for business meetings and formal events.",
                    Price = 4500,
                    Stock = 25,
                    CategoryId = 2,
                    ImageUrl = "/images/products/chico__fotografo-eJFMPXcTtbU-unsplash.jpg",
                    IsFeatured = true,
                    IsActive = true
                },
                new Product
                {
                    Name = "Silk Evening Dress",
                    Description = "Stunning silk dress with elegant design. Perfect for special occasions.",
                    Price = 3800,
                    Stock = 18,
                    CategoryId = 2,
                    ImageUrl = "/images/products/igor-omilaev-lDWTfYhZ85w-unsplash.jpg",
                    IsActive = true
                },
                new Product
                {
                    Name = "Designer Outfit Set",
                    Description = "Complete outfit set with coordinated pieces for a sophisticated look.",
                    Price = 5500,
                    Stock = 12,
                    CategoryId = 2,
                    ImageUrl = "/images/products/sandy-millar-S5pFhDxUXyw-unsplash.jpg",
                    IsFeatured = true,
                    IsActive = true
                },
                
                // Category 3 - Smart Electronics
                new Product
                {
                    Name = "Premium Wireless Earbuds",
                    Description = "High-quality wireless earbuds with noise cancellation and premium sound.",
                    Price = 2800,
                    Stock = 50,
                    CategoryId = 3,
                    ImageUrl = "/images/products/nathan-dumlao-5xyknPxKOq8-unsplash.jpg",
                    IsFeatured = true,
                    IsActive = true
                },
                new Product
                {
                    Name = "Smart Watch Pro",
                    Description = "Advanced smartwatch with health monitoring and premium design.",
                    Price = 5500,
                    Stock = 35,
                    CategoryId = 3,
                    ImageUrl = "/images/products/nathan-dumlao-oPsUNgdfo2A-unsplash.jpg",
                    IsActive = true
                },
                new Product
                {
                    Name = "Portable Speaker",
                    Description = "Premium portable speaker with exceptional sound quality and elegant design.",
                    Price = 1800,
                    Stock = 40,
                    CategoryId = 3,
                    ImageUrl = "/images/products/jerry-wang-qBrF1yu5Wys-unsplash.jpg",
                    IsActive = true
                },
                
                // Category 4 - Accessories
                new Product
                {
                    Name = "Leather Wallet Set",
                    Description = "Premium leather wallet set with card holder and coin purse.",
                    Price = 1200,
                    Stock = 30,
                    CategoryId = 4,
                    ImageUrl = "/images/products/the-drink-break-4sWDjeixAJU-unsplash.jpg",
                    IsFeatured = true,
                    IsActive = true
                },
                new Product
                {
                    Name = "Designer Sunglasses",
                    Description = "Stylish sunglasses with premium lenses and elegant frame.",
                    Price = 2200,
                    Stock = 25,
                    CategoryId = 4,
                    ImageUrl = "/images/products/chris-hardy-H5Ffv4I5ZMI-unsplash.jpg",
                    IsActive = true
                },
                new Product
                {
                    Name = "Leather Belt Set",
                    Description = "Premium leather belt set with classic buckle design.",
                    Price = 850,
                    Stock = 45,
                    CategoryId = 4,
                    ImageUrl = "/images/products/dmitry-chernyshov-mP7aPSUm7aE-unsplash.jpg",
                    IsActive = true
                },
                
                // Category 5 - Lifestyle
                new Product
                {
                    Name = "Premium Yoga Mat",
                    Description = "High-quality yoga mat with superior grip and comfort.",
                    Price = 950,
                    Stock = 60,
                    CategoryId = 5,
                    ImageUrl = "/images/products/maxim-hopman-Hin-rzhOdWs-unsplash.jpg",
                    IsFeatured = true,
                    IsActive = true
                },
                new Product
                {
                    Name = "Essential Oil Set",
                    Description = "Premium set of essential oils for relaxation and wellness.",
                    Price = 1500,
                    Stock = 35,
                    CategoryId = 5,
                    ImageUrl = "/images/products/milada-vigerova-p8Drpg_duLw-unsplash.jpg",
                    IsActive = true
                },
                new Product
                {
                    Name = "Home Fragrance Set",
                    Description = "Elegant home fragrance collection with premium scents.",
                    Price = 1100,
                    Stock = 28,
                    CategoryId = 5,
                    ImageUrl = "/images/products/tobias-tullius-Fg15LdqpWrs-unsplash.jpg",
                    IsActive = true
                },
                
                // Category 6 - Footwear
                new Product
                {
                    Name = "Premium Leather Shoes",
                    Description = "Handcrafted leather shoes with premium finish. Perfect for formal occasions.",
                    Price = 3500,
                    Stock = 22,
                    CategoryId = 6,
                    ImageUrl = "/images/products/amanz-AoV1tpTXoHM-unsplash.jpg",
                    IsFeatured = true,
                    IsActive = true
                },
                new Product
                {
                    Name = "Designer Sneakers",
                    Description = "Trendy designer sneakers with comfort and style combined.",
                    Price = 2800,
                    Stock = 40,
                    CategoryId = 6,
                    ImageUrl = "/images/products/gulfer-ergin-LUGuCtvlk1Q-unsplash.jpg",
                    IsActive = true
                },
                new Product
                {
                    Name = "Casual Loafers",
                    Description = "Elegant casual loafers for everyday sophistication.",
                    Price = 1900,
                    Stock = 35,
                    CategoryId = 6,
                    ImageUrl = "/images/products/prince-akachi-iuqcmC4NVNo-unsplash.jpg",
                    IsActive = true
                }
            };

            context.Products.AddRange(products);
            await context.SaveChangesAsync();
        }
    }
}