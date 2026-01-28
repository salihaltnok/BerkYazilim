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

        // GET: api/profile/1
        // (Normalde ID'yi token'dan alırız ama şimdilik manuel gönderelim veya sabit 1 alalım)
        [HttpGet("{id}")]
        public async Task<IActionResult> GetProfile(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound("Kullanıcı bulunamadı.");

            // Şifreyi güvenlik gereği göndermiyoruz
            return Ok(new
            {
                fullName = user.FullName,
                dealerCode = user.DealerCode,
                role = user.Role,
                email = user.Email ?? "demo@berkyazilim.com", // Varsayılan
                phone = user.Phone ?? "0555 000 00 00",
                address = user.Address ?? "İstanbul, Türkiye"
            });
        }

        // PUT: api/profile/update-password
        [HttpPut("update-password")]
        public async Task<IActionResult> UpdatePassword([FromBody] ChangePasswordRequest request)
        {
            // Şimdilik ID'si 1 olan kullanıcıyı baz alıyoruz (Ahmet Bilişim)
            var user = await _context.Users.FindAsync(1);
            if (user == null) return NotFound("Kullanıcı bulunamadı.");

            // Eski şifre kontrolü
            if (user.Password != request.OldPassword)
            {
                return BadRequest("Mevcut şifreniz hatalı!");
            }

            // Yeni şifreyi kaydet
            user.Password = request.NewPassword;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Şifreniz başarıyla güncellendi." });
        }
    }

    public class ChangePasswordRequest
    {
        public string OldPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }
}