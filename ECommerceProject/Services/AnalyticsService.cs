using ECommerceProject.Data.Interfaces;
using ECommerceProject.Models.Enums;
using ECommerceProject.Models.ViewModels;
using ECommerceProject.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ECommerceProject.Models.Entities;

namespace ECommerceProject.Services
{
    public class AnalyticsService : IAnalyticsService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;

        public AnalyticsService(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        public async Task<SalesAnalyticsViewModel> GetSalesAnalyticsAsync()
        {
            var model = new SalesAnalyticsViewModel();

            // Get all orders and items
            var ordersList = await _unitOfWork.Orders.GetQueryable(asNoTracking: true)
                .ToListAsync();

            var orderItemsList = await _unitOfWork.OrderItems.GetQueryable(asNoTracking: true)
                .ToListAsync();

            // Preload reference data into dictionaries to avoid N+1 queries
            var productsDict = (await _unitOfWork.Products.GetQueryable(asNoTracking: true)
                .ToListAsync())
                .ToDictionary(p => p.Id);

            var categoriesDict = (await _unitOfWork.Categories.GetQueryable(asNoTracking: true)
                .ToListAsync())
                .ToDictionary(c => c.Id);

            // Overview Stats
            model.TotalOrders = ordersList.Count;
            model.TotalRevenue = ordersList.Sum(o => o.TotalAmount);
            model.AverageOrderValue = model.TotalOrders > 0 ? model.TotalRevenue / model.TotalOrders : 0;

            var customerRole = await _userManager.GetUsersInRoleAsync("Customer");
            model.TotalCustomers = customerRole.Count;

            // This Month vs Last Month
            var now = DateTime.Now;
            var thisMonthStart = new DateTime(now.Year, now.Month, 1);
            var lastMonthStart = thisMonthStart.AddMonths(-1);
            var lastMonthEnd = thisMonthStart.AddDays(-1);

            var thisMonthOrders = ordersList.Where(o => o.OrderDate >= thisMonthStart).ToList();
            var lastMonthOrders = ordersList.Where(o => o.OrderDate >= lastMonthStart && o.OrderDate <= lastMonthEnd).ToList();

            model.OrdersThisMonth = thisMonthOrders.Count;
            model.OrdersLastMonth = lastMonthOrders.Count;
            model.OrdersGrowthPercentage = lastMonthOrders.Count > 0
                ? (int)(((model.OrdersThisMonth - model.OrdersLastMonth) / (decimal)model.OrdersLastMonth) * 100)
                : 100;

            model.RevenueThisMonth = thisMonthOrders.Sum(o => o.TotalAmount);
            model.RevenueLastMonth = lastMonthOrders.Sum(o => o.TotalAmount);
            model.RevenueGrowthPercentage = model.RevenueLastMonth > 0
                ? (model.RevenueThisMonth - model.RevenueLastMonth) / model.RevenueLastMonth * 100
                : 100;

            // Orders by Status
            model.PendingOrders = ordersList.Count(o => o.Status == OrderStatus.Pending);
            model.PaidOrders = ordersList.Count(o => o.Status == OrderStatus.Paid);
            model.ShippedOrders = ordersList.Count(o => o.Status == OrderStatus.Shipped);
            model.DeliveredOrders = ordersList.Count(o => o.Status == OrderStatus.Delivered);
            model.CancelledOrders = ordersList.Count(o => o.Status == OrderStatus.Cancelled);

            // Top Selling Products
            var productSales = orderItemsList
                .GroupBy(oi => oi.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    QuantitySold = g.Sum(oi => oi.Quantity),
                    TotalRevenue = g.Sum(oi => oi.TotalPrice)
                })
                .OrderByDescending(x => x.QuantitySold)
                .Take(10);

            foreach (var ps in productSales)
            {
                if (productsDict.TryGetValue(ps.ProductId, out var product))
                {
                    model.TopSellingProducts.Add(new ProductSalesData
                    {
                        ProductId = ps.ProductId,
                        ProductName = product.Name,
                        ImageUrl = product.ImageUrl,
                        QuantitySold = ps.QuantitySold,
                        TotalRevenue = ps.TotalRevenue
                    });
                }
            }

            // Top Customers
            var usersDict = (await _unitOfWork.Users.GetQueryable(asNoTracking: true)
                .ToListAsync())
                .ToDictionary(u => u.Id);

            var customerSales = ordersList
                .GroupBy(o => o.UserId)
                .Select(g => new
                {
                    UserId = g.Key,
                    TotalOrders = g.Count(),
                    TotalSpent = g.Sum(o => o.TotalAmount)
                })
                .OrderByDescending(x => x.TotalSpent)
                .Take(10);

            foreach (var cs in customerSales)
            {
                if (usersDict.TryGetValue(cs.UserId, out var user))
                {
                    model.TopCustomers.Add(new CustomerSalesData
                    {
                        UserId = cs.UserId,
                        CustomerName = user.FullName,
                        Email = user.Email!,
                        TotalOrders = cs.TotalOrders,
                        TotalSpent = cs.TotalSpent
                    });
                }
            }

            // Category Performance
            var categorySales = orderItemsList
                .Join(productsDict.Values, oi => oi.ProductId, p => p.Id, (oi, p) => new { oi, p })
                .GroupBy(x => x.p.CategoryId)
                .Select(g => new
                {
                    CategoryId = g.Key,
                    ProductsSold = g.Sum(x => x.oi.Quantity),
                    Revenue = g.Sum(x => x.oi.TotalPrice)
                });

            foreach (var cs in categorySales)
            {
                if (categoriesDict.TryGetValue(cs.CategoryId, out var category))
                {
                    model.CategorySales.Add(new CategorySalesData
                    {
                        CategoryId = cs.CategoryId,
                        CategoryName = category.Name,
                        ProductsSold = cs.ProductsSold,
                        Revenue = cs.Revenue
                    });
                }
            }

            // Daily Sales (Last 30 Days)
            model.DailySales = await GetDailySalesAsync(30);

            // Monthly Sales (Last 12 Months)
            model.MonthlySales = await GetMonthlySalesAsync(12);

            return model;
        }

