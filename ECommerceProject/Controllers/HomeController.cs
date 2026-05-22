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
        private readonly Services.Interfaces.IEmailService _emailService;

        public HomeController(ILogger<HomeController> logger, IUnitOfWork unitOfWork, Services.Interfaces.IEmailService emailService)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
            _emailService = emailService;
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

        public IActionResult Terms()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Subscribe(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                TempData["ErrorMessage"] = "Please enter a valid email address.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                await _emailService.SendEmailAsync(
                    "shophub.contact@example.com",
                    "New Newsletter Subscription",
                    $"New subscriber: {email}");

                TempData["SuccessMessage"] = "Thank you for subscribing!";
            }
            catch
            {
                TempData["ErrorMessage"] = "Something went wrong. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [ResponseCache(Duration = 86400)]
        public async Task<IActionResult> Sitemap()
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}";

            var urls = new List<string>
            {
                $"{baseUrl}/",
                $"{baseUrl}/Home/Privacy",
                $"{baseUrl}/Home/Terms",
                $"{baseUrl}/About",
                $"{baseUrl}/Contact",
                $"{baseUrl}/FAQ",
                $"{baseUrl}/Products",
            };

            var categories = await _unitOfWork.Categories.GetAsync(c => c.IsActive);
            foreach (var category in categories)
            {
                urls.Add($"{baseUrl}/Products?categoryId={category.Id}");
            }

            var products = await _unitOfWork.Products.GetAsync(p => p.IsActive);
            foreach (var product in products)
            {
                urls.Add($"{baseUrl}/Products/Details/{product.Id}");
            }

            var xml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<urlset xmlns=""http://www.sitemaps.org/schemas/sitemap/0.9"">";

            foreach (var url in urls)
            {
                xml += $@"
  <url>
    <loc>{url}</loc>
    <changefreq>weekly</changefreq>
    <priority>0.8</priority>
  </url>";
            }

            xml += @"
</urlset>";

            return Content(xml, "application/xml");
        }
    }
}