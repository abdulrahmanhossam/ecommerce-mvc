using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerceProject.Migrations
{
    public partial class SeedData : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Insert Categories
            migrationBuilder.Sql(@"
                INSERT INTO Categories (Name, Description, ImageUrl, CreatedDate, IsActive) VALUES
                ('Luxury Watches', 'Premium timepieces from world-renowned brands', '/images/categories/cat_watches.png', GETDATE(), 1),
                ('Fashion & Style', 'Trendy clothing and accessories', '/images/categories/cat_fashion.png', GETDATE(), 1),
                ('Smart Electronics', 'Latest gadgets and technology', '/images/categories/cat_electronics.png', GETDATE(), 1),
                ('Accessories', 'Premium accessories for every occasion', '/images/categories/cat_accessories.png', GETDATE(), 1),
                ('Lifestyle', 'Premium lifestyle products', '/images/categories/cat_lifestyle.png', GETDATE(), 1),
                ('Footwear', 'Elegant shoes for all occasions', '/images/categories/cat_footwear.png', GETDATE(), 1)
            ");

            // Insert Products - Category 1: Luxury Watches
            migrationBuilder.Sql(@"
                INSERT INTO Products (Name, Description, Price, Stock, CategoryId, ImageUrl, IsFeatured, IsActive, CreatedDate) VALUES
                ('Elegant Gold Watch', 'A stunning gold-plated timepiece with premium craftsmanship. Perfect for formal occasions and luxury events.', 15999, 15, 1, '/images/products/gold_watch.png', 1, 1, GETDATE()),
                ('Classic Silver Timepiece', 'Timeless silver watch with minimalist design. Swiss movement for precise timekeeping.', 12500, 20, 1, '/images/products/silver_watch.png', 1, 1, GETDATE()),
                ('Diamond Accent Watch', 'Luxurious watch featuring diamond accents and premium leather strap.', 22500, 8, 1, '/images/products/diamond_watch.png', 1, 1, GETDATE())
            ");

            // Insert Products - Category 2: Fashion & Style
            migrationBuilder.Sql(@"
                INSERT INTO Products (Name, Description, Price, Stock, CategoryId, ImageUrl, IsFeatured, IsActive, CreatedDate) VALUES
                ('Premium Wool Blazer', 'Elegant wool blazer with perfect fit. Ideal for business meetings and formal events.', 4500, 25, 2, '/images/products/wool_blazer.png', 1, 1, GETDATE()),
                ('Silk Evening Dress', 'Stunning silk dress with elegant design. Perfect for special occasions.', 3800, 18, 2, '/images/products/evening_dress.png', 0, 1, GETDATE()),
                ('Designer Outfit Set', 'Complete outfit set with coordinated pieces for a sophisticated look.', 5500, 12, 2, '/images/products/designer_outfit.png', 1, 1, GETDATE())
            ");

            // Insert Products - Category 3: Smart Electronics
            migrationBuilder.Sql(@"
                INSERT INTO Products (Name, Description, Price, Stock, CategoryId, ImageUrl, IsFeatured, IsActive, CreatedDate) VALUES
                ('Premium Wireless Earbuds', 'High-quality wireless earbuds with noise cancellation and premium sound.', 2800, 50, 3, '/images/products/wireless_earbuds.png', 1, 1, GETDATE()),
                ('Smart Watch Pro', 'Advanced smartwatch with health monitoring and premium design.', 5500, 35, 3, '/images/products/smartwatch_pro.png', 0, 1, GETDATE()),
                ('Portable Speaker', 'Premium portable speaker with exceptional sound quality and elegant design.', 1800, 40, 3, '/images/categories/cat_electronics.png', 0, 1, GETDATE())
            ");

            // Insert Products - Category 4: Accessories
            migrationBuilder.Sql(@"
                INSERT INTO Products (Name, Description, Price, Stock, CategoryId, ImageUrl, IsFeatured, IsActive, CreatedDate) VALUES
                ('Leather Wallet Set', 'Premium leather wallet set with card holder and coin purse.', 1200, 30, 4, '/images/products/leather_wallet.png', 1, 1, GETDATE()),
                ('Designer Sunglasses', 'Stylish sunglasses with premium lenses and elegant frame.', 2200, 25, 4, '/images/products/designer_outfit.png', 0, 1, GETDATE()),
                ('Leather Belt Set', 'Premium leather belt set with classic buckle design.', 850, 45, 4, '/images/products/designer_outfit.png', 0, 1, GETDATE())
            ");

            // Insert Products - Category 5: Lifestyle
            migrationBuilder.Sql(@"
                INSERT INTO Products (Name, Description, Price, Stock, CategoryId, ImageUrl, IsFeatured, IsActive, CreatedDate) VALUES
                ('Premium Yoga Mat', 'High-quality yoga mat with superior grip and comfort.', 950, 60, 5, '/images/products/yoga_mat.png', 1, 1, GETDATE()),
                ('Essential Oil Set', 'Premium set of essential oils for relaxation and wellness.', 1500, 35, 5, '/images/categories/cat_lifestyle.png', 0, 1, GETDATE()),
                ('Home Fragrance Set', 'Elegant home fragrance collection with premium scents.', 1100, 28, 5, '/images/categories/cat_lifestyle.png', 0, 1, GETDATE())
            ");

            // Insert Products - Category 6: Footwear
            migrationBuilder.Sql(@"
                INSERT INTO Products (Name, Description, Price, Stock, CategoryId, ImageUrl, IsFeatured, IsActive, CreatedDate) VALUES
                ('Premium Leather Shoes', 'Handcrafted leather shoes with premium finish. Perfect for formal occasions.', 3500, 22, 6, '/images/products/leather_shoes.png', 1, 1, GETDATE()),
                ('Designer Sneakers', 'Trendy designer sneakers with comfort and style combined.', 2800, 40, 6, '/images/categories/cat_footwear.png', 0, 1, GETDATE()),
                ('Casual Loafers', 'Elegant casual loafers for everyday sophistication.', 1900, 35, 6, '/images/categories/cat_footwear.png', 0, 1, GETDATE())
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM Products");
            migrationBuilder.Sql("DELETE FROM Categories");
        }
    }
}