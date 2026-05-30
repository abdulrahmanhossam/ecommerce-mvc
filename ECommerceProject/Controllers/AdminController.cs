using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ECommerceProject.Data.Interfaces;
using ECommerceProject.Models.Entities;
using ECommerceProject.Models.Enums;
using ECommerceProject.Models.ViewModels;
using System.Security.Claims;
using ECommerceProject.Services.Interfaces;

namespace ECommerceProject.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly IUnitOfWork _unitOfWork;

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IAnalyticsService _analyticsService;

    private readonly IImageService _imageService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        IUnitOfWork unitOfWork,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IAnalyticsService analyticsService,
        IImageService imageService,
        ILogger<AdminController> logger)
    {
        _unitOfWork = unitOfWork;
        _userManager = userManager;
        _roleManager = roleManager;
        _analyticsService = analyticsService;
        _imageService = imageService;
        _logger = logger;
    }

    // Dashboard
    public async Task<IActionResult> Dashboard()
    {
        var orderQuery = _unitOfWork.Orders.GetQueryable(asNoTracking: true);

        ViewBag.TotalProducts = await _unitOfWork.Products.CountAsync(p => p.IsActive);
        ViewBag.TotalCategories = await _unitOfWork.Categories.CountAsync(c => c.IsActive);
        ViewBag.TotalOrders = await orderQuery.CountAsync();
        ViewBag.TotalUsers = await _unitOfWork.Users.CountAsync();

        ViewBag.TotalRevenue = await orderQuery.SumAsync(o => o.TotalAmount);

        ViewBag.PendingOrders = await orderQuery.CountAsync(o =>
            o.Status == OrderStatus.Pending ||
            o.Status == OrderStatus.Paid ||
            o.Status == OrderStatus.Processing);

        ViewBag.CompletedOrders = await orderQuery.CountAsync(o => o.Status == OrderStatus.Delivered);

        ViewBag.LowStockProducts = await _unitOfWork.Products.CountAsync(p => p.Stock < 10 && p.IsActive);

        var salesByStatus = await orderQuery
            .GroupBy(o => o.Status)
            .Select(g => new { Status = g.Key.ToString(), Count = g.Count(), Total = g.Sum(o => o.TotalAmount) })
            .ToListAsync();
        ViewBag.SalesByStatus = salesByStatus;

        var top5ProductIds = await _unitOfWork.OrderItems.GetQueryable(asNoTracking: true)
            .GroupBy(oi => oi.ProductId)
            .Select(g => new { ProductId = g.Key, TotalQuantity = g.Sum(oi => oi.Quantity), TotalSales = g.Sum(oi => oi.TotalPrice) })
            .OrderByDescending(x => x.TotalQuantity)
            .Take(5)
            .ToListAsync();

        var prodIds = top5ProductIds.Select(x => x.ProductId).ToList();
        var productNames = await _unitOfWork.Products.GetQueryable(asNoTracking: true)
            .Where(p => prodIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Name })
            .ToDictionaryAsync(p => p.Id, p => p.Name);

        var topProductsList = top5ProductIds
            .Select(item => (
                item.ProductId,
                productNames.GetValueOrDefault(item.ProductId, "Unknown"),
                item.TotalQuantity,
                item.TotalSales))
            .ToList();
        ViewBag.TopProducts = topProductsList;

        var ordersByPayment = await orderQuery
            .GroupBy(o => o.PaymentMethod)
            .Select(g => new { Method = g.Key.ToString(), Count = g.Count(), Total = g.Sum(o => o.TotalAmount) })
            .ToListAsync();
        ViewBag.OrdersByPayment = ordersByPayment;

        var recentOrders = await orderQuery.OrderByDescending(o => o.OrderDate).Take(5).ToListAsync();
        var userIds = recentOrders.Select(o => o.UserId).Distinct().ToList();
        var userDict = await _unitOfWork.Users.GetQueryable(asNoTracking: true)
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new { u.Id, u.FullName })
            .ToDictionaryAsync(u => u.Id, u => u.FullName);

        foreach (var order in recentOrders)
        {
            order.User = new ApplicationUser { FullName = userDict.GetValueOrDefault(order.UserId, "Unknown") };
        }
        ViewBag.RecentOrders = recentOrders;

        return View();
    }

    // ==================== Users Management ====================

    public async Task<IActionResult> Users(int page = 1)
    {
        var query = _unitOfWork.Users.GetQueryable(asNoTracking: true)
            .OrderByDescending(u => u.CreatedDate);
        var users = await PaginatedList<ApplicationUser>.CreateAsync(query, page, 15);

        var userIds = users.Items.Select(u => u.Id).ToList();
        var orderCounts = await _unitOfWork.Orders.GetQueryable(asNoTracking: true)
            .Where(o => userIds.Contains(o.UserId))
            .GroupBy(o => o.UserId)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.UserId, x => x.Count);

        var allRoles = await _roleManager.Roles.Select(r => r.Name!).ToListAsync();
        var userRolesDict = new Dictionary<string, List<string>>();

        foreach (var role in allRoles)
        {
            var usersInRole = await _userManager.GetUsersInRoleAsync(role);
            foreach (var u in usersInRole)
            {
                if (!userRolesDict.ContainsKey(u.Id))
                    userRolesDict[u.Id] = [];
                userRolesDict[u.Id].Add(role);
            }
        }

        var usersWithRoles = new List<(ApplicationUser User, IList<string> Roles, int OrderCount)>();

        foreach (var user in users.Items)
        {
            var roles = userRolesDict.GetValueOrDefault(user.Id, []) as IList<string>;
            var orderCount = orderCounts.GetValueOrDefault(user.Id, 0);
            usersWithRoles.Add((user, roles, orderCount));
        }

        ViewBag.UsersWithRoles = usersWithRoles;

        return View(users);
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

    public async Task<IActionResult> Products(int page = 1)
    {
        var query = _unitOfWork.Products.GetQueryable(asNoTracking: true)
            .OrderByDescending(p => p.CreatedDate);
        var products = await PaginatedList<Product>.CreateAsync(query, page, 15);
        var categories = (await _unitOfWork.Categories.GetAllAsync()).ToDictionary(c => c.Id, c => c.Name);
        ViewBag.CategoryNames = categories;
        return View(products);
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
        var categoriesList = (await _unitOfWork.Categories.GetAllAsync())
            .OrderBy(c => c.Name).ToList();

        var catIds = categoriesList.Select(c => c.Id).ToList();
        var productCounts = await _unitOfWork.Products.GetQueryable(asNoTracking: true)
            .Where(p => catIds.Contains(p.CategoryId))
            .GroupBy(p => p.CategoryId)
            .Select(g => new { CategoryId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CategoryId, x => x.Count);

        var categoriesWithStats = categoriesList
            .Select(c => (c, productCounts.GetValueOrDefault(c.Id, 0)))
            .ToList();

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

    public async Task<IActionResult> Orders(int page = 1)
    {
        var query = _unitOfWork.Orders.GetQueryable(asNoTracking: true)
            .OrderByDescending(o => o.OrderDate);
        var orders = await PaginatedList<Order>.CreateAsync(query, page, 15);

        var userIds = orders.Items.Select(o => o.UserId).Distinct().ToList();
        var userNames = (await _unitOfWork.Users.GetAsync(u => userIds.Contains(u.Id), asNoTracking: true))
            .ToDictionary(u => u.Id, u => u.FullName);
        ViewBag.UserNames = userNames;

        return View(orders);
    }

    [HttpGet]
    public async Task<IActionResult> OrderDetails(int id)
    {
        var order = await _unitOfWork.Orders.GetByIdAsync(id, asNoTracking: true);

        if (order == null)
            return NotFound();

        // جلب Order Items مع المنتجات
        var orderItemsWithProducts = await _unitOfWork.OrderItems.GetQueryable(asNoTracking: true)
            .Where(oi => oi.OrderId == id)
            .Include(oi => oi.Product)
            .ToListAsync();

        var orderItemsWithProducts2 = orderItemsWithProducts
            .Select(oi => (oi, oi.Product))
            .ToList();

        ViewBag.OrderItems = orderItemsWithProducts2;

        // جلب User
        var orderUser = await _unitOfWork.Users.GetFirstOrDefaultAsync(u => u.Id == order.UserId, asNoTracking: true);
        ViewBag.User = orderUser;

        // جلب Payment
        var payment = await _unitOfWork.Payments.GetFirstOrDefaultAsync(p => p.OrderId == id, asNoTracking: true);
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

    public async Task<IActionResult> PromoCodes(int page = 1)
    {
        var query = _unitOfWork.PromoCodes.GetQueryable(asNoTracking: true)
            .OrderByDescending(p => p.CreatedDate);
        var promoCodes = await PaginatedList<PromoCode>.CreateAsync(query, page, 15);
        return View(promoCodes);
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