        public async Task<List<DailySalesData>> GetDailySalesAsync(int days = 30)
        {
            var result = new List<DailySalesData>();
            var startDate = DateTime.Now.Date.AddDays(-days);

            var orders = await _unitOfWork.Orders.GetQueryable(asNoTracking: true)
                .Where(o => o.OrderDate >= startDate)
                .ToListAsync();

            var dailyData = orders
                .GroupBy(o => o.OrderDate.Date)
                .Select(g => new DailySalesData
                {
                    Date = g.Key,
                    OrderCount = g.Count(),
                    Revenue = g.Sum(o => o.TotalAmount)
                })
                .OrderBy(d => d.Date)
                .ToList();

            // Fill missing days with zero
            for (int i = 0; i < days; i++)
            {
                var date = startDate.AddDays(i);
                var existing = dailyData.FirstOrDefault(d => d.Date == date);

                if (existing != null)
                {
                    result.Add(existing);
                }
                else
                {
                    result.Add(new DailySalesData
                    {
                        Date = date,
                        OrderCount = 0,
                        Revenue = 0
                    });
                }
            }

            return result;
        }

        public async Task<List<MonthlySalesData>> GetMonthlySalesAsync(int months = 12)
        {
            var result = new List<MonthlySalesData>();
            var startDate = DateTime.Now.AddMonths(-months);

            var orders = await _unitOfWork.Orders.GetQueryable(asNoTracking: true)
                .Where(o => o.OrderDate >= startDate)
                .ToListAsync();

            var monthlyData = orders
                .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month })
                .Select(g => new MonthlySalesData
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    MonthName = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy"),
                    OrderCount = g.Count(),
                    Revenue = g.Sum(o => o.TotalAmount)
                })
                .OrderBy(m => m.Year)
                .ThenBy(m => m.Month)
                .ToList();

            return monthlyData;
        }
    }
}