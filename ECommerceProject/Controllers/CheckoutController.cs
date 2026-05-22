using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using ECommerceProject.Data.Interfaces;
using ECommerceProject.Models.Entities;
using ECommerceProject.Models.Enums;
using ECommerceProject.Models.ViewModels;
using ECommerceProject.Services.Interfaces;

namespace ECommerceProject.Controllers
{
    public class ValidatePromoCodeRequest
    {
        public string Code { get; set; } = string.Empty;
        public decimal OrderTotal { get; set; }
    }

    [Authorize]
    public class CheckoutController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly IPaymentService _paymentService;
        private readonly ILogger<CheckoutController> _logger;

        public CheckoutController(
            IUnitOfWork unitOfWork,
            UserManager<ApplicationUser> userManager,
            IEmailService emailService,
            IPaymentService paymentService,
            ILogger<CheckoutController> logger)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _emailService = emailService;
            _paymentService = paymentService;
            _logger = logger;
        }

        // GET: Checkout
        public async Task<IActionResult> Index(string? promoCode = null)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var cartItems = await _unitOfWork.ShoppingCarts.GetQueryable(asNoTracking: true)
                .Where(c => c.UserId == userId)
                .Include(c => c.Product)
                .ToListAsync();

            if (!cartItems.Any())
            {
                TempData["ErrorMessage"] = "Your cart is empty!";
                return RedirectToAction("Index", "Cart");
            }

            var subtotal = cartItems.Sum(item => item.Product.Price * item.Quantity);
            var tax = subtotal * 0.14m;
            var total = subtotal + tax;

            ViewBag.Subtotal = subtotal;
            ViewBag.Tax = tax;
            ViewBag.Total = total;

            var user = await _userManager.FindByIdAsync(userId);

            var model = new CheckoutViewModel
            {
                FullName = user?.FullName ?? "",
                Email = user?.Email ?? "",
                PhoneNumber = user?.PhoneNumber ?? "",
                Address = user?.Address ?? "",
                City = user?.City ?? "",
                Country = user?.Country ?? "",
                PromoCode = promoCode
            };

            return View(model);
        }

        // POST: Checkout/PlaceOrder
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(CheckoutViewModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            if (!ModelState.IsValid)
            {
                var cartItems = await _unitOfWork.ShoppingCarts.GetAsync(
                    c => c.UserId == userId,
                    c => c.Product);

                var subtotal = cartItems.Sum(item => item.Product.Price * item.Quantity);

                ViewBag.Subtotal = subtotal;
                ViewBag.Tax = subtotal * 0.14m;
                ViewBag.Total = subtotal * 1.14m;

                return View("Index", model);
            }

            try
            {
                await using var transaction = await _unitOfWork.BeginTransactionAsync();

                try
                {
                    var cartItems = await _unitOfWork.ShoppingCarts.GetAsync(
                        c => c.UserId == userId,
                        c => c.Product);

                    if (!cartItems.Any())
                    {
                        TempData["ErrorMessage"] = "Your cart is empty!";
                        return RedirectToAction("Index", "Cart");
                    }

                    var cartItemList = cartItems.ToList();
                    decimal subtotal = 0;
                    var orderItems = new List<OrderItem>();

                    foreach (var cartItem in cartItemList)
                    {
                        var product = cartItem.Product;

                        if (product == null || !product.IsActive)
                            continue;

                        if (product.Stock < cartItem.Quantity)
                        {
                            TempData["ErrorMessage"] = $"{product.Name} is out of stock!";
                            return RedirectToAction("Index", "Cart");
                        }

                        var itemTotal = product.Price * cartItem.Quantity;
                        subtotal += itemTotal;

                        orderItems.Add(new OrderItem
                        {
                            ProductId = product.Id,
                            Quantity = cartItem.Quantity,
                            UnitPrice = product.Price,
                            TotalPrice = itemTotal
                        });

                        product.Stock -= cartItem.Quantity;
                        _unitOfWork.Products.Update(product);
                    }

                    decimal tax = subtotal * 0.14m;
                    decimal totalAmount = subtotal + tax;

                    int? promoCodeId = null;
                    decimal discountAmount = 0;

                    if (!string.IsNullOrWhiteSpace(model.PromoCode))
                    {
                        var promoCode = await _unitOfWork.PromoCodes.GetFirstOrDefaultAsync(
                            p => p.Code.ToUpper() == model.PromoCode.ToUpper() && p.IsActive);

                        if (promoCode != null)
                        {
                            bool isValid = true;

                            if (promoCode.StartDate.HasValue && DateTime.Now < promoCode.StartDate.Value)
                                isValid = false;

                            if (promoCode.EndDate.HasValue && DateTime.Now > promoCode.EndDate.Value)
                                isValid = false;

                            if (promoCode.UsageLimit.HasValue && promoCode.UsageCount >= promoCode.UsageLimit.Value)
                                isValid = false;

                            if (promoCode.MinimumPurchase.HasValue && totalAmount < promoCode.MinimumPurchase.Value)
                                isValid = false;

                            if (isValid)
                            {
                                if (promoCode.DiscountType == DiscountType.Percentage)
                                {
                                    discountAmount = totalAmount * (promoCode.DiscountValue / 100);

                                    if (promoCode.MaximumDiscount.HasValue && discountAmount > promoCode.MaximumDiscount.Value)
                                        discountAmount = promoCode.MaximumDiscount.Value;
                                }
                                else
                                {
                                    discountAmount = promoCode.DiscountValue;
                                }

                                if (discountAmount > totalAmount)
                                    discountAmount = totalAmount;

                                totalAmount -= discountAmount;
                                promoCodeId = promoCode.Id;

                                promoCode.UsageCount++;
                                _unitOfWork.PromoCodes.Update(promoCode);

                                TempData["SuccessMessage"] = $"Promo code applied! You saved ${discountAmount:F2}";
                            }
                            else
                            {
                                TempData["InfoMessage"] = "Promo code could not be applied.";
                            }
                        }
                        else
                        {
                            TempData["InfoMessage"] = "Invalid promo code.";
                        }
                    }

                    var order = new Order
                    {
                        UserId = userId,
                        OrderDate = DateTime.Now,
                        TotalAmount = totalAmount,
                        Status = OrderStatus.Pending,
                        PaymentMethod = model.PaymentMethod,
                        ShippingAddress = model.Address,
                        City = model.City,
                        State = model.State,
                        ZipCode = model.ZipCode,
                        Country = model.Country,
                        PhoneNumber = model.PhoneNumber,
                        Notes = model.Notes ?? string.Empty,
                        PromoCodeId = promoCodeId,
                        DiscountAmount = discountAmount,
                        OrderItems = orderItems
                    };

                    await _unitOfWork.Orders.AddAsync(order);
                    await _unitOfWork.SaveAsync();

                    var payment = new Payment
                    {
                        OrderId = order.Id,
                        Amount = totalAmount,
                        PaymentDate = DateTime.Now,
                        PaymentMethod = model.PaymentMethod,
                        Status = PaymentStatus.Pending,
                        TransactionId = $"PENDING-{order.Id}-{DateTime.Now.Ticks}"
                    };

                    await _unitOfWork.Payments.AddAsync(payment);
                    await _unitOfWork.SaveAsync();

                    if (model.PaymentMethod == PaymentMethod.Stripe ||
                        model.PaymentMethod == PaymentMethod.CreditCard)
                    {
                        try
                        {
                            var productNames = cartItemList
                                .Select(ci => ci.Product.Name)
                                .ToList();

                            var checkoutUrl = await _paymentService.CreateCheckoutSessionAsync(
                                order.Id,
                                totalAmount,
                                productNames);

                            _unitOfWork.ShoppingCarts.DeleteRange(cartItems);
                            await _unitOfWork.SaveAsync();

                            await transaction.CommitAsync();
                            return Redirect(checkoutUrl);
                        }
                        catch (Exception ex)
                        {
                            await transaction.RollbackAsync();
                            _logger.LogError(ex, "Stripe payment error");
                            TempData["ErrorMessage"] = "Payment gateway error. Please try again.";
                            return RedirectToAction("Index");
                        }
                    }

                    payment.Status = PaymentStatus.Pending;
                    _unitOfWork.Payments.Update(payment);
                    await _unitOfWork.SaveAsync();

                    _unitOfWork.ShoppingCarts.DeleteRange(cartItems);
                    await _unitOfWork.SaveAsync();

                    await transaction.CommitAsync();

                    TempData["SuccessMessage"] = "Order placed successfully!";

                    try
                    {
                        var user = await _userManager.FindByIdAsync(userId);
                        if (user != null)
                        {
                            await _emailService.SendOrderConfirmationEmailAsync(
                                user.Email!,
                                user.FullName,
                                order.Id,
                                totalAmount);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to send order confirmation email");
                    }

                    return RedirectToAction("OrderConfirmation", new { orderId = order.Id });
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error placing order for user {UserId}", userId);
                TempData["ErrorMessage"] = "An error occurred while placing your order. Please try again.";
                return RedirectToAction("Index");
            }
        }

        // GET: Checkout/OrderConfirmation
        public async Task<IActionResult> OrderConfirmation(int orderId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var order = await _unitOfWork.Orders.GetByIdAsync(orderId);

            if (order == null || order.UserId != userId)
            {
                return NotFound();
            }

            var orderItems = await _unitOfWork.OrderItems.GetAsync(
                oi => oi.OrderId == orderId,
                oi => oi.Product);

            ViewBag.OrderItems = orderItems.Select(oi => (oi, oi.Product)).ToList();

            var payment = await _unitOfWork.Payments.GetFirstOrDefaultAsync(p => p.OrderId == orderId);
            ViewBag.Payment = payment;

            return View(order);
        }

        // GET: Checkout/PaymentSuccess
        public async Task<IActionResult> PaymentSuccess(int orderId)
        {
            try
            {
                var order = await _unitOfWork.Orders.GetByIdAsync(orderId);
                if (order == null)
                {
                    return NotFound();
                }

                var payment = await _unitOfWork.Payments.GetFirstOrDefaultAsync(p => p.OrderId == orderId);
                if (payment != null)
                {
                    payment.Status = PaymentStatus.Completed;
                    payment.PaymentDate = DateTime.Now;
                    _unitOfWork.Payments.Update(payment);
                    await _unitOfWork.SaveAsync();
                }

                order.Status = OrderStatus.Paid;
                _unitOfWork.Orders.Update(order);
                await _unitOfWork.SaveAsync();

                TempData["SuccessMessage"] = "Payment successful! Your order has been confirmed.";

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var user = await _userManager.FindByIdAsync(userId);
                if (user != null)
                {
                    try
                    {
                        await _emailService.SendOrderConfirmationEmailAsync(
                            user.Email!,
                            user.FullName,
                            order.Id,
                            order.TotalAmount);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to send order confirmation email for PaymentSuccess");
                    }
                }

                return RedirectToAction("OrderConfirmation", new { orderId = orderId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Payment success processing error for order {OrderId}", orderId);
                TempData["ErrorMessage"] = "An error occurred while processing your payment.";
                return RedirectToAction("Index", "Cart");
            }
        }

        // GET: Checkout/PaymentCancelled
        public async Task<IActionResult> PaymentCancelled(int orderId)
        {
            try
            {
                var order = await _unitOfWork.Orders.GetByIdAsync(orderId);
                if (order != null)
                {
                    var payment = await _unitOfWork.Payments.GetFirstOrDefaultAsync(p => p.OrderId == orderId);
                    if (payment != null)
                    {
                        payment.Status = PaymentStatus.Failed;
                        _unitOfWork.Payments.Update(payment);
                    }

                    order.Status = OrderStatus.Cancelled;
                    _unitOfWork.Orders.Update(order);
                    await _unitOfWork.SaveAsync();
                }

                TempData["ErrorMessage"] = "Payment was cancelled. Please try again.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Payment cancellation error for order {OrderId}", orderId);
                return RedirectToAction("Index", "Cart");
            }
        }

        // POST: Checkout/ValidatePromoCode
        [HttpPost]
        public async Task<IActionResult> ValidatePromoCode([FromBody] ValidatePromoCodeRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Code))
                {
                    return Json(new { success = false, message = "Please enter a promo code." });
                }

                var promoCode = await _unitOfWork.PromoCodes.GetFirstOrDefaultAsync(
                    p => p.Code.ToUpper() == request.Code.ToUpper());

                if (promoCode == null)
                {
                    return Json(new { success = false, message = "Invalid promo code." });
                }

                if (!promoCode.IsActive)
                {
                    return Json(new { success = false, message = "This promo code is no longer active." });
                }

                if (promoCode.StartDate.HasValue && DateTime.Now < promoCode.StartDate.Value)
                {
                    return Json(new { success = false, message = "This promo code is not yet valid." });
                }

                if (promoCode.EndDate.HasValue && DateTime.Now > promoCode.EndDate.Value)
                {
                    return Json(new { success = false, message = "This promo code has expired." });
                }

                if (promoCode.UsageLimit.HasValue && promoCode.UsageCount >= promoCode.UsageLimit.Value)
                {
                    return Json(new { success = false, message = "This promo code has reached its usage limit." });
                }

                if (promoCode.MinimumPurchase.HasValue && request.OrderTotal < promoCode.MinimumPurchase.Value)
                {
                    return Json(new
                    {
                        success = false,
                        message = $"Minimum purchase of {promoCode.MinimumPurchase.Value:C} required for this promo code."
                    });
                }

                decimal discountAmount = 0;

                if (promoCode.DiscountType == DiscountType.Percentage)
                {
                    discountAmount = request.OrderTotal * (promoCode.DiscountValue / 100);

                    if (promoCode.MaximumDiscount.HasValue && discountAmount > promoCode.MaximumDiscount.Value)
                    {
                        discountAmount = promoCode.MaximumDiscount.Value;
                    }
                }
                else
                {
                    discountAmount = promoCode.DiscountValue;
                }

                if (discountAmount > request.OrderTotal)
                {
                    discountAmount = request.OrderTotal;
                }

                var newTotal = request.OrderTotal - discountAmount;

                return Json(new
                {
                    success = true,
                    discountAmount = discountAmount,
                    newTotal = newTotal,
                    message = $"Promo code applied! You saved {discountAmount:C}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Promo code validation error");
                return Json(new { success = false, message = "An error occurred while validating the promo code." });
            }
        }
    }
}