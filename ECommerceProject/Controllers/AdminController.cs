using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ECommerceProject.Data.Interfaces;
using ECommerceProject.Models.Entities;
using ECommerceProject.Models.Enums;
using ECommerceProject.Models.ViewModels;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using ECommerceProject.Services.Interfaces;

namespace ECommerceProject.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly IUnitOfWork _unitOfWork;

    private readonly UserManager<ApplicationUser> _userManager;

    private readonly IAnalyticsService _analyticsService;

    private readonly IImageService _imageService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        IUnitOfWork unitOfWork,
        UserManager<ApplicationUser> userManager,
        IAnalyticsService analyticsService,
        IImageService imageService,
        ILogger<AdminController> logger)
    {
        _unitOfWork = unitOfWork;
        _userManager = userManager;
        _analyticsService = analyticsService;
        _imageService = imageService;
        _logger = logger;
    }

    // Dashboard
    public async Task<IActionResult> Dashboard()
    {
        var allOrders = (await _unitOfWork.Orders.GetAllAsync()).ToList();
        
        ViewBag.TotalProducts = await _unitOfWork.Products.CountAsync();
        ViewBag.TotalCategories = await _unitOfWork.Categories.CountAsync();
        ViewBag.TotalOrders = allOrders.Count;
        ViewBag.TotalUsers = await _unitOfWork.Users.CountAsync();

        // Total Revenue
        ViewBag.TotalRevenue = allOrders.Sum(o => o.TotalAmount);

        // Pending Orders (Pending, Paid, Processing)
        ViewBag.PendingOrders = allOrders.Count(o => 
            o.Status == OrderStatus.Pending || 
            o.Status == OrderStatus.Paid || 
            o.Status == OrderStatus.Processing);

        // Completed Orders (Delivered)
        ViewBag.CompletedOrders = allOrders.Count(o => o.Status == OrderStatus.Delivered);

        // Low Stock Products
        ViewBag.LowStockProducts = await _unitOfWork.Products.CountAsync(p => p.Stock < 10 && p.IsActive);

        // Sales by status
        var salesByStatus = allOrders
            .GroupBy(o => o.Status)
            .Select(g => new { Status = g.Key.ToString(), Count = g.Count(), Total = g.Sum(o => o.TotalAmount) })
            .ToList();
        ViewBag.SalesByStatus = salesByStatus;

        // Top Products (by quantity sold)
        var allProducts = (await _unitOfWork.Products.GetAllAsync())
            .ToDictionary(p => p.Id, p => p.Name);

        var topProductGroups = (await _unitOfWork.OrderItems.GetAllAsync())
            .GroupBy(oi => oi.ProductId)
            .Select(g => new { ProductId = g.Key, TotalQuantity = g.Sum(oi => oi.Quantity), TotalSales = g.Sum(oi => oi.TotalPrice) })
            .OrderByDescending(x => x.TotalQuantity)
            .Take(5)
            .ToList();

        var topProductsList = new List<(int ProductId, string Name, int Quantity, decimal Sales)>();
        foreach (var item in topProductGroups)
        {
            topProductsList.Add((
                item.ProductId,
                allProducts.GetValueOrDefault(item.ProductId, "Unknown"),
                item.TotalQuantity,
                item.TotalSales
            ));
        }
        ViewBag.TopProducts = topProductsList;

        // Orders by Payment Method
        var ordersByPayment = allOrders
            .GroupBy(o => o.PaymentMethod)
            .Select(g => new { Method = g.Key.ToString(), Count = g.Count(), Total = g.Sum(o => o.TotalAmount) })
            .ToList();
        ViewBag.OrdersByPayment = ordersByPayment;

        // جلب آخر 5 طلبات مع User info
        var recentOrders = allOrders.OrderByDescending(o => o.OrderDate).Take(5).ToList();
        var userIds = recentOrders.Select(o => o.UserId).Distinct().ToList();
        var userDict = (await _unitOfWork.Users.GetAsync(u => userIds.Contains(u.Id)))
            .ToDictionary(u => u.Id, u => u.FullName);

        foreach (var order in recentOrders)
        {
            userDict.TryGetValue(order.UserId, out var name);
            order.User = new ApplicationUser { FullName = name ?? "Unknown" };
        }
        ViewBag.RecentOrders = recentOrders;

        return View();
    }

    // ==================== Users Management ====================

    public async Task<IActionResult> Users()
    {
        var users = await _unitOfWork.Users.GetAllAsync();
        var usersList = users.OrderByDescending(u => u.CreatedDate).ToList();

        // جلب الـ Roles لكل مستخدم
        var usersWithRoles = new List<(ApplicationUser User, IList<string> Roles, int OrderCount)>();

        foreach (var user in usersList)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var orderCount = await _unitOfWork.Orders.CountAsync(o => o.UserId == user.Id);
            usersWithRoles.Add((user, roles, orderCount));
        }

        ViewBag.UsersWithRoles = usersWithRoles;

        return View(usersList);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleUserStatus(string userId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction(nameof(Users));
            }

            // منع Admin من تعطيل نفسه
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (user.Id == currentUserId)
            {
                TempData["ErrorMessage"] = "You cannot deactivate your own account!";
                return RedirectToAction(nameof(Users));
            }

            user.LockoutEnd = user.LockoutEnd.HasValue ? null : DateTimeOffset.MaxValue;
            await _userManager.UpdateAsync(user);

            TempData["SuccessMessage"] = $"User {(user.LockoutEnd.HasValue ? "deactivated" : "activated")} successfully!";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AdminController error");
            TempData["ErrorMessage"] = "Error updating user status.";
        }

        return RedirectToAction(nameof(Users));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteUser(string userId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction(nameof(Users));
            }

            // منع Admin من حذف نفسه
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (user.Id == currentUserId)
            {
                TempData["ErrorMessage"] = "You cannot delete your own account!";
                return RedirectToAction(nameof(Users));
            }

            // حذف العربة
            var cartItems = await _unitOfWork.ShoppingCarts.GetAsync(c => c.UserId == userId);
            if (cartItems.Any())
            {
                _unitOfWork.ShoppingCarts.DeleteRange(cartItems);
                await _unitOfWork.SaveAsync();
            }

            await _userManager.DeleteAsync(user);

            TempData["SuccessMessage"] = "User deleted successfully!";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AdminController error");
            TempData["ErrorMessage"] = "Error deleting user.";
        }

        return RedirectToAction(nameof(Users));
    }

    // ==================== Products Management ====================

    public async Task<IActionResult> Products()
    {
        var products = await _unitOfWork.Products.GetAllAsync();
        var categories = (await _unitOfWork.Categories.GetAllAsync()).ToDictionary(c => c.Id, c => c.Name);
        ViewBag.CategoryNames = categories;
        return View(products.ToList());
    }

    [HttpGet]
    public async Task<IActionResult> CreateProduct()
    {
        var categories = await _unitOfWork.Categories.GetAsync(c => c.IsActive);
        ViewBag.Categories = categories.ToList();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateProduct(Product product, IFormFile? ImageFile)
    {
        ModelState.Remove("Category");
        ModelState.Remove("OrderItems");
        ModelState.Remove("ShoppingCarts");
        ModelState.Remove("ProductVariants");

        if (!ModelState.IsValid)
        {
            var categories = await _unitOfWork.Categories.GetAsync(c => c.IsActive);
            ViewBag.Categories = categories.ToList();
            return View(product);
        }

        try
        {
            if (ImageFile != null && ImageFile.Length > 0)
            {
                product.ImageUrl = await _imageService.UploadImageAsync(ImageFile, "products");
            }

            product.CreatedDate = DateTime.Now;
            product.IsActive = true;

            await _unitOfWork.Products.AddAsync(product);
            await _unitOfWork.SaveAsync();

            TempData["SuccessMessage"] = "Product created successfully!";
            return RedirectToAction(nameof(Products));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AdminController error");
            ModelState.AddModelError("", $"Error: {ex.Message}");

            var categories = await _unitOfWork.Categories.GetAsync(c => c.IsActive);
            ViewBag.Categories = categories.ToList();
            return View(product);
        }
    }

    [HttpGet]
    public async Task<IActionResult> EditProduct(int id)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(id);
        if (product == null)
            return NotFound();

        var categories = await _unitOfWork.Categories.GetAsync(c => c.IsActive);
        ViewBag.Categories = categories.ToList();
        return View(product);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditProduct(Product product, IFormFile? ImageFile, bool? removeImage)
    {
        ModelState.Remove("Category");
        ModelState.Remove("OrderItems");
        ModelState.Remove("ShoppingCarts");
        ModelState.Remove("ProductVariants");

        if (!ModelState.IsValid)
        {
            var categories = await _unitOfWork.Categories.GetAsync(c => c.IsActive);
            ViewBag.Categories = categories.ToList();
            return View(product);
        }

        try
        {
            var existingProduct = await _unitOfWork.Products.GetByIdAsync(product.Id);

            if (existingProduct == null)
                return NotFound();

            if (removeImage == true && !string.IsNullOrEmpty(existingProduct.ImageUrl))
            {
                await _imageService.DeleteImageAsync(existingProduct.ImageUrl);
                existingProduct.ImageUrl = null;
            }
            else if (ImageFile != null && ImageFile.Length > 0)
            {
                if (!string.IsNullOrEmpty(existingProduct.ImageUrl))
                {
                    await _imageService.DeleteImageAsync(existingProduct.ImageUrl);
                }
                existingProduct.ImageUrl = await _imageService.UploadImageAsync(ImageFile, "products");
            }

            existingProduct.Name = product.Name;
            existingProduct.Description = product.Description;
            existingProduct.Price = product.Price;
            existingProduct.Stock = product.Stock;
            existingProduct.CategoryId = product.CategoryId;
            existingProduct.IsFeatured = product.IsFeatured;
            existingProduct.IsActive = product.IsActive;

            _unitOfWork.Products.Update(existingProduct);
            await _unitOfWork.SaveAsync();

            TempData["SuccessMessage"] = "Product updated successfully!";
            return RedirectToAction(nameof(Products));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AdminController error");
            ModelState.AddModelError("", "Error updating product");

            var categories = await _unitOfWork.Categories.GetAsync(c => c.IsActive);
            ViewBag.Categories = categories.ToList();
            return View(product);
        }
    }

    [HttpPost]
    public async Task<IActionResult> RemoveProductImage(int id)
    {
        try
        {
            var product = await _unitOfWork.Products.GetByIdAsync(id);
            if (product != null && !string.IsNullOrEmpty(product.ImageUrl))
            {
                await _imageService.DeleteImageAsync(product.ImageUrl);
                product.ImageUrl = null;
                _unitOfWork.Products.Update(product);
                await _unitOfWork.SaveAsync();
                return Json(new { success = true });
            }
            return Json(new { success = false, message = "Image not found" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        try
        {
            var product = await _unitOfWork.Products.GetByIdAsync(id);

            if (product == null)
            {
                TempData["ErrorMessage"] = "Product not found.";
                return RedirectToAction(nameof(Products));
            }

            // التحقق من وجود طلبات
            var hasOrders = await _unitOfWork.OrderItems.AnyAsync(oi => oi.ProductId == id);

            if (hasOrders)
            {
                // Soft Delete - نخلي المنتج Inactive بس
                product.IsActive = false;
                _unitOfWork.Products.Update(product);
                await _unitOfWork.SaveAsync();

                TempData["SuccessMessage"] = "Product deactivated successfully! (Cannot delete - has order history)";
            }
            else
            {
                // Hard Delete - حذف فعلي
                _unitOfWork.Products.Delete(product);
                await _unitOfWork.SaveAsync();

                TempData["SuccessMessage"] = "Product deleted successfully!";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AdminController error");
            TempData["ErrorMessage"] = "Error processing request.";
        }

        return RedirectToAction(nameof(Products));
    }

    // ==================== Categories Management ====================

    public async Task<IActionResult> Categories()
    {
        var categories = await _unitOfWork.Categories.GetAllAsync();
        var categoriesList = categories.OrderBy(c => c.Name).ToList();

        // إحصائيات لكل Category
        var categoriesWithStats = new List<(Category Category, int ProductCount)>();

        foreach (var category in categoriesList)
        {
            var productCount = await _unitOfWork.Products.CountAsync(p => p.CategoryId == category.Id);
            categoriesWithStats.Add((category, productCount));
        }

        ViewBag.CategoriesWithStats = categoriesWithStats;

        return View(categoriesList);
    }

    [HttpGet]
    public IActionResult CreateCategory()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCategory(CategoryViewModel model, IFormFile? ImageFile)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            if (ImageFile != null && ImageFile.Length > 0)
            {
                model.ImageUrl = await _imageService.UploadImageAsync(ImageFile, "categories");
            }

            var category = new Category
            {
                Name = model.Name,
                Description = model.Description,
                ImageUrl = model.ImageUrl,
                IsActive = model.IsActive,
                CreatedDate = DateTime.Now
            };

            await _unitOfWork.Categories.AddAsync(category);
            await _unitOfWork.SaveAsync();

            TempData["SuccessMessage"] = "Category created successfully!";
            return RedirectToAction(nameof(Categories));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AdminController error");
            ModelState.AddModelError("", "Error creating category");
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> EditCategory(int id)
    {
        var category = await _unitOfWork.Categories.GetByIdAsync(id);

        if (category == null)
            return NotFound();

        var model = new CategoryViewModel
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            ImageUrl = category.ImageUrl,
            IsActive = category.IsActive
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditCategory(CategoryViewModel model, IFormFile? ImageFile, bool? removeImage)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var category = await _unitOfWork.Categories.GetByIdAsync(model.Id);

            if (category == null)
                return NotFound();

            if (removeImage == true && !string.IsNullOrEmpty(category.ImageUrl))
            {
                await _imageService.DeleteImageAsync(category.ImageUrl);
                category.ImageUrl = null;
            }
            else if (ImageFile != null && ImageFile.Length > 0)
            {
                if (!string.IsNullOrEmpty(category.ImageUrl))
                {
                    await _imageService.DeleteImageAsync(category.ImageUrl);
                }
                category.ImageUrl = await _imageService.UploadImageAsync(ImageFile, "categories");
            }

            category.Name = model.Name;
            category.Description = model.Description;
            category.IsActive = model.IsActive;

            _unitOfWork.Categories.Update(category);
            await _unitOfWork.SaveAsync();

            TempData["SuccessMessage"] = "Category updated successfully!";
            return RedirectToAction(nameof(Categories));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AdminController error");
            ModelState.AddModelError("", "Error updating category");
            return View(model);
        }
    }

    [HttpPost]
    public async Task<IActionResult> RemoveCategoryImage(int id)
    {
        try
        {
            var category = await _unitOfWork.Categories.GetByIdAsync(id);
            if (category != null && !string.IsNullOrEmpty(category.ImageUrl))
            {
                await _imageService.DeleteImageAsync(category.ImageUrl);
                category.ImageUrl = null;
                _unitOfWork.Categories.Update(category);
                await _unitOfWork.SaveAsync();
                return Json(new { success = true });
            }
            return Json(new { success = false, message = "Image not found" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        try
        {
            var category = await _unitOfWork.Categories.GetByIdAsync(id);

            if (category == null)
            {
                TempData["ErrorMessage"] = "Category not found.";
                return RedirectToAction(nameof(Categories));
            }

            // التحقق من وجود منتجات في الفئة
            var hasProducts = await _unitOfWork.Products.AnyAsync(p => p.CategoryId == id);

            if (hasProducts)
            {
                TempData["ErrorMessage"] = "Cannot delete category with existing products. Please remove or reassign products first.";
                return RedirectToAction(nameof(Categories));
            }

            _unitOfWork.Categories.Delete(category);
            await _unitOfWork.SaveAsync();

            TempData["SuccessMessage"] = "Category deleted successfully!";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AdminController error");
            TempData["ErrorMessage"] = "Error deleting category.";
        }

        return RedirectToAction(nameof(Categories));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleCategoryStatus(int id)
    {
        try
        {
            var category = await _unitOfWork.Categories.GetByIdAsync(id);

            if (category == null)
            {
                TempData["ErrorMessage"] = "Category not found.";
                return RedirectToAction(nameof(Categories));
            }

            category.IsActive = !category.IsActive;
            _unitOfWork.Categories.Update(category);
            await _unitOfWork.SaveAsync();

            TempData["SuccessMessage"] = $"Category {(category.IsActive ? "activated" : "deactivated")} successfully!";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AdminController error");
            TempData["ErrorMessage"] = "Error updating category status.";
        }

        return RedirectToAction(nameof(Categories));
    }

    // ==================== Orders Management ====================

    public async Task<IActionResult> Orders()
    {
        var orders = await _unitOfWork.Orders.GetAllAsync();
        var ordersList = orders.OrderByDescending(o => o.OrderDate).ToList();

        var userIds = ordersList.Select(o => o.UserId).Distinct().ToList();
        var userNames = (await _unitOfWork.Users.GetAsync(u => userIds.Contains(u.Id)))
            .ToDictionary(u => u.Id, u => u.FullName);
        ViewBag.UserNames = userNames;

        return View(ordersList);
    }

    [HttpGet]
    public async Task<IActionResult> OrderDetails(int id)
    {
        var order = await _unitOfWork.Orders.GetByIdAsync(id);

        if (order == null)
            return NotFound();

        // جلب Order Items مع المنتجات
        var orderItems = await _unitOfWork.OrderItems.GetAsync(oi => oi.OrderId == id);
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

        // جلب User
        var user = await _unitOfWork.Users.GetAsync(u => u.Id == order.UserId);
        ViewBag.User = user.FirstOrDefault();

        // جلب Payment
        var payment = await _unitOfWork.Payments.GetFirstOrDefaultAsync(p => p.OrderId == id);
        ViewBag.Payment = payment;

        return View(order);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateOrderStatus(int orderId, OrderStatus status)
    {
        var order = await _unitOfWork.Orders.GetByIdAsync(orderId);

        if (order == null)
        {
            TempData["ErrorMessage"] = "Order not found.";
            return RedirectToAction(nameof(Orders));
        }

        order.Status = status;

        if (status == OrderStatus.Delivered)
        {
            order.DeliveredDate = DateTime.Now;
        }

        _unitOfWork.Orders.Update(order);
        await _unitOfWork.SaveAsync();

        TempData["SuccessMessage"] = $"Order status updated to {status}";
        return RedirectToAction(nameof(OrderDetails), new { id = orderId });
    }

    // ==================== Analytics ====================

    public async Task<IActionResult> Analytics()
    {
        var model = await _analyticsService.GetSalesAnalyticsAsync();
        return View(model);
    }

    // ==================== Promo Codes Management ====================

    public async Task<IActionResult> PromoCodes()
    {
        var promoCodes = await _unitOfWork.PromoCodes.GetAllAsync();
        var promoCodesList = promoCodes.OrderByDescending(p => p.CreatedDate).ToList();
        return View(promoCodesList);
    }

    [HttpGet]
    public IActionResult CreatePromoCode()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreatePromoCode(PromoCode promoCode)
    {
        ModelState.Remove("Orders");

        if (!ModelState.IsValid)
        {
            return View(promoCode);
        }

        try
        {
            // التأكد من عدم تكرار الكود
            var existing = await _unitOfWork.PromoCodes.GetFirstOrDefaultAsync(
                p => p.Code.ToUpper() == promoCode.Code.ToUpper());

            if (existing != null)
            {
                ModelState.AddModelError("Code", "This promo code already exists.");
                return View(promoCode);
            }

            promoCode.Code = promoCode.Code.ToUpper();
            promoCode.CreatedDate = DateTime.Now;
            promoCode.UsageCount = 0;

            await _unitOfWork.PromoCodes.AddAsync(promoCode);
            await _unitOfWork.SaveAsync();

            TempData["SuccessMessage"] = "Promo code created successfully!";
            return RedirectToAction(nameof(PromoCodes));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AdminController error");
            ModelState.AddModelError("", "Error creating promo code");
            return View(promoCode);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TogglePromoCodeStatus(int id)
    {
        try
        {
            var promoCode = await _unitOfWork.PromoCodes.GetByIdAsync(id);

            if (promoCode != null)
            {
                promoCode.IsActive = !promoCode.IsActive;
                _unitOfWork.PromoCodes.Update(promoCode);
                await _unitOfWork.SaveAsync();

                TempData["SuccessMessage"] = $"Promo code {(promoCode.IsActive ? "activated" : "deactivated")} successfully!";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AdminController error");
            TempData["ErrorMessage"] = "Error updating promo code status.";
        }

        return RedirectToAction(nameof(PromoCodes));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePromoCode(int id)
    {
        try
        {
            var promoCode = await _unitOfWork.PromoCodes.GetByIdAsync(id);

            if (promoCode != null)
            {
                _unitOfWork.PromoCodes.Delete(promoCode);
                await _unitOfWork.SaveAsync();
                TempData["SuccessMessage"] = "Promo code deleted successfully!";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AdminController error");
            TempData["ErrorMessage"] = "Error deleting promo code.";
        }

        return RedirectToAction(nameof(PromoCodes));
    }
}