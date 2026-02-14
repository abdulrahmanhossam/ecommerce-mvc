namespace ECommerceProject.Services.Interfaces
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string body);
        Task SendOrderConfirmationEmailAsync(string toEmail, string customerName, int orderId, decimal totalAmount);
        Task SendPasswordResetEmailAsync(string toEmail, string resetLink);
        Task SendRegistrationConfirmationEmailAsync(string toEmail, string customerName);
    }
}