using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using ECommerceProject.Data.Interfaces;
using System.Diagnostics;
using System.Text;
using ECommerceProject.Models;

namespace ECommerceProject.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMemoryCache _memoryCache;
        private readonly Services.Interfaces.IEmailService _emailService;

        public HomeController(ILogger<HomeController> logger, IUnitOfWork unitOfWork, IMemoryCache memoryCache, Services.Interfaces.IEmailService emailService)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
            _memoryCache = memoryCache;
            _emailService = emailService;
        }

        [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any)]
        public async Task<IActionResult> Index()
        {
            var featuredProducts = await _memoryCache.GetOrCreateAsync("FeaturedProducts", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
                return await _unitOfWork.Products.GetQueryable(asNoTracking: true)
                    .Where(p => p.IsFeatured && p.IsActive)
                    .OrderBy(p => p.Id)
                    .Take(8)
                    .ToListAsync();
            });

            var categories = await _memoryCache.GetOrCreateAsync("HomeCategories", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
                return await _unitOfWork.Categories.GetQueryable(asNoTracking: true)
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.Name)
                    .Take(6)
                    .ToListAsync();
            });

            ViewBag.FeaturedProducts = featuredProducts;
            ViewBag.Categories = categories;

            return View();
        }

        [ResponseCache(Duration = 3600)]
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 3600)]
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

            var sb = new StringBuilder();
            sb.Append(@"<?xml version=""1.0"" encoding=""UTF-8""?>
<urlset xmlns=""http://www.sitemaps.org/schemas/sitemap/0.9"">");

            foreach (var url in urls)
            {
                sb.Append(@"
  <url>
    <loc>").Append(url).Append(@"</loc>
    <changefreq>weekly</changefreq>
    <priority>0.8</priority>
  </url>");
            }

            sb.Append(@"
</urlset>");

            return Content(sb.ToString(), "application/xml");
        }
    }
}