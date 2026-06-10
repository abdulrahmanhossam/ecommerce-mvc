using Microsoft.AspNetCore.Identity;
using ECommerceProject.Data.Context;
using ECommerceProject.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace ECommerceProject.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        // Correct any existing category/product images with placeholder paths
        await CorrectExistingImagesAsync(context);

        using var transaction = await context.Database.BeginTransactionAsync();

        try
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
                        ImageUrl = "/images/categories/cat_watches.png"
                    },
                    new Category 
                    { 
                        Name = "Fashion & Style", 
                        Description = "Trendy clothing and accessories",
                        ImageUrl = "/images/categories/cat_fashion.png"
                    },
                    new Category 
                    { 
                        Name = "Smart Electronics", 
                        Description = "Latest gadgets and technology",
                        ImageUrl = "/images/categories/cat_electronics.png"
                    },
                    new Category 
                    { 
                        Name = "Accessories", 
                        Description = "Premium accessories for every occasion",
                        ImageUrl = "/images/categories/cat_accessories.png"
                    },
                    new Category 
                    { 
                        Name = "Lifestyle", 
                        Description = "Premium lifestyle products",
                        ImageUrl = "/images/categories/cat_lifestyle.png"
                    },
                    new Category 
                    { 
                        Name = "Footwear", 
                        Description = "Elegant shoes for all occasions",
                        ImageUrl = "/images/categories/cat_footwear.png"
                    }
                };

                context.Categories.AddRange(categories);
                await context.SaveChangesAsync();
            }

            // Build a dictionary of category name -> id for dynamic lookups
            var categoryLookup = context.Categories
                .ToDictionary(c => c.Name, c => c.Id);

            // Create Luxury Products
            if (!context.Products.Any())
            {
                var products = new List<Product>
                {
                    // Luxury Watches
                    new Product
                    {
                        Name = "Elegant Gold Watch",
                        Description = "A stunning gold-plated timepiece with premium craftsmanship. Perfect for formal occasions and luxury events.",
                        Price = 15999,
                        Stock = 15,
                        CategoryId = categoryLookup["Luxury Watches"],
                        ImageUrl = "/images/products/gold_watch.png",
                        IsFeatured = true,
                        IsActive = true
                    },
                    new Product
                    {
                        Name = "Classic Silver Timepiece",
                        Description = "Timeless silver watch with minimalist design. Swiss movement for precise timekeeping.",
                        Price = 12500,
                        Stock = 20,
                        CategoryId = categoryLookup["Luxury Watches"],
                        ImageUrl = "/images/products/silver_watch.png",
                        IsFeatured = true,
                        IsActive = true
                    },
                    new Product
                    {
                        Name = "Diamond Accent Watch",
                        Description = "Luxurious watch featuring diamond accents and premium leather strap.",
                        Price = 22500,
                        Stock = 8,
                        CategoryId = categoryLookup["Luxury Watches"],
                        ImageUrl = "/images/products/diamond_watch.png",
                        IsFeatured = true,
                        IsActive = true
                    },

                    // Fashion & Style
                    new Product
                    {
                        Name = "Premium Wool Blazer",
                        Description = "Elegant wool blazer with perfect fit. Ideal for business meetings and formal events.",
                        Price = 4500,
                        Stock = 25,
                        CategoryId = categoryLookup["Fashion & Style"],
                        ImageUrl = "/images/products/wool_blazer.png",
                        IsFeatured = true,
                        IsActive = true
                    },
                    new Product
                    {
                        Name = "Silk Evening Dress",
                        Description = "Stunning silk dress with elegant design. Perfect for special occasions.",
                        Price = 3800,
                        Stock = 18,
                        CategoryId = categoryLookup["Fashion & Style"],
                        ImageUrl = "/images/products/evening_dress.png",
                        IsActive = true
                    },
                    new Product
                    {
                        Name = "Designer Outfit Set",
                        Description = "Complete outfit set with coordinated pieces for a sophisticated look.",
                        Price = 5500,
                        Stock = 12,
                        CategoryId = categoryLookup["Fashion & Style"],
                        ImageUrl = "/images/products/designer_outfit.png",
                        IsFeatured = true,
                        IsActive = true
                    },

                    // Smart Electronics
                    new Product
                    {
                        Name = "Premium Wireless Earbuds",
                        Description = "High-quality wireless earbuds with noise cancellation and premium sound.",
                        Price = 2800,
                        Stock = 50,
                        CategoryId = categoryLookup["Smart Electronics"],
                        ImageUrl = "/images/products/wireless_earbuds.png",
                        IsFeatured = true,
                        IsActive = true
                    },
                    new Product
                    {
                        Name = "Smart Watch Pro",
                        Description = "Advanced smartwatch with health monitoring and premium design.",
                        Price = 5500,
                        Stock = 35,
                        CategoryId = categoryLookup["Smart Electronics"],
                        ImageUrl = "/images/products/smartwatch_pro.png",
                        IsActive = true
                    },
                    new Product
                    {
                        Name = "Portable Speaker",
                        Description = "Premium portable speaker with exceptional sound quality and elegant design.",
                        Price = 1800,
                        Stock = 40,
                        CategoryId = categoryLookup["Smart Electronics"],
                        ImageUrl = "/images/categories/cat_electronics.png",
                        IsActive = true
                    },

                    // Accessories
                    new Product
                    {
                        Name = "Leather Wallet Set",
                        Description = "Premium leather wallet set with card holder and coin purse.",
                        Price = 1200,
                        Stock = 30,
                        CategoryId = categoryLookup["Accessories"],
                        ImageUrl = "/images/products/leather_wallet.png",
                        IsFeatured = true,
                        IsActive = true
                    },
                    new Product
                    {
                        Name = "Designer Sunglasses",
                        Description = "Stylish sunglasses with premium lenses and elegant frame.",
                        Price = 2200,
                        Stock = 25,
                        CategoryId = categoryLookup["Accessories"],
                        ImageUrl = "/images/products/designer_outfit.png",
                        IsActive = true
                    },
                    new Product
                    {
                        Name = "Leather Belt Set",
                        Description = "Premium leather belt set with classic buckle design.",
                        Price = 850,
                        Stock = 45,
                        CategoryId = categoryLookup["Accessories"],
                        ImageUrl = "/images/products/designer_outfit.png",
                        IsActive = true
                    },

                    // Lifestyle
                    new Product
                    {
                        Name = "Premium Yoga Mat",
                        Description = "High-quality yoga mat with superior grip and comfort.",
                        Price = 950,
                        Stock = 60,
                        CategoryId = categoryLookup["Lifestyle"],
                        ImageUrl = "/images/products/yoga_mat.png",
                        IsFeatured = true,
                        IsActive = true
                    },
                    new Product
                    {
                        Name = "Essential Oil Set",
                        Description = "Premium set of essential oils for relaxation and wellness.",
                        Price = 1500,
                        Stock = 35,
                        CategoryId = categoryLookup["Lifestyle"],
                        ImageUrl = "/images/categories/cat_lifestyle.png",
                        IsActive = true
                    },
                    new Product
                    {
                        Name = "Home Fragrance Set",
                        Description = "Elegant home fragrance collection with premium scents.",
                        Price = 1100,
                        Stock = 28,
                        CategoryId = categoryLookup["Lifestyle"],
                        ImageUrl = "/images/categories/cat_lifestyle.png",
                        IsActive = true
                    },

                    // Footwear
                    new Product
                    {
                        Name = "Premium Leather Shoes",
                        Description = "Handcrafted leather shoes with premium finish. Perfect for formal occasions.",
                        Price = 3500,
                        Stock = 22,
                        CategoryId = categoryLookup["Footwear"],
                        ImageUrl = "/images/products/leather_shoes.png",
                        IsFeatured = true,
                        IsActive = true
                    },
                    new Product
                    {
                        Name = "Designer Sneakers",
                        Description = "Trendy designer sneakers with comfort and style combined.",
                        Price = 2800,
                        Stock = 40,
                        CategoryId = categoryLookup["Footwear"],
                        ImageUrl = "/images/categories/cat_footwear.png",
                        IsActive = true
                    },
                    new Product
                    {
                        Name = "Casual Loafers",
                        Description = "Elegant casual loafers for everyday sophistication.",
                        Price = 1900,
                        Stock = 35,
                        CategoryId = categoryLookup["Footwear"],
                        ImageUrl = "/images/categories/cat_footwear.png",
                        IsActive = true
                    }
                };

                context.Products.AddRange(products);
                await context.SaveChangesAsync();
            }

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private static async Task CorrectExistingImagesAsync(ApplicationDbContext context)
    {
        // Category image mapping
        var categoryImages = new Dictionary<string, string>
        {
            { "Luxury Watches", "/images/categories/cat_watches.png" },
            { "Fashion & Style", "/images/categories/cat_fashion.png" },
            { "Smart Electronics", "/images/categories/cat_electronics.png" },
            { "Accessories", "/images/categories/cat_accessories.png" },
            { "Lifestyle", "/images/categories/cat_lifestyle.png" },
            { "Footwear", "/images/categories/cat_footwear.png" }
        };

        var categories = await context.Categories.ToListAsync();
        bool categoriesChanged = false;
        foreach (var cat in categories)
        {
            if (categoryImages.TryGetValue(cat.Name, out var correctUrl) && cat.ImageUrl != correctUrl)
            {
                cat.ImageUrl = correctUrl;
                categoriesChanged = true;
            }
        }
        if (categoriesChanged)
        {
            await context.SaveChangesAsync();
        }

        // Product image mapping
        var productImages = new Dictionary<string, string>
        {
            { "Elegant Gold Watch", "/images/products/gold_watch.png" },
            { "Classic Silver Timepiece", "/images/products/silver_watch.png" },
            { "Diamond Accent Watch", "/images/products/diamond_watch.png" },
            { "Premium Wool Blazer", "/images/products/wool_blazer.png" },
            { "Silk Evening Dress", "/images/products/evening_dress.png" },
            { "Designer Outfit Set", "/images/products/designer_outfit.png" },
            { "Premium Wireless Earbuds", "/images/products/wireless_earbuds.png" },
            { "Smart Watch Pro", "/images/products/smartwatch_pro.png" },
            { "Portable Speaker", "/images/categories/cat_electronics.png" },
            { "Leather Wallet Set", "/images/products/leather_wallet.png" },
            { "Designer Sunglasses", "/images/products/designer_outfit.png" },
            { "Leather Belt Set", "/images/products/designer_outfit.png" },
            { "Premium Yoga Mat", "/images/products/yoga_mat.png" },
            { "Essential Oil Set", "/images/categories/cat_lifestyle.png" },
            { "Home Fragrance Set", "/images/categories/cat_lifestyle.png" },
            { "Premium Leather Shoes", "/images/products/leather_shoes.png" },
            { "Designer Sneakers", "/images/categories/cat_footwear.png" },
            { "Casual Loafers", "/images/categories/cat_footwear.png" }
        };

        var products = await context.Products.ToListAsync();
        bool productsChanged = false;
        foreach (var prod in products)
        {
            if (productImages.TryGetValue(prod.Name, out var correctUrl) && prod.ImageUrl != correctUrl)
            {
                prod.ImageUrl = correctUrl;
                productsChanged = true;
            }
        }
        if (productsChanged)
        {
            await context.SaveChangesAsync();
        }
    }
}