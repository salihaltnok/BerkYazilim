using BerkYazilim.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BerkYazilim.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SuperAdminController : ControllerBase
    {
        private readonly AppDbContext _context;

        public SuperAdminController(AppDbContext context)
        {
            _context = context;
        }

        // Tüm Müşterileri (Admin Rolündekileri) Getir
        [HttpGet("tenants")]
        public async Task<IActionResult> GetTenants()
        {
            var tenants = await _context.Users
                .Where(u => u.Role == "Admin") // Sadece firma sahiplerini getir
                .Select(u => new
                {
                    u.Id,
                    u.FullName,
                    u.DealerCode,
                    u.Phone,
                    u.IsActive,
                    u.SubscriptionEndDate,
                    TotalDealers = _context.Users.Count(d => d.Role == "Dealer") // Altındaki bayi sayısı (Basit mantık)
                })
                .ToListAsync();

            return Ok(tenants);
        }
        [HttpPost("toggle-status/{id}")]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            // 1. Yeni durumu belirle (Mevcut durumun tersi)
            bool newState = !user.IsActive;

            // 2. Admin'in durumunu güncelle
            user.IsActive = newState;

            // 3. EĞER BU KULLANICI "ADMIN" İSE, TÜM BAYİLERİN DURUMUNU DA ONA EŞİTLE
            // (Single Tenant yapısında olduğumuz için veritabanındaki diğer Dealer'ları buluyoruz)
            if (user.Role == "Admin")
            {
                var dealers = await _context.Users
                    .Where(u => u.Role == "Dealer")
                    .ToListAsync();

                foreach (var dealer in dealers)
                {
                    dealer.IsActive = newState;
                }
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Firma ve bayileri güncellendi.", newState = user.IsActive });
        }

        [HttpPost("extend-subscription/{id}")]
        public async Task<IActionResult> ExtendSubscription(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            var baseDate = user.SubscriptionEndDate != null && user.SubscriptionEndDate > DateTime.Now
                ? user.SubscriptionEndDate.Value
                : DateTime.Now;

            user.SubscriptionEndDate = baseDate.AddYears(1);

            // Admin'i aktif et
            user.IsActive = true;

            // --- YENİ EKLENEN KISIM: Bayileri de aktif et ---
            if (user.Role == "Admin")
            {
                var dealers = await _context.Users.Where(u => u.Role == "Dealer").ToListAsync();
                foreach (var dealer in dealers)
                {
                    dealer.IsActive = true;
                }
            }
            // ------------------------------------------------

            await _context.SaveChangesAsync();
            return Ok(new { message = "Süre 1 yıl uzatıldı ve sistem aktif edildi", newDate = user.SubscriptionEndDate });
        }
    }
}