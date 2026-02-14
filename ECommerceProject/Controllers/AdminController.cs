using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ECommerceProject.Data.Interfaces;
using ECommerceProject.Models.Entities;
using ECommerceProject.Models.Enums;
using ECommerceProject.Models.ViewModels;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace ECommerceProject.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        // Dashboard
        public async Task<IActionResult> Dashboard()
        {
            ViewBag.TotalProducts = await _unitOfWork.Products.CountAsync();
            ViewBag.TotalCategories = await _unitOfWork.Categories.CountAsync();
            ViewBag.TotalOrders = await _unitOfWork.Orders.CountAsync();
            ViewBag.TotalUsers = await _unitOfWork.Users.CountAsync();

            // جلب آخر 5 طلبات
            var recentOrders = await _unitOfWork.Orders.GetAllAsync();
            ViewBag.RecentOrders = recentOrders.OrderByDescending(o => o.OrderDate).Take(5).ToList();

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
                Console.WriteLine($"Error: {ex.Message}");
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
                Console.WriteLine($"Error: {ex.Message}");
                TempData["ErrorMessage"] = "Error deleting user.";
            }

            return RedirectToAction(nameof(Users));
        }

        // ==================== Products Management ====================

        public async Task<IActionResult> Products()
        {
            var products = await _unitOfWork.Products.GetAllAsync();
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
        public async Task<IActionResult> CreateProduct(Product product)
        {
            // إزالة Navigation Properties من الـ Validation
            ModelState.Remove("Category");
            ModelState.Remove("OrderItems");
            ModelState.Remove("ShoppingCarts");
            ModelState.Remove("ProductVariants");

            if (!ModelState.IsValid)
            {
                Console.WriteLine("=== ModelState is INVALID ===");
                foreach (var key in ModelState.Keys)
                {
                    var errors = ModelState[key]?.Errors;
                    if (errors != null && errors.Count > 0)
                    {
                        foreach (var error in errors)
                        {
                            Console.WriteLine($"Key: {key}, Error: {error.ErrorMessage}");
                        }
                    }
                }

                var categories = await _unitOfWork.Categories.GetAsync(c => c.IsActive);
                ViewBag.Categories = categories.ToList();
                return View(product);
            }

            try
            {
                Console.WriteLine("=== Creating Product ===");
                Console.WriteLine($"Name: {product.Name}");
                Console.WriteLine($"Price: {product.Price}");
                Console.WriteLine($"Stock: {product.Stock}");
                Console.WriteLine($"CategoryId: {product.CategoryId}");

                product.CreatedDate = DateTime.Now;
                product.IsActive = true;

                await _unitOfWork.Products.AddAsync(product);
                var result = await _unitOfWork.SaveAsync();

                Console.WriteLine($"Save result: {result} rows affected");

                TempData["SuccessMessage"] = "Product created successfully!";
                return RedirectToAction(nameof(Products));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"=== EXCEPTION ===");
                Console.WriteLine($"Message: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }

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
        public async Task<IActionResult> EditProduct(Product product)
        {
            // إزالة Navigation Properties من الـ Validation
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

                // تحديث البيانات
                existingProduct.Name = product.Name;
                existingProduct.Description = product.Description;
                existingProduct.Price = product.Price;
                existingProduct.Stock = product.Stock;
                existingProduct.CategoryId = product.CategoryId;
                existingProduct.ImageUrl = product.ImageUrl;
                existingProduct.IsFeatured = product.IsFeatured;
                existingProduct.IsActive = product.IsActive;

                _unitOfWork.Products.Update(existingProduct);
                await _unitOfWork.SaveAsync();

                TempData["SuccessMessage"] = "Product updated successfully!";
                return RedirectToAction(nameof(Products));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                ModelState.AddModelError("", "Error updating product");

                var categories = await _unitOfWork.Categories.GetAsync(c => c.IsActive);
                ViewBag.Categories = categories.ToList();
                return View(product);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            try
            {
                var product = await _unitOfWork.Products.GetByIdAsync(id);
                if (product != null)
                {
                    _unitOfWork.Products.Delete(product);
                    await _unitOfWork.SaveAsync();
                    TempData["SuccessMessage"] = "Product deleted successfully!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Product not found.";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                TempData["ErrorMessage"] = "Error deleting product. Make sure there are no orders associated with it.";
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
        public async Task<IActionResult> CreateCategory(CategoryViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
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
                Console.WriteLine($"Error: {ex.Message}");
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
        public async Task<IActionResult> EditCategory(CategoryViewModel model)
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

                category.Name = model.Name;
                category.Description = model.Description;
                category.ImageUrl = model.ImageUrl;
                category.IsActive = model.IsActive;

                _unitOfWork.Categories.Update(category);
                await _unitOfWork.SaveAsync();

                TempData["SuccessMessage"] = "Category updated successfully!";
                return RedirectToAction(nameof(Categories));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                ModelState.AddModelError("", "Error updating category");
                return View(model);
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
                Console.WriteLine($"Error: {ex.Message}");
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
                Console.WriteLine($"Error: {ex.Message}");
                TempData["ErrorMessage"] = "Error updating category status.";
            }

            return RedirectToAction(nameof(Categories));
        }

        // ==================== Orders Management ====================

        public async Task<IActionResult> Orders()
        {
            var orders = await _unitOfWork.Orders.GetAllAsync();
            var ordersList = orders.OrderByDescending(o => o.OrderDate).ToList();
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
    }
}