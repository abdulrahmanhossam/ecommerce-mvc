using Microsoft.AspNetCore.Mvc;
using ECommerceProject.Data.Interfaces;

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
            // جلب كل المنتجات
            var products = await _unitOfWork.Products.GetAllAsync();

            // تحميل  Categories (Eager Loading)
            var productsWithCategories = products.Select(p => new
            {
                Product = p,
                CategoryId = p.CategoryId
            });

            // فلترة حسب Category
            if (categoryId.HasValue)
            {
                products = products.Where(p => p.CategoryId == categoryId.Value);
            }

            // بحث حسب الاسم
            if (!string.IsNullOrEmpty(searchTerm))
            {
                products = products.Where(p =>
                    p.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    (p.Description != null && p.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                );
            }

            // فلترة حسب السعر
            if (minPrice.HasValue)
            {
                products = products.Where(p => p.Price >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                products = products.Where(p => p.Price <= maxPrice.Value);
            }

            // فقط المنتجات النشطة
            products = products.Where(p => p.IsActive);

            // جلب الـ Categories للفلتر
            var categories = await _unitOfWork.Categories.GetAllAsync();
            ViewBag.Categories = categories.Where(c => c.IsActive).ToList();
            ViewBag.SelectedCategoryId = categoryId;
            ViewBag.SearchTerm = searchTerm;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;

            return View(products.ToList());
        }

        // GET: Products/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _unitOfWork.Products.GetByIdAsync(id.Value);

            if (product == null || !product.IsActive)
            {
                return NotFound();
            }

            // جلب الـ Category
            product.Category = await _unitOfWork.Categories.GetByIdAsync(product.CategoryId);

            // جلب الـ Product Variants
            var variants = await _unitOfWork.ProductVariants.GetAsync(v => v.ProductId == id.Value);
            ViewBag.ProductVariants = variants.Where(v => v.IsActive).ToList();

            // جلب منتجات مشابهة من نفس الفئة
            var relatedProducts = await _unitOfWork.Products.GetAsync(p =>
                p.CategoryId == product.CategoryId &&
                p.Id != product.Id &&
                p.IsActive
            );
            ViewBag.RelatedProducts = relatedProducts.Take(4).ToList();

            return View(product);
        }

        // GET: Products/ByCategory/5
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