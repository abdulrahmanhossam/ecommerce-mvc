using Microsoft.AspNetCore.Mvc;

namespace ECommerceProject.Controllers
{
    public class FAQController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
