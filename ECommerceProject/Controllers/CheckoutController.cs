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
        private readonly ILogger<CheckoutController> _logger;
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly IPaymentService _paymentService;

        public CheckoutController(
            IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager, IEmailService emailService,
            ILogger<CheckoutController> logger, IPaymentService paymentService)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _emailService = emailService;
            _logger = logger;
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

            // التحقق من وجود منتجات في العربة
            var cartItems = await _unitOfWork.ShoppingCarts.GetAsync(c => c.UserId == userId);

            if (!cartItems.Any())
            {
                TempData["ErrorMessage"] = "Your cart is empty!";
                return RedirectToAction("Index", "Cart");
            }

            // حساب الإجمالي
            decimal subtotal = 0;
            foreach (var item in cartItems)
            {
                var product = await _unitOfWork.Products.GetByIdAsync(item.ProductId);
                if (product != null)
                {
                    subtotal += product.Price * item.Quantity;
                }
            }

            var tax = subtotal * 0.14m; // ضريبة 14%
            var total = subtotal + tax;

            ViewBag.Subtotal = subtotal;
            ViewBag.Tax = tax;
            ViewBag.Total = total;

            // استخدام UserManager بدل Repository
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
                // جلب العربة
                var cartItems = await _unitOfWork.ShoppingCarts.GetAsync(c => c.UserId == userId);

                if (!cartItems.Any())
                {
                    TempData["ErrorMessage"] = "Your cart is empty!";
                    return RedirectToAction("Index", "Cart");
                }

                // حساب الإجمالي
                decimal totalAmount = 0;
                var orderItems = new List<OrderItem>();

                foreach (var cartItem in cartItems)
                {
                    var product = await _unitOfWork.Products.GetByIdAsync(cartItem.ProductId);

                    if (product == null || !product.IsActive)
                        continue;

                    // التحقق من المخزون
                    if (product.Stock < cartItem.Quantity)
                    {
                        TempData["ErrorMessage"] = $"{product.Name} is out of stock!";
                        return RedirectToAction("Index", "Cart");
                    }

                    var itemTotal = product.Price * cartItem.Quantity;
                    totalAmount += itemTotal;

                    orderItems.Add(new OrderItem
                    {
                        ProductId = product.Id,
                        Quantity = cartItem.Quantity,
                        UnitPrice = product.Price,
                        TotalPrice = itemTotal
                    });

                    // تقليل المخزون
                    product.Stock -= cartItem.Quantity;
                    _unitOfWork.Products.Update(product);
                }

                // إضافة الضريبة
                totalAmount = totalAmount * 1.14m;

                // إنشاء الطلب
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
                    OrderItems = orderItems
                };

                await _unitOfWork.Orders.AddAsync(order);
                await _unitOfWork.SaveAsync();

                // إنشاء Payment Record
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

                // لو اختار Credit Card أو Stripe
                if (model.PaymentMethod == PaymentMethod.Stripe ||
                    model.PaymentMethod == PaymentMethod.CreditCard)
                {
                    try
                    {
                        // جمع أسماء المنتجات
                        var productNames = new List<string>();
                        foreach (var item in orderItems)
                        {
                            var product = await _unitOfWork.Products.GetByIdAsync(item.ProductId);
                            if (product != null)
                            {
                                productNames.Add(product.Name);
                            }
                        }

                        // إنشاء Stripe Checkout Session
                        var checkoutUrl = await _paymentService.CreateCheckoutSessionAsync(
                            order.Id,
                            totalAmount,
                            productNames);

                        // حذف العربة
                        _unitOfWork.ShoppingCarts.DeleteRange(cartItems);
                        await _unitOfWork.SaveAsync();

                        // Redirect للدفع على Stripe
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
                    // Cash on Delivery أو PayPal
                    payment.Status = PaymentStatus.Pending;
                    _unitOfWork.Payments.Update(payment);
                    await _unitOfWork.SaveAsync();

                    // حذف العربة
                    _unitOfWork.ShoppingCarts.DeleteRange(cartItems);
                    await _unitOfWork.SaveAsync();

                    TempData["SuccessMessage"] = "Order placed successfully!";

                    // إرسال Email
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
                        // لا نوقف العملية لو فشل الإيميل
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

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var order = await _unitOfWork.Orders.GetByIdAsync(orderId);

            if (order == null || order.UserId != userId)
            {
                return NotFound();
            }

            // جلب Order Items
            var orderItems = await _unitOfWork.OrderItems.GetAsync(oi => oi.OrderId == orderId);
            ViewBag.OrderItems = orderItems.ToList();

            // جلب Payment
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
                    return NotFound();

                // تحديث حالة الدفع
                var payment = await _unitOfWork.Payments.GetFirstOrDefaultAsync(p => p.OrderId == orderId);

                if (payment != null)
                {
                    payment.Status = PaymentStatus.Completed;
                    payment.PaymentDate = DateTime.Now;
                    payment.TransactionId = $"STRIPE-{orderId}-{DateTime.Now.Ticks}";
                    _unitOfWork.Payments.Update(payment);
                }

                // تحديث حالة الأوردر
                order.Status = OrderStatus.Paid;
                _unitOfWork.Orders.Update(order);

                await _unitOfWork.SaveAsync();

                // إرسال Email
                try
                {
                    var user = await _userManager.FindByIdAsync(order.UserId);
                    if (user != null)
                    {
                        await _emailService.SendOrderConfirmationEmailAsync(
                            user.Email!, user.FullName, order.Id, order.TotalAmount);
                    }
                }
                catch { }

                TempData["SuccessMessage"] = "Payment successful! Your order is confirmed.";
                return RedirectToAction("OrderConfirmation", new { orderId });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                TempData["ErrorMessage"] = "Error processing payment confirmation.";
                return RedirectToAction("Index", "Home");
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
                    // تحديث حالة الأوردر
                    order.Status = OrderStatus.Cancelled;
                    _unitOfWork.Orders.Update(order);

                    // إرجاع المخزون
                    var orderItems = await _unitOfWork.OrderItems.GetAsync(oi => oi.OrderId == orderId);
                    foreach (var item in orderItems)
                    {
                        var product = await _unitOfWork.Products.GetByIdAsync(item.ProductId);
                        if (product != null)
                        {
                            product.Stock += item.Quantity;
                            _unitOfWork.Products.Update(product);
                        }
                    }

                    await _unitOfWork.SaveAsync();
                }

                TempData["ErrorMessage"] = "Payment cancelled. Your order has been cancelled and stock restored.";
                return RedirectToAction("Index", "Cart");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return RedirectToAction("Index", "Home");
            }
        }
    }
}