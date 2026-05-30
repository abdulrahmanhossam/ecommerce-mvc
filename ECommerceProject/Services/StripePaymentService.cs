using Stripe;
using Stripe.Checkout;
using ECommerceProject.Services.Interfaces;

namespace ECommerceProject.Services
{
    public class StripePaymentService : IPaymentService
    {
        private readonly string _secretKey;
        private readonly string _domain;
        private readonly ILogger<StripePaymentService> _logger;

        public StripePaymentService(IConfiguration configuration, ILogger<StripePaymentService> logger)
        {
            _secretKey = configuration["Stripe:SecretKey"] ?? throw new ArgumentNullException("Stripe SecretKey");
            _domain = configuration["Stripe:Domain"] ?? throw new ArgumentNullException("Stripe Domain");
            _logger = logger;
            StripeConfiguration.ApiKey = _secretKey;
        }

        public async Task<string> CreateCheckoutSessionAsync(int orderId, decimal amount, List<string> productNames)
        {
            try
            {
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
                    SuccessUrl = $"{_domain}/Checkout/PaymentSuccess?orderId={orderId}",
                    CancelUrl = $"{_domain}/Checkout/PaymentCancelled?orderId={orderId}",
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