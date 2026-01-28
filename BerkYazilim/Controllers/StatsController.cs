using BerkYazilim.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BerkYazilim.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StatsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public StatsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetDashboardStats()
        {
            // Veritabanından gerçek sayıları çekiyoruz
            var totalOrders = await _context.Orders.CountAsync();

            var pendingOrders = await _context.Orders
                .Where(o => o.Status == "Pending" || o.Status == "Beklemede")
                .CountAsync();

            var completedOrders = await _context.Orders
                .Where(o => o.Status == "Delivered" || o.Status == "Teslim Edildi")
                .CountAsync();

            var totalProducts = await _context.Products.CountAsync();

            var lowStockProducts = await _context.Products
                .Where(p => p.Stock < 20) // Stoğu 20'den az olanlar kritik
                .CountAsync();

            // Frontend'e gönderilecek paket
            return Ok(new
            {
                totalOrders,
                pendingOrders,
                completedOrders,
                totalProducts,
                lowStockProducts
            });
        }
    }
}