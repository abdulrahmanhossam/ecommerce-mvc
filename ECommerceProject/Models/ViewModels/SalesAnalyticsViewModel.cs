namespace ECommerceProject.Models.ViewModels
{
    public class SalesAnalyticsViewModel
    {
        // Overview Stats
        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
        public int TotalCustomers { get; set; }
        public decimal AverageOrderValue { get; set; }

        // Period Comparison
        public decimal RevenueThisMonth { get; set; }
        public decimal RevenueLastMonth { get; set; }
        public decimal RevenueGrowthPercentage { get; set; }

        public int OrdersThisMonth { get; set; }
        public int OrdersLastMonth { get; set; }
        public int OrdersGrowthPercentage { get; set; }

        // Sales by Status
        public int PendingOrders { get; set; }
        public int PaidOrders { get; set; }
        public int ShippedOrders { get; set; }
        public int DeliveredOrders { get; set; }
        public int CancelledOrders { get; set; }

        // Top Products
        public List<ProductSalesData> TopSellingProducts { get; set; } = new();

        // Top Customers
        public List<CustomerSalesData> TopCustomers { get; set; } = new();

        // Category Performance
        public List<CategorySalesData> CategorySales { get; set; } = new();

        // Daily Sales (Last 30 Days)
        public List<DailySalesData> DailySales { get; set; } = new();

        // Monthly Sales (Last 12 Months)
        public List<MonthlySalesData> MonthlySales { get; set; } = new();
    }

    public class ProductSalesData
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public int QuantitySold { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class CustomerSalesData
    {
        public string UserId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int TotalOrders { get; set; }
        public decimal TotalSpent { get; set; }
    }

    public class CategorySalesData
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public int ProductsSold { get; set; }
        public decimal Revenue { get; set; }
    }

    public class DailySalesData
    {
        public DateTime Date { get; set; }
        public int OrderCount { get; set; }
        public decimal Revenue { get; set; }
    }

    public class MonthlySalesData
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string MonthName { get; set; } = string.Empty;
        public int OrderCount { get; set; }
        public decimal Revenue { get; set; }
    }
}