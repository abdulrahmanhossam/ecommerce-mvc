using Stripe;
using Stripe.Checkout;
using Microsoft.Extensions.Options;
using ECommerceProject.Services.Interfaces;

namespace ECommerceProject.Services
{
    public class StripePaymentService : IPaymentService
    {
        private readonly string _secretKey;
        private readonly ILogger<StripePaymentService> _logger;

        public StripePaymentService(IConfiguration configuration, ILogger<StripePaymentService> logger)
        {
            _secretKey = configuration["Stripe:SecretKey"] ?? throw new ArgumentNullException("Stripe SecretKey");
            _logger = logger;
            StripeConfiguration.ApiKey = _secretKey;
        }

        public async Task<string> CreatePaymentIntentAsync(decimal amount, string currency = "usd")
        {
            try
            {
                var options = new PaymentIntentCreateOptions
                {
                    Amount = (long)(amount * 100), // Stripe يستخدم cents
                    Currency = currency,
                    PaymentMethodTypes = new List<string> { "card" },
                };

                var service = new PaymentIntentService();
                var paymentIntent = await service.CreateAsync(options);

                return paymentIntent.ClientSecret;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Stripe Error: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> ConfirmPaymentAsync(string paymentIntentId)
        {
            try
            {
                var service = new PaymentIntentService();
                var paymentIntent = await service.GetAsync(paymentIntentId);

                return paymentIntent.Status == "succeeded";
            }
            catch (Exception ex)
            {
                _logger.LogError($"Stripe Error: {ex.Message}");
                return false;
            }
        }

        public async Task<string> CreateCheckoutSessionAsync(int orderId, decimal amount, List<string> productNames)
        {
            try
            {
                var domain = "https://localhost:7000"; // غيره لدومين موقعك

                var lineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = "usd",
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = $"Order #{orderId}",
                                Description = string.Join(", ", productNames.Take(3))
                            },
                            UnitAmount = (long)(amount * 100),
                        },
                        Quantity = 1,
                    }
                };

                var options = new SessionCreateOptions
                {
                    PaymentMethodTypes = new List<string> { "card" },
                    LineItems = lineItems,
                    Mode = "payment",
                    SuccessUrl = $"{domain}/Checkout/PaymentSuccess?orderId={orderId}",
                    CancelUrl = $"{domain}/Checkout/PaymentCancelled?orderId={orderId}",
                    Metadata = new Dictionary<string, string>
                    {
                        { "order_id", orderId.ToString() }
                    }
                };

                var service = new SessionService();
                var session = await service.CreateAsync(options);

                return session.Url; // URL للدفع
            }
            catch (Exception ex)
            {
                _logger.LogError($"Stripe Checkout Error: {ex.Message}");
                throw;
            }
        }
    }
}