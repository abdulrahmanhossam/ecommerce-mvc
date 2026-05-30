using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
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
        private readonly IMemoryCache _memoryCache;

        public ProductsController(IUnitOfWork unitOfWork, IMemoryCache memoryCache)
        {
            _unitOfWork = unitOfWork;
            _memoryCache = memoryCache;
        }

        // GET: Products
        public async Task<IActionResult> Index(int? categoryId, string? searchTerm, decimal? minPrice, decimal? maxPrice,
            string? sortBy, int page = 1)
        {
            var query = _unitOfWork.Products.GetQueryable(asNoTracking: true).Where(p => p.IsActive);

            ApplyFilters(ref query, categoryId, searchTerm, minPrice, maxPrice, sortBy);

            int pageSize = 12;
            var products = await PaginatedList<Product>.CreateAsync(query, page, pageSize);

            var categories = await GetCachedCategoriesAsync();
            ViewBag.Categories = categories;
            ViewBag.SelectedCategoryId = categoryId;
            ViewBag.SearchTerm = searchTerm;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;
            ViewBag.SortBy = sortBy;

            if (Request.Headers.XRequestedWith == "XMLHttpRequest")
                return PartialView("_ProductGrid", products);

            return View(products);
        }

        [HttpGet]
        public async Task<IActionResult> Filter(int? categoryId, string? searchTerm, decimal? minPrice, decimal? maxPrice,
            string? sortBy, int page = 1)
        {
            var query = _unitOfWork.Products.GetQueryable(asNoTracking: true).Where(p => p.IsActive);

            ApplyFilters(ref query, categoryId, searchTerm, minPrice, maxPrice, sortBy);

            int pageSize = 12;
            var products = await PaginatedList<Product>.CreateAsync(query, page, pageSize);

            return PartialView("_ProductGrid", products);
        }

        private static void ApplyFilters(ref IQueryable<Product> query, int? categoryId, string? searchTerm,
            decimal? minPrice, decimal? maxPrice, string? sortBy)
        {
            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryId == categoryId.Value);

            if (!string.IsNullOrEmpty(searchTerm))
                query = query.Where(p =>
                    p.Name.Contains(searchTerm) ||
                    (p.Description != null && p.Description.Contains(searchTerm)));

            if (minPrice.HasValue)
                query = query.Where(p => p.Price >= minPrice.Value);

            if (maxPrice.HasValue)
                query = query.Where(p => p.Price <= maxPrice.Value);

            query = sortBy switch
            {
                "price_asc" => query.OrderBy(p => p.Price),
                "price_desc" => query.OrderByDescending(p => p.Price),
                "name_asc" => query.OrderBy(p => p.Name),
                "name_desc" => query.OrderByDescending(p => p.Name),
                "newest" => query.OrderByDescending(p => p.CreatedDate),
                _ => query.OrderByDescending(p => p.CreatedDate)
            };
        }

        private async Task<List<Category>> GetCachedCategoriesAsync()
        {
            return await _memoryCache.GetOrCreateAsync("ProductCategories", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
                entry.SlidingExpiration = TimeSpan.FromMinutes(2);
                return (await _unitOfWork.Categories.GetAsync(c => c.IsActive, asNoTracking: true)).ToList();
            }) ?? [];
        }

        // GET: Products/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(id, asNoTracking: true);

            if (product == null || !product.IsActive)
            {
                return NotFound();
            }

            var category = await _unitOfWork.Categories.GetByIdAsync(product.CategoryId, asNoTracking: true);
            ViewBag.Category = category;

            var variants = await _unitOfWork.ProductVariants.GetAsync(v => v.ProductId == id, asNoTracking: true);
            ViewBag.ProductVariants = variants.ToList();

            var relatedProducts = await _unitOfWork.Products.GetAsync(
                p => p.CategoryId == product.CategoryId && p.Id != id && p.IsActive, asNoTracking: true);
            ViewBag.RelatedProducts = relatedProducts.Take(4).ToList();

            var reviews = await _unitOfWork.ProductReviews.GetAsync(
                r => r.ProductId == id && r.IsApproved, asNoTracking: true);
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
            catch
            {
                TempData["ErrorMessage"] = "Failed to submit review. Please try again.";
            }

            return RedirectToAction(nameof(Details), new { id = model.ProductId });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
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

        private async Task<bool> HasPurchasedProduct(string userId, int productId)
        {
            return await _unitOfWork.OrderItems.GetQueryable(asNoTracking: true)
                .AnyAsync(oi => oi.ProductId == productId && oi.Order.UserId == userId);
        }

        public async Task<IActionResult> ByCategory(int id, int page = 1)
        {
            var category = await _unitOfWork.Categories.GetByIdAsync(id, asNoTracking: true);

            if (category == null || !category.IsActive)
            {
                return NotFound();
            }

            var query = _unitOfWork.Products.GetQueryable(asNoTracking: true)
                .Where(p => p.CategoryId == id && p.IsActive)
                .OrderByDescending(p => p.CreatedDate);
            var products = await PaginatedList<Product>.CreateAsync(query, page, 12);

            var categories = await GetCachedCategoriesAsync();
            ViewBag.Categories = categories;
            ViewBag.SelectedCategoryId = id;
            ViewBag.SearchTerm = null;
            ViewBag.MinPrice = null;
            ViewBag.MaxPrice = null;
            ViewBag.SortBy = "newest";
            ViewBag.CategoryName = category.Name;
            ViewBag.CategoryDescription = category.Description;

            if (Request.Headers.XRequestedWith == "XMLHttpRequest")
                return PartialView("_ProductGrid", products);

            return View("Index", products);
        }
    }
}