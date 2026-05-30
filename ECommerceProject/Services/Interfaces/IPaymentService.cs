namespace ECommerceProject.Services.Interfaces;

public interface IPaymentService
{
    Task<string> CreateCheckoutSessionAsync(int orderId, decimal amount, List<string> productNames);
}