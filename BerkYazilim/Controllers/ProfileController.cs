using BerkYazilim.Data;
using BerkYazilim.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BerkYazilim.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProfileController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ProfileController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/profile/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetProfile(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound("Kullanıcı bulunamadı.");

            // --- İSTATİSTİKLERİ HESAPLA ---
            // Kullanıcının toplam sipariş sayısı
            var orderCount = await _context.Orders.CountAsync(o => o.UserId == id);

            // Kullanıcının toplam harcaması (Ciro)
            var totalSpend = await _context.Orders
                                    .Where(o => o.UserId == id)
                                    .SumAsync(o => o.TotalAmount);

            return Ok(new
            {
                fullName = user.FullName,
                dealerCode = user.DealerCode,
                role = user.Role,
                email = user.Email,
                phone = user.Phone,
                address = user.Address,
                taxOffice = user.TaxOffice,
                taxNumber = user.TaxNumber,
                creditLimit = user.CreditLimit, // Kredi limitini de gönderiyoruz

                // Yeni Eklenenler:
                orderCount = orderCount,
                totalSpend = totalSpend
            });
        }

        // PUT: api/profile/5/update-info  <-- ID'yi URL'den alıyoruz
        [HttpPut("{id}/update-info")]
        public async Task<IActionResult> UpdateInfo(int id, [FromBody] UpdateProfileRequest request)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound("Kullanıcı bulunamadı.");

            user.FullName = request.FullName;
            user.Phone = request.Phone;
            user.Email = request.Email;
            user.Address = request.Address;
            user.TaxOffice = request.TaxOffice;
            user.TaxNumber = request.TaxNumber;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Profil bilgileri başarıyla güncellendi." });
        }

        // PUT: api/profile/5/update-password <-- ID'yi URL'den alıyoruz
        [HttpPut("{id}/update-password")]
        public async Task<IActionResult> UpdatePassword(int id, [FromBody] ChangePasswordRequest request)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound("Kullanıcı bulunamadı.");

            if (user.Password != request.OldPassword)
            {
                return BadRequest("Mevcut şifreniz hatalı!");
            }

            user.Password = request.NewPassword;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Şifreniz başarıyla güncellendi." });
        }
    }

    // Request sınıfları aynı kalabilir (aşağıda tekrar yazmıyorum, User.cs içinde veya burada tanımlı kalabilir)
    public class ChangePasswordRequest { public string OldPassword { get; set; } public string NewPassword { get; set; } }
    public class UpdateProfileRequest { public string FullName { get; set; } public string? Phone { get; set; } public string? Email { get; set; } public string? Address { get; set; } public string? TaxOffice { get; set; } public string? TaxNumber { get; set; } }
}