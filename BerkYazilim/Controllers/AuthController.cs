using BerkYazilim.Data;
using BerkYazilim.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BerkYazilim.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AuthController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var settings = await _context.SystemSettings.FirstOrDefaultAsync();

            // 2. BAKIM MODU KONTROLÜ (Admin hariç herkese kapa)
            if (settings != null && settings.MaintenanceMode)
            {
                var userCheck = await _context.Users.FirstOrDefaultAsync(u => u.DealerCode == request.DealerCode);
                if (userCheck != null && userCheck.Role != "Admin")
                {
                    return BadRequest(new { error = "Sistem şu anda bakım modundadır. Lütfen daha sonra deneyiniz." });
                }
            }

            // 1. Kullanıcıyı veritabanında ara
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.DealerCode == request.DealerCode && u.Password == request.Password);

            // 2. Kullanıcı yoksa veya şifre yanlışsa hata dön
            if (user == null)
            {
                return Unauthorized(new { error = "Hatalı Bayi Kodu veya Şifre!" });
            }

            // 3. Giriş başarılıysa frontend'in beklediği formatta cevap dön
            return Ok(new
            {
                token = "fake-jwt-token-123456", // Şimdilik sahte bir token veriyoruz
                bayi = new
                {
                    id = user.Id,
                    ad = user.FullName,
                    kod = user.DealerCode,
                    rol = user.Role
                }
            });
        }
    }
}