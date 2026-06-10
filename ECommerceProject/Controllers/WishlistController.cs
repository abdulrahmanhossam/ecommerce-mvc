using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using ECommerceProject.Data.Interfaces;
using ECommerceProject.Models.Entities;

namespace ECommerceProject.Controllers
{
    [Authorize]
    public class WishlistController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public WishlistController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var wishlistItems = await _unitOfWork.Wishlists.GetQueryable(asNoTracking: true)
                .Where(w => w.UserId == userId)
                .Include(w => w.Product)
                .ToListAsync();

            var wishlistWithProducts = wishlistItems
                .Where(w => w.Product is { IsActive: true })
                .Select(w => (w, w.Product))
                .ToList();

            ViewBag.WishlistCount = wishlistWithProducts.Count;
            return View(wishlistWithProducts);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add([FromBody] WishlistRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Please login first" });
            }

            var product = await _unitOfWork.Products.GetByIdAsync(request.ProductId);
            if (product == null || !product.IsActive)
            {
                return Json(new { success = false, message = "Product not found" });
            }

            var existingItem = await _unitOfWork.Wishlists.GetFirstOrDefaultAsync(
                w => w.UserId == userId && w.ProductId == request.ProductId);

            if (existingItem != null)
            {
                return Json(new { success = false, message = "Product already in wishlist" });
            }

            var wishlistItem = new Wishlist
            {
                UserId = userId,
                ProductId = request.ProductId,
                AddedDate = DateTime.Now
            };

            await _unitOfWork.Wishlists.AddAsync(wishlistItem);
            await _unitOfWork.SaveAsync();

            var wishlistCount = await _unitOfWork.Wishlists.CountAsync(w => w.UserId == userId);

            return Json(new { success = true, message = "Added to wishlist", count = wishlistCount });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove([FromBody] WishlistRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var item = await _unitOfWork.Wishlists.GetFirstOrDefaultAsync(
                w => w.UserId == userId && w.ProductId == request.ProductId);

            if (item != null)
            {
                _unitOfWork.Wishlists.Delete(item);
                await _unitOfWork.SaveAsync();
            }

            var wishlistCount = await _unitOfWork.Wishlists.CountAsync(w => w.UserId == userId);

            return Json(new { success = true, message = "Removed from wishlist", count = wishlistCount });
        }

        [HttpGet]
        public async Task<IActionResult> GetCount()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { count = 0 });
            }

            var count = await _unitOfWork.Wishlists.CountAsync(w => w.UserId == userId);
            return Json(new { count });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckIsInWishlist([FromBody] WishlistRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { isInWishlist = false });
            }

            var item = await _unitOfWork.Wishlists.GetFirstOrDefaultAsync(
                w => w.UserId == userId && w.ProductId == request.ProductId);

            return Json(new { isInWishlist = item != null });
        }
    }

    public class WishlistRequest
    {
        public int ProductId { get; set; }
    }
}