using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ECommerceProject.Models.Entities;
using ECommerceProject.Models.ViewModels;
using ECommerceProject.Services.Interfaces;
using ECommerceProject.Data.Interfaces;
using System.Security.Claims;

namespace ECommerceProject.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<AccountController> _logger;
        private readonly IEmailService _emailService;
        private readonly IUnitOfWork _unitOfWork;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<AccountController> logger,
            IEmailService emailService,
            IUnitOfWork unitOfWork)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _emailService = emailService;
            _unitOfWork = unitOfWork;
        }

        // ==================== REGISTER ====================

        [HttpGet]
        public IActionResult Register()
        {
            // لو المستخدم مسجل دخول، نرجعه للصفحة الرئيسية
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // إنشاء مستخدم جديد
            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                PhoneNumber = model.PhoneNumber,
                Address = model.Address,
                City = model.City,
                Country = model.Country,
                CreatedDate = DateTime.Now
            };

            // محاولة إنشاء المستخدم
            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "Customer");

                _logger.LogInformation($"User {user.Email} registered successfully");

                // إرسال Email ترحيبي
                try
                {
                    await _emailService.SendRegistrationConfirmationEmailAsync(user.Email, user.FullName);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to send registration email: {ex.Message}");
                }

                await _signInManager.SignInAsync(user, isPersistent: false);

                TempData["SuccessMessage"] = "Registration successful! Welcome to our store.";
                return RedirectToAction("Index", "Home");
            }

            // لو فيه أخطاء، نعرضها
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        // ==================== LOGIN ====================

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
                return View(model);

            // محاولة تسجيل الدخول
            var result = await _signInManager.PasswordSignInAsync(
                model.Email,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: false);

            if (result.Succeeded)
            {
                _logger.LogInformation($"User {model.Email} logged in");

                // لو فيه returnUrl، نرجعه ليها
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);

                return RedirectToAction("Index", "Home");
            }

            if (result.IsLockedOut)
            {
                _logger.LogWarning($"User {model.Email} account locked out");
                ModelState.AddModelError(string.Empty, "Account locked. Please try again later.");
                return View(model);
            }

            ModelState.AddModelError(string.Empty, "Invalid login attempt. Check your email and password.");
            return View(model);
        }

        // ==================== LOGOUT ====================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out");
            TempData["SuccessMessage"] = "You have been logged out successfully.";
            return RedirectToAction("Index", "Home");
        }

        // ==================== FORGOT PASSWORD ====================

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);

            // حتى لو المستخدم مش موجود، نعرض رسالة نجاح (أمان)
            if (user == null)
            {
                return RedirectToAction(nameof(ForgotPasswordConfirmation));
            }

            // إنشاء Token لإعادة تعيين كلمة المرور
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            var callbackUrl = Url.Action(
                "ResetPassword",
                "Account",
                new { token, email = user.Email },
                protocol: Request.Scheme);

            if (string.IsNullOrEmpty(callbackUrl) || string.IsNullOrEmpty(user.Email))
            {
                _logger.LogError("Failed to generate password reset callback URL for user {Email}", user.Email);
                return RedirectToAction(nameof(ForgotPasswordConfirmation));
            }

            // إرسال Email
            try
            {
                await _emailService.SendPasswordResetEmailAsync(user.Email, callbackUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to send password reset email: {ex.Message}");
            }

            return RedirectToAction(nameof(ForgotPasswordConfirmation));
        }

        [HttpGet]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        // ==================== RESET PASSWORD ====================

        [HttpGet]
        public IActionResult ResetPassword(string? token = null, string? email = null)
        {
            if (token == null || email == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid password reset token.");
            }

            var model = new ResetPasswordViewModel
            {
                Token = token ?? string.Empty,
                Email = email ?? string.Empty
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                return RedirectToAction(nameof(ResetPasswordConfirmation));
            }

            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);

            if (result.Succeeded)
            {
                _logger.LogInformation($"Password reset successful for {user.Email}");
                return RedirectToAction(nameof(ResetPasswordConfirmation));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }

        // ==================== PROFILE MANAGEMENT ====================

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return RedirectToAction(nameof(Login));

            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
                return NotFound();

            var model = new ProfileViewModel
            {
                FullName = user.FullName,
                Email = user.Email!,
                PhoneNumber = user.PhoneNumber,
                Address = user.Address,
                City = user.City,
                Country = user.Country,
                MemberSince = user.CreatedDate
            };

            await PopulateProfileViewBagAsync(userId);
            ViewBag.MemberSince = user.CreatedDate;

            return View(model);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return RedirectToAction(nameof(Login));

                await PopulateProfileViewBagAsync(userId);
                ViewBag.MemberSince = (await _userManager.FindByIdAsync(userId))?.CreatedDate;

                return View(model);
            }

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId))
                return RedirectToAction(nameof(Login));

            var user = await _userManager.FindByIdAsync(currentUserId);

            if (user == null)
                return NotFound();

            user.FullName = model.FullName;
            user.PhoneNumber = model.PhoneNumber;
            user.Address = model.Address;
            user.City = model.City;
            user.Country = model.Country;

            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Profile updated successfully!";
                return RedirectToAction(nameof(Profile));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            await PopulateProfileViewBagAsync(currentUserId);
            ViewBag.MemberSince = user.CreatedDate;

            return View(model);
        }

        private async Task PopulateProfileViewBagAsync(string userId)
        {
            var orders = (await _unitOfWork.Orders.GetAsync(o => o.UserId == userId, asNoTracking: true)).ToList();
            ViewBag.TotalOrders = orders.Count;
            ViewBag.TotalSpent = orders.Sum(o => o.TotalAmount);
            ViewBag.PendingOrders = orders.Count(o =>
                o.Status == ECommerceProject.Models.Enums.OrderStatus.Pending ||
                o.Status == ECommerceProject.Models.Enums.OrderStatus.Paid ||
                o.Status == ECommerceProject.Models.Enums.OrderStatus.Processing);
            ViewBag.CompletedOrders = orders.Count(o => o.Status == ECommerceProject.Models.Enums.OrderStatus.Delivered);

            ViewBag.WishlistCount = await _unitOfWork.Wishlists.CountAsync(w => w.UserId == userId);
            ViewBag.CartCount = await _unitOfWork.ShoppingCarts.CountAsync(c => c.UserId == userId);
            ViewBag.RecentOrders = orders.OrderByDescending(o => o.OrderDate).Take(3).ToList();
        }

        // ==================== CHANGE PASSWORD ====================

        [Authorize]
        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction(nameof(Login));

            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
                return NotFound();

            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);

            if (result.Succeeded)
            {
                await _signInManager.RefreshSignInAsync(user);
                _logger.LogInformation($"User {user.Email} changed password successfully");

                TempData["SuccessMessage"] = "Password changed successfully!";
                return RedirectToAction(nameof(Profile));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        // ==================== DELETE ACCOUNT ====================

        [Authorize]
        [HttpGet]
        public IActionResult DeleteAccount()
        {
            return View();
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAccountConfirmed()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction(nameof(Login));

            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
                return NotFound();

            await using var transaction = await _unitOfWork.BeginTransactionAsync();

            try
            {
                // حذف البيانات المرتبطة قبل حذف المستخدم (FK constraints)
                var cartItems = await _unitOfWork.ShoppingCarts.GetAsync(c => c.UserId == userId);
                if (cartItems.Any())
                    _unitOfWork.ShoppingCarts.DeleteRange(cartItems);

                var wishlistItems = await _unitOfWork.Wishlists.GetAsync(w => w.UserId == userId);
                if (wishlistItems.Any())
                    _unitOfWork.Wishlists.DeleteRange(wishlistItems);

                var reviews = await _unitOfWork.ProductReviews.GetAsync(r => r.UserId == userId);
                if (reviews.Any())
                    _unitOfWork.ProductReviews.DeleteRange(reviews);

                // جلب الـ Payments المرتبطة بالأوردرات وحذفها
                var orders = await _unitOfWork.Orders.GetAsync(o => o.UserId == userId);
                if (orders.Any())
                {
                    var orderIds = orders.Select(o => o.Id).ToList();
                    var payments = await _unitOfWork.Payments.GetAsync(p => orderIds.Contains(p.OrderId));
                    if (payments.Any())
                        _unitOfWork.Payments.DeleteRange(payments);

                    _unitOfWork.Orders.DeleteRange(orders);
                }

                await _unitOfWork.SaveAsync();

                // حذف المستخدم
                var result = await _userManager.DeleteAsync(user);

                if (result.Succeeded)
                {
                    await transaction.CommitAsync();
                    await _signInManager.SignOutAsync();
                    _logger.LogInformation("User {Email} deleted their account", user.Email);

                    TempData["SuccessMessage"] = "Your account has been deleted.";
                    return RedirectToAction("Index", "Home");
                }

                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = "Failed to delete account.";
                return RedirectToAction(nameof(Profile));
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // ==================== ACCESS DENIED ====================

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}