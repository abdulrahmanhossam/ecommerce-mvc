using Microsoft.AspNetCore.Mvc;

namespace ECommerceProject.Controllers
{
    public class AboutController : Controller
    {
        [ResponseCache(Duration = 3600)]
        public IActionResult Index()
        {
            return View();
        }
    }
}
