using Microsoft.AspNetCore.Mvc;

namespace ECommerceProject.Controllers
{
    public class AboutController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
