using Microsoft.AspNetCore.Mvc;
using ECommerceProject.Data.Interfaces;
using System.Diagnostics;
using ECommerceProject.Models;

namespace ECommerceProject.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IUnitOfWork _unitOfWork;

        public HomeController(ILogger<HomeController> logger, IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

        public async Task<IActionResult> Index()
        {
            // جلب المنتجات المميزة
            var featuredProducts = await _unitOfWork.Products.GetAsync(p => p.IsFeatured && p.IsActive);
            ViewBag.FeaturedProducts = featuredProducts.Take(8).ToList();

            // جلب الفئات
            var categories = await _unitOfWork.Categories.GetAsync(c => c.IsActive);
            ViewBag.Categories = categories.Take(6).ToList();

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}