using BerkYazilim.Data;
using BerkYazilim.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BerkYazilim.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SettingsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public SettingsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/settings
        [HttpGet]
        public async Task<ActionResult<SystemSetting>> GetSettings()
        {
            var settings = await _context.SystemSettings.FirstOrDefaultAsync();
            if (settings == null)
            {
                // Eğer yoksa varsayılan oluştur
                settings = new SystemSetting();
                _context.SystemSettings.Add(settings);
                await _context.SaveChangesAsync();
            }
            return settings;
        }

        // POST: api/settings
        [HttpPost]
        public async Task<IActionResult> UpdateSettings([FromBody] SystemSetting updatedSettings)
        {
            var settings = await _context.SystemSettings.FirstOrDefaultAsync();
            if (settings == null) return NotFound("Ayar kaydı bulunamadı.");

            // Değerleri güncelle
            settings.PlatformName = updatedSettings.PlatformName;
            settings.SupportEmail = updatedSettings.SupportEmail;
            settings.Currency = updatedSettings.Currency;
            settings.MaintenanceMode = updatedSettings.MaintenanceMode;
            settings.AllowNewDealers = updatedSettings.AllowNewDealers;
            settings.EnforceStrongPassword = updatedSettings.EnforceStrongPassword;
            settings.SmtpServer = updatedSettings.SmtpServer;
            settings.SmtpPort = updatedSettings.SmtpPort;
            settings.SmtpUser = updatedSettings.SmtpUser;
            settings.SmtpPassword = updatedSettings.SmtpPassword;
            settings.DefaultCreditLimit = updatedSettings.DefaultCreditLimit;
            settings.DefaultTaxRate = updatedSettings.DefaultTaxRate;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Ayarlar başarıyla güncellendi." });
        }
    }
}