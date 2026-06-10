using Stripe;
using Stripe.Checkout;
using ECommerceProject.Services.Interfaces;

namespace ECommerceProject.Services
{
    public class StripePaymentService : IPaymentService
    {
        private static bool _stripeConfigured;
        private static readonly object _stripeLock = new();
        private readonly string _secretKey;
        private readonly string _domain;
        private readonly ILogger<StripePaymentService> _logger;

        public StripePaymentService(IConfiguration configuration, ILogger<StripePaymentService> logger)
        {
            _secretKey = configuration["Stripe:SecretKey"] ?? throw new ArgumentNullException(nameof(configuration), "Stripe SecretKey not configured");
            _domain = configuration["Stripe:Domain"] ?? throw new ArgumentNullException(nameof(configuration), "Stripe Domain not configured");
            _logger = logger;

            if (!_stripeConfigured)
            {
                lock (_stripeLock)
                {
                    if (!_stripeConfigured)
                    {
                        StripeConfiguration.ApiKey = _secretKey;
                        _stripeConfigured = true;
                    }
                }
            }
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

                return session.Url ?? throw new InvalidOperationException("Stripe returned a session without a checkout URL.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Stripe Checkout Error for order {OrderId}", orderId);
                throw;
            }
        }
    }
}