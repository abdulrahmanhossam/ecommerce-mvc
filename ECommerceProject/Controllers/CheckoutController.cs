using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ECommerceProject.Data.Interfaces;
using ECommerceProject.Models.Entities;
using ECommerceProject.Models.Enums;
using ECommerceProject.Models.ViewModels;
using ECommerceProject.Services.Interfaces;

namespace ECommerceProject.Controllers
{
    [Authorize]
    public class CheckoutController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly IPaymentService _paymentService;

        public CheckoutController(
            IUnitOfWork unitOfWork,
            UserManager<ApplicationUser> userManager,
            IEmailService emailService,
            IPaymentService paymentService)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _emailService = emailService;
            _paymentService = paymentService;
        }

        // GET: Checkout
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var cartItems = await _unitOfWork.ShoppingCarts.GetAsync(c => c.UserId == userId);

            if (!cartItems.Any())
            {
                TempData["ErrorMessage"] = "Your cart is empty!";
                return RedirectToAction("Index", "Cart");
            }

            decimal subtotal = 0;
            foreach (var item in cartItems)
            {
                var product = await _unitOfWork.Products.GetByIdAsync(item.ProductId);
                if (product != null)
                {
                    subtotal += product.Price * item.Quantity;
                }
            }

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
                Country = user?.Country ?? ""
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
                var cartItems = await _unitOfWork.ShoppingCarts.GetAsync(c => c.UserId == userId);

                decimal subtotal = 0;
                foreach (var item in cartItems)
                {
                    var product = await _unitOfWork.Products.GetByIdAsync(item.ProductId);
                    if (product != null)
                    {
                        subtotal += product.Price * item.Quantity;
                    }
                }

                ViewBag.Subtotal = subtotal;
                ViewBag.Tax = subtotal * 0.14m;
                ViewBag.Total = subtotal * 1.14m;

                return View("Index", model);
            }

            try
            {
                var cartItems = await _unitOfWork.ShoppingCarts.GetAsync(c => c.UserId == userId);

                if (!cartItems.Any())
                {
                    TempData["ErrorMessage"] = "Your cart is empty!";
                    return RedirectToAction("Index", "Cart");
                }

                decimal subtotal = 0;
                var orderItems = new List<OrderItem>();

                foreach (var cartItem in cartItems)
                {
                    var product = await _unitOfWork.Products.GetByIdAsync(cartItem.ProductId);

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

                // حساب Tax
                decimal tax = subtotal * 0.14m;
                decimal totalAmount = subtotal + tax;

                // معالجة PromoCode
                int? promoCodeId = null;
                decimal discountAmount = 0;

                Console.WriteLine($"PromoCode from form: '{model.PromoCode}'");

                if (!string.IsNullOrWhiteSpace(model.PromoCode))
                {
                    var promoCode = await _unitOfWork.PromoCodes.GetFirstOrDefaultAsync(
                        p => p.Code.ToUpper() == model.PromoCode.ToUpper() && p.IsActive);

                    if (promoCode != null)
                    {
                        Console.WriteLine($"PromoCode found: {promoCode.Code}");

                        bool isValid = true;

                        if (promoCode.StartDate.HasValue && DateTime.Now < promoCode.StartDate.Value)
                        {
                            Console.WriteLine("PromoCode not yet valid");
                            isValid = false;
                        }

                        if (promoCode.EndDate.HasValue && DateTime.Now > promoCode.EndDate.Value)
                        {
                            Console.WriteLine("PromoCode expired");
                            isValid = false;
                        }

                        if (promoCode.UsageLimit.HasValue && promoCode.UsageCount >= promoCode.UsageLimit.Value)
                        {
                            Console.WriteLine("PromoCode usage limit reached");
                            isValid = false;
                        }

                        if (promoCode.MinimumPurchase.HasValue && totalAmount < promoCode.MinimumPurchase.Value)
                        {
                            Console.WriteLine($"Minimum purchase not met: {totalAmount} < {promoCode.MinimumPurchase.Value}");
                            isValid = false;
                        }

                        if (isValid)
                        {
                            if (promoCode.DiscountType == DiscountType.Percentage)
                            {
                                discountAmount = totalAmount * (promoCode.DiscountValue / 100);

                                if (promoCode.MaximumDiscount.HasValue && discountAmount > promoCode.MaximumDiscount.Value)
                                {
                                    discountAmount = promoCode.MaximumDiscount.Value;
                                }

                                Console.WriteLine($"Percentage discount: {promoCode.DiscountValue}% = ${discountAmount}");
                            }
                            else
                            {
                                discountAmount = promoCode.DiscountValue;
                                Console.WriteLine($"Fixed discount: ${discountAmount}");
                            }

                            if (discountAmount > totalAmount)
                            {
                                discountAmount = totalAmount;
                            }

                            totalAmount -= discountAmount;
                            promoCodeId = promoCode.Id;

                            promoCode.UsageCount++;
                            _unitOfWork.PromoCodes.Update(promoCode);

                            Console.WriteLine($"Final total after discount: ${totalAmount}");
                            TempData["SuccessMessage"] = $"Promo code applied! You saved ${discountAmount:F2}";
                        }
                        else
                        {
                            TempData["InfoMessage"] = "Promo code could not be applied.";
                        }
                    }
                    else
                    {
                        Console.WriteLine("PromoCode not found or inactive");
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
                        var productNames = new List<string>();
                        foreach (var item in orderItems)
                        {
                            var product = await _unitOfWork.Products.GetByIdAsync(item.ProductId);
                            if (product != null)
                            {
                                productNames.Add(product.Name);
                            }
                        }

                        var checkoutUrl = await _paymentService.CreateCheckoutSessionAsync(
                            order.Id,
                            totalAmount,
                            productNames);

                        _unitOfWork.ShoppingCarts.DeleteRange(cartItems);
                        await _unitOfWork.SaveAsync();

                        return Redirect(checkoutUrl);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Stripe Error: {ex.Message}");
                        TempData["ErrorMessage"] = "Payment gateway error. Please try again.";
                        return RedirectToAction("Index");
                    }
                }
                else
                {
                    payment.Status = PaymentStatus.Pending;
                    _unitOfWork.Payments.Update(payment);
                    await _unitOfWork.SaveAsync();

                    _unitOfWork.ShoppingCarts.DeleteRange(cartItems);
                    await _unitOfWork.SaveAsync();

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
                        Console.WriteLine($"Email Error: {ex.Message}");
                    }

                    return RedirectToAction("OrderConfirmation", new { orderId = order.Id });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
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

            var orderItems = await _unitOfWork.OrderItems.GetAsync(oi => oi.OrderId == orderId);
            var orderItemsWithProducts = new List<(OrderItem Item, Product Product)>();

            foreach (var item in orderItems)
            {
                var product = await _unitOfWork.Products.GetByIdAsync(item.ProductId);
                if (product != null)
                {
                    orderItemsWithProducts.Add((item, product));
                }
            }

            ViewBag.OrderItems = orderItemsWithProducts;

            var payment = await _unitOfWork.Payments.GetFirstOrDefaultAsync(p => p.OrderId == orderId);
            ViewBag.Payment = payment;

            return View(order);
        }

        // POST: Checkout/ValidatePromoCode
        [HttpPost]
        public async Task<IActionResult> ValidatePromoCode(string code, decimal orderTotal)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(code))
                {
                    return Json(new { success = false, message = "Please enter a promo code." });
                }

                var promoCode = await _unitOfWork.PromoCodes.GetFirstOrDefaultAsync(
                    p => p.Code.ToUpper() == code.ToUpper());

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

                if (promoCode.MinimumPurchase.HasValue && orderTotal < promoCode.MinimumPurchase.Value)
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
                    discountAmount = orderTotal * (promoCode.DiscountValue / 100);

                    if (promoCode.MaximumDiscount.HasValue && discountAmount > promoCode.MaximumDiscount.Value)
                    {
                        discountAmount = promoCode.MaximumDiscount.Value;
                    }
                }
                else
                {
                    discountAmount = promoCode.DiscountValue;
                }

                if (discountAmount > orderTotal)
                {
                    discountAmount = orderTotal;
                }

                var newTotal = orderTotal - discountAmount;

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
                Console.WriteLine($"Error: {ex.Message}");
                return Json(new { success = false, message = "An error occurred while validating the promo code." });
            }
        }
    }
}