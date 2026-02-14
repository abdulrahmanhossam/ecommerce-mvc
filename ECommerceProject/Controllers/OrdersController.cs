using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ECommerceProject.Data.Interfaces;

namespace ECommerceProject.Controllers
{
    [Authorize]
    public class OrdersController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public OrdersController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // GET: Orders/MyOrders
        public async Task<IActionResult> MyOrders()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var orders = await _unitOfWork.Orders.GetAsync(o => o.UserId == userId);
            var ordersList = orders.OrderByDescending(o => o.OrderDate).ToList();

            return View(ordersList);
        }

        // GET: Orders/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var order = await _unitOfWork.Orders.GetByIdAsync(id);

            if (order == null || order.UserId != userId)
            {
                return NotFound();
            }

            // جلب Order Items مع المنتجات
            var orderItems = await _unitOfWork.OrderItems.GetAsync(oi => oi.OrderId == id);
            var orderItemsWithProducts = new List<(Models.Entities.OrderItem Item, Models.Entities.Product Product)>();

            foreach (var item in orderItems)
            {
                var product = await _unitOfWork.Products.GetByIdAsync(item.ProductId);
                if (product != null)
                {
                    orderItemsWithProducts.Add((item, product));
                }
            }

            ViewBag.OrderItems = orderItemsWithProducts;

            // جلب Payment
            var payment = await _unitOfWork.Payments.GetFirstOrDefaultAsync(p => p.OrderId == id);
            ViewBag.Payment = payment;

            return View(order);
        }
    }
}