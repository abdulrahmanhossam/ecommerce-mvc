using ECommerceProject.Models.ViewModels;

namespace ECommerceProject.Services.Interfaces
{
    public interface IAnalyticsService
    {
        Task<SalesAnalyticsViewModel> GetSalesAnalyticsAsync();
        Task<List<DailySalesData>> GetDailySalesAsync(int days = 30);
        Task<List<MonthlySalesData>> GetMonthlySalesAsync(int months = 12);
    }
}