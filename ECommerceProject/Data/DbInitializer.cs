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

        // Create Admin User
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

        // Create Sample Categories
        if (!context.Categories.Any())
        {
            var categories = new List<Category>
            {
                new Category { Name = "Electronics", Description = "Phones, Laptops, Smart Devices", ImageUrl = "/images/categories/electronics.jpg" },
                new Category { Name = "Clothing", Description = "Men and Women Clothing", ImageUrl = "/images/categories/clothes.jpg" },
                new Category { Name = "Shoes", Description = "Sports and Casual Shoes", ImageUrl = "/images/categories/shoes.jpg" },
                new Category { Name = "Books", Description = "Books and Magazines", ImageUrl = "/images/categories/books.jpg" },
                new Category { Name = "Toys", Description = "Kids Toys and Video Games", ImageUrl = "/images/categories/toys.jpg" }
            };

            context.Categories.AddRange(categories);
            await context.SaveChangesAsync();
        }

        // Create Sample Products
        if (!context.Products.Any())
        {
            var products = new List<Product>
            {
                new Product
                {
                    Name = "iPhone 15 Pro",
                    Description = "Latest Apple Phone with A17 Pro chip",
                    Price = 45000,
                    Stock = 50,
                    CategoryId = 1,
                    ImageUrl = "https://images.unsplash.com/photo-1592286927505-534a04738dd3?w=500",
                    IsFeatured = true
                },
                new Product
                {
                    Name = "Samsung Galaxy S24",
                    Description = "Samsung Flagship Phone with AI features",
                    Price = 35000,
                    Stock = 30,
                    CategoryId = 1,
                    ImageUrl = "https://images.unsplash.com/photo-1610945415295-d9bbf067e59c?w=500",
                    IsFeatured = true
                },
                new Product
                {
                    Name = "Cotton T-Shirt",
                    Description = "100% Premium Cotton Comfortable T-Shirt",
                    Price = 250,
                    Stock = 100,
                    CategoryId = 2,
                    ImageUrl = "https://images.unsplash.com/photo-1521572163474-6864f9cf17ab?w=500"
                },
                new Product
                {
                    Name = "Nike Air Max",
                    Description = "Comfortable Sports Shoes with Air Technology",
                    Price = 1500,
                    Stock = 40,
                    CategoryId = 3,
                    ImageUrl = "https://images.unsplash.com/photo-1542291026-7eec264c27ff?w=500",
                    IsFeatured = true
                },
                new Product
                {
                    Name = "C# Programming Book",
                    Description = "Learn Programming from Scratch - Complete Guide",
                    Price = 350,
                    Stock = 25,
                    CategoryId = 4,
                    ImageUrl = "https://images.unsplash.com/photo-1589998059171-988d887df646?w=500"
                }
            };

            context.Products.AddRange(products);
            await context.SaveChangesAsync();
        }
    }
}