using Microsoft.AspNetCore.Mvc;
using ECommerceProject.Services.Interfaces;

namespace ECommerceProject.Controllers
{
    public class ContactController : Controller
    {
        private readonly IEmailService _emailService;
        private readonly string _contactEmail;

        public ContactController(IEmailService emailService, IConfiguration configuration)
        {
            _emailService = emailService;
            _contactEmail = configuration["EmailSettings:ContactEmail"] ?? "ataba.contact@example.com";
        }

        [ResponseCache(Duration = 3600)]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Send(string name, string email, string subject, string message)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(message))
            {
                TempData["ErrorMessage"] = "All fields are required.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                await _emailService.SendEmailAsync(
                    _contactEmail,
                    $"Contact Form: {subject}",
                    $"From: {name} ({email})<br/><br/>{message}");

                TempData["SuccessMessage"] = "Your message has been sent. We'll get back to you soon.";
            }
            catch
            {
                TempData["ErrorMessage"] = "Something went wrong. Please try again later.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
