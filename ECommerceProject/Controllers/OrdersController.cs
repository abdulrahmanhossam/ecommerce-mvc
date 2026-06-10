using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using ECommerceProject.Data.Interfaces;
using ECommerceProject.Models.ViewModels;

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
        public async Task<IActionResult> MyOrders(int page = 1)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var query = _unitOfWork.Orders.GetQueryable(asNoTracking: true)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate);

            var orders = await PaginatedList<ECommerceProject.Models.Entities.Order>.CreateAsync(query, page, 10);

            return View(orders);
        }

        // GET: Orders/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var order = await _unitOfWork.Orders.GetByIdAsync(id, asNoTracking: true);

            if (order == null || order.UserId != userId)
            {
                return NotFound();
            }

            var orderItems = await _unitOfWork.OrderItems.GetQueryable(asNoTracking: true)
                .Where(oi => oi.OrderId == id)
                .Include(oi => oi.Product)
                .ToListAsync();

            ViewBag.OrderItems = orderItems
                .Select(item => (item, item.Product))
                .ToList();

            // جلب Payment
            var payment = await _unitOfWork.Payments.GetFirstOrDefaultAsync(p => p.OrderId == id, asNoTracking: true);
            ViewBag.Payment = payment;

            return View(order);
        }
    }
}