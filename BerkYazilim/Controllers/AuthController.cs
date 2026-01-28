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
            // 1. ÖNCE KULLANICIYI SADECE KOD İLE BUL (Şifre kontrolünü SQL'de yapmıyoruz)
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.DealerCode == request.DealerCode);

            // 2. Kullanıcı yoksa hata dön
            if (user == null)
            {
                return Unauthorized(new { error = "Hatalı Bayi Kodu veya Şifre!" });
            }

            // 3. ŞİFRE DOĞRULAMA (BCrypt Verify)
            // Girilen şifre ile veritabanındaki hash eşleşiyor mu?
            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.Password);

            if (!isPasswordValid)
            {
                return Unauthorized(new { error = "Hatalı Bayi Kodu veya Şifre!" });
            }

            // 4. PASİFLİK VE ABONELİK KONTROLLERİ (Aynen koruyoruz)
            if (user.Role != "SuperAdmin")
            {
                if (!user.IsActive)
                {
                    return BadRequest(new { error = "Hesabınız pasife alınmıştır. Lütfen Berk Yazılım ile görüşün." });
                }

                if (user.SubscriptionEndDate.HasValue && user.SubscriptionEndDate < DateTime.Now)
                {
                    return BadRequest(new { error = "Abonelik süreniz dolmuştur. Lütfen üyeliğinizi yenileyin." });
                }
            }
            var log = new AuditLog
            {
                UserId = user.Id,
                UserName = user.FullName,
                Action = "Giriş Başarılı",
                Details = $"{user.Role} rolüyle giriş yapıldı.",
                // Önce Header'a bak (Gerçek IP), yoksa bağlantı IP'sini al
                IpAddress = Request.Headers.ContainsKey("X-Forwarded-For")
    ? Request.Headers["X-Forwarded-For"].ToString()
    : (HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Bilinmiyor"),
                Timestamp = DateTime.Now
            };
            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();
            // --- LOGLAMA BİTİŞ ---
            // 5. Giriş Başarılı
            return Ok(GenerateLoginResponse(user));
        }

        // --- YENİ: GHOST LOGIN (IMPERSONATION) ---
        // Sadece SuperAdmin'in kullanabileceği özel bir kapı
        [HttpPost("impersonate")]
        public async Task<IActionResult> Impersonate([FromBody] int targetUserId)
        {
            // Gerçek projede burada [Authorize(Roles="SuperAdmin")] attribute'ü ve Header kontrolü olur.
            // Şimdilik sadece isteği yapanın kimliğini basitçe kontrol edelim veya
            // frontend'de SuperAdmin panelinden tetiklendiğini varsayalım.

            var targetUser = await _context.Users.FindAsync(targetUserId);
            if (targetUser == null) return NotFound("Kullanıcı bulunamadı.");

            // Şifre sormadan direkt giriş bilgilerini dönüyoruz!
            return Ok(GenerateLoginResponse(targetUser));
        }

        // Cevap formatını tekilleştirdik
        private object GenerateLoginResponse(User user)
        {
            return new
            {
                token = "fake-jwt-" + user.Role, // Rolü token içine gömdük (basit simülasyon)
                bayi = new
                {
                    id = user.Id,
                    ad = user.FullName,
                    kod = user.DealerCode,
                    rol = user.Role
                }
            };
        }
    }
}