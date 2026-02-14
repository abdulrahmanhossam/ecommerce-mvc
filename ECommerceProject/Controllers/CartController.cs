using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ECommerceProject.Data.Interfaces;
using ECommerceProject.Models.Entities;

namespace ECommerceProject.Controllers
{
    [Authorize]
    public class CartController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public CartController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // GET: Cart
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            // جلب العربة الخاصة بالمستخدم
            var cartItems = await _unitOfWork.ShoppingCarts.GetAsync(c => c.UserId == userId);

            // جلب تفاصيل المنتجات
            var cartWithProducts = new List<(ShoppingCart Cart, Product Product)>();

            foreach (var item in cartItems)
            {
                var product = await _unitOfWork.Products.GetByIdAsync(item.ProductId);
                if (product != null && product.IsActive)
                {
                    cartWithProducts.Add((item, product));
                }
            }

            ViewBag.CartItems = cartWithProducts;

            // حساب الإجمالي
            decimal total = cartWithProducts.Sum(x => x.Product.Price * x.Cart.Quantity);
            ViewBag.Total = total;

            return View();
        }

        // POST: Cart/AddToCart
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            // التحقق من المنتج
            var product = await _unitOfWork.Products.GetByIdAsync(productId);

            if (product == null || !product.IsActive)
            {
                TempData["ErrorMessage"] = "Product not found.";
                return RedirectToAction("Index", "Products");
            }

            // التحقق من الكمية المتاحة
            if (product.Stock < quantity)
            {
                TempData["ErrorMessage"] = $"Only {product.Stock} units available.";
                return RedirectToAction("Details", "Products", new { id = productId });
            }

            // البحث عن المنتج في العربة
            var existingCartItem = await _unitOfWork.ShoppingCarts.GetFirstOrDefaultAsync(
                c => c.UserId == userId && c.ProductId == productId);

            if (existingCartItem != null)
            {
                // تحديث الكمية
                existingCartItem.Quantity += quantity;

                // التأكد من عدم تجاوز المخزون
                if (existingCartItem.Quantity > product.Stock)
                {
                    TempData["ErrorMessage"] = $"Cannot add more than {product.Stock} units.";
                    return RedirectToAction("Details", "Products", new { id = productId });
                }

                _unitOfWork.ShoppingCarts.Update(existingCartItem);
            }
            else
            {
                // إضافة منتج جديد للعربة
                var cartItem = new ShoppingCart
                {
                    UserId = userId,
                    ProductId = productId,
                    Quantity = quantity,
                    AddedDate = DateTime.Now
                };

                await _unitOfWork.ShoppingCarts.AddAsync(cartItem);
            }

            await _unitOfWork.SaveAsync();

            TempData["SuccessMessage"] = $"{product.Name} added to cart successfully!";
            return RedirectToAction(nameof(Index));
        }

        // POST: Cart/UpdateQuantity
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateQuantity(int cartId, int quantity)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var cartItem = await _unitOfWork.ShoppingCarts.GetByIdAsync(cartId);

            if (cartItem == null || cartItem.UserId != userId)
            {
                TempData["ErrorMessage"] = "Cart item not found.";
                return RedirectToAction(nameof(Index));
            }

            // التحقق من المخزون
            var product = await _unitOfWork.Products.GetByIdAsync(cartItem.ProductId);

            if (product == null)
            {
                TempData["ErrorMessage"] = "Product not found.";
                return RedirectToAction(nameof(Index));
            }

            if (quantity > product.Stock)
            {
                TempData["ErrorMessage"] = $"Only {product.Stock} units available.";
                return RedirectToAction(nameof(Index));
            }

            if (quantity <= 0)
            {
                // حذف المنتج من العربة
                _unitOfWork.ShoppingCarts.Delete(cartItem);
            }
            else
            {
                cartItem.Quantity = quantity;
                _unitOfWork.ShoppingCarts.Update(cartItem);
            }

            await _unitOfWork.SaveAsync();

            TempData["SuccessMessage"] = "Cart updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        // POST: Cart/RemoveItem
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveItem(int cartId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var cartItem = await _unitOfWork.ShoppingCarts.GetByIdAsync(cartId);

            if (cartItem != null && cartItem.UserId == userId)
            {
                _unitOfWork.ShoppingCarts.Delete(cartItem);
                await _unitOfWork.SaveAsync();

                TempData["SuccessMessage"] = "Item removed from cart.";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Cart/ClearCart
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearCart()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var cartItems = await _unitOfWork.ShoppingCarts.GetAsync(c => c.UserId == userId);

            if (cartItems.Any())
            {
                _unitOfWork.ShoppingCarts.DeleteRange(cartItems);
                await _unitOfWork.SaveAsync();

                TempData["SuccessMessage"] = "Cart cleared successfully!";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Cart/GetCartCount (للـ Badge في Navbar)
        public async Task<IActionResult> GetCartCount()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Json(new { count = 0 });

            var count = await _unitOfWork.ShoppingCarts.CountAsync(c => c.UserId == userId);

            return Json(new { count });
        }
    }
}