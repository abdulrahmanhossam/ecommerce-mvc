using Microsoft.AspNetCore.Mvc;
using ECommerceProject.Data.Interfaces;
using Microsoft.AspNetCore.Authorization;
using ECommerceProject.Models.Entities;
using System.Security.Claims;
using ECommerceProject.Models.ViewModels;

namespace ECommerceProject.Controllers
{
    public class ProductsController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public ProductsController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // GET: Products
        public async Task<IActionResult> Index(int? categoryId, string? searchTerm, decimal? minPrice, decimal? maxPrice)
        {
            var products = await _unitOfWork.Products.GetAllAsync();

            if (categoryId.HasValue)
            {
                products = products.Where(p => p.CategoryId == categoryId.Value);
            }

            if (!string.IsNullOrEmpty(searchTerm))
            {
                products = products.Where(p =>
                    p.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    (p.Description != null && p.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                );
            }

            if (minPrice.HasValue)
            {
                products = products.Where(p => p.Price >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                products = products.Where(p => p.Price <= maxPrice.Value);
            }

            products = products.Where(p => p.IsActive);

            var categories = await _unitOfWork.Categories.GetAllAsync();
            ViewBag.Categories = categories.Where(c => c.IsActive).ToList();
            ViewBag.SelectedCategoryId = categoryId;
            ViewBag.SearchTerm = searchTerm;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;

            return View(products.ToList());
        }

        // GET: Products/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(id);

            if (product == null || !product.IsActive)
            {
                return NotFound();
            }

            var category = await _unitOfWork.Categories.GetByIdAsync(product.CategoryId);
            ViewBag.Category = category;

            var variants = await _unitOfWork.ProductVariants.GetAsync(v => v.ProductId == id);
            ViewBag.Variants = variants.ToList();

            var relatedProducts = await _unitOfWork.Products.GetAsync(
                p => p.CategoryId == product.CategoryId && p.Id != id && p.IsActive);
            ViewBag.RelatedProducts = relatedProducts.Take(4).ToList();

            var reviews = await _unitOfWork.ProductReviews.GetAsync(
                r => r.ProductId == id && r.IsApproved);
            var reviewsList = reviews.OrderByDescending(r => r.CreatedDate).ToList();
            ViewBag.Reviews = reviewsList;

            if (reviewsList.Any())
            {
                ViewBag.AverageRating = reviewsList.Average(r => r.Rating);
                ViewBag.TotalReviews = reviewsList.Count;
                ViewBag.FiveStars = reviewsList.Count(r => r.Rating == 5);
                ViewBag.FourStars = reviewsList.Count(r => r.Rating == 4);
                ViewBag.ThreeStars = reviewsList.Count(r => r.Rating == 3);
                ViewBag.TwoStars = reviewsList.Count(r => r.Rating == 2);
                ViewBag.OneStar = reviewsList.Count(r => r.Rating == 1);
            }
            else
            {
                ViewBag.AverageRating = 0.0;
                ViewBag.TotalReviews = 0;
                ViewBag.FiveStars = 0;
                ViewBag.FourStars = 0;
                ViewBag.ThreeStars = 0;
                ViewBag.TwoStars = 0;
                ViewBag.OneStar = 0;
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId))
            {
                var hasPurchased = await HasPurchasedProduct(userId, id);
                ViewBag.HasPurchased = hasPurchased;

                var hasReviewed = await _unitOfWork.ProductReviews.AnyAsync(
                    r => r.ProductId == id && r.UserId == userId);
                ViewBag.HasReviewed = hasReviewed;
            }
            else
            {
                ViewBag.HasPurchased = false;
                ViewBag.HasReviewed = false;
            }

            return View(product);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddReview(AddReviewViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please provide a valid review.";
                return RedirectToAction(nameof(Details), new { id = model.ProductId });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var existingReview = await _unitOfWork.ProductReviews.GetFirstOrDefaultAsync(
                    r => r.ProductId == model.ProductId && r.UserId == userId);

                if (existingReview != null)
                {
                    TempData["ErrorMessage"] = "You have already reviewed this product.";
                    return RedirectToAction(nameof(Details), new { id = model.ProductId });
                }

                var hasPurchased = await HasPurchasedProduct(userId, model.ProductId);

                var review = new ProductReview
                {
                    ProductId = model.ProductId,
                    UserId = userId,
                    Rating = model.Rating,
                    Title = model.Title,
                    Comment = model.Comment,
                    CreatedDate = DateTime.Now,
                    IsVerifiedPurchase = hasPurchased,
                    IsApproved = true
                };

                await _unitOfWork.ProductReviews.AddAsync(review);
                await _unitOfWork.SaveAsync();

                TempData["SuccessMessage"] = "Thank you for your review!";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                TempData["ErrorMessage"] = "Failed to submit review. Please try again.";
            }

            return RedirectToAction(nameof(Details), new { id = model.ProductId });
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> MarkHelpful(int reviewId, bool helpful)
        {
            try
            {
                var review = await _unitOfWork.ProductReviews.GetByIdAsync(reviewId);

                if (review != null)
                {
                    if (helpful)
                    {
                        review.HelpfulCount++;
                    }
                    else
                    {
                        review.NotHelpfulCount++;
                    }

                    _unitOfWork.ProductReviews.Update(review);
                    await _unitOfWork.SaveAsync();

                    return Json(new { success = true, helpfulCount = review.HelpfulCount, notHelpfulCount = review.NotHelpfulCount });
                }

                return Json(new { success = false });
            }
            catch
            {
                return Json(new { success = false });
            }
        }

        // ✅ Helper Method (الطريقة الصحيحة)
        private async Task<bool> HasPurchasedProduct(string userId, int productId)
        {
            // جلب طلبات المستخدم
            var userOrders = await _unitOfWork.Orders.GetAsync(o => o.UserId == userId);
            var orderIds = userOrders.Select(o => o.Id).ToList();

            if (!orderIds.Any())
                return false;

            // جلب OrderItems للطلبات دي
            var orderItems = await _unitOfWork.OrderItems.GetAsync(
                oi => orderIds.Contains(oi.OrderId) && oi.ProductId == productId);

            return orderItems.Any();
        }

        public async Task<IActionResult> ByCategory(int id)
        {
            var category = await _unitOfWork.Categories.GetByIdAsync(id);

            if (category == null || !category.IsActive)
            {
                return NotFound();
            }

            var products = await _unitOfWork.Products.GetAsync(p => p.CategoryId == id && p.IsActive);

            ViewBag.CategoryName = category.Name;
            ViewBag.CategoryDescription = category.Description;

            return View("Index", products.ToList());
        }
    }
}