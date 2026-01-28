namespace BerkYazilim.Models
{
    public class SystemSetting
    {
        public int Id { get; set; }

        // Genel Ayarlar
        public string PlatformName { get; set; } = "BerkYazilim B2B";
        public string SupportEmail { get; set; } = "destek@berkyazilim.com";
        public string Currency { get; set; } = "TRY";

        // Güvenlik
        public bool MaintenanceMode { get; set; } = false;
        public bool AllowNewDealers { get; set; } = true;
        public bool EnforceStrongPassword { get; set; } = true;

        // SMTP (E-posta)
        public string SmtpServer { get; set; } = "smtp.office365.com";
        public int SmtpPort { get; set; } = 587;
        public string SmtpUser { get; set; } = "info@berkyazilim.com";
        public string SmtpPassword { get; set; } = ""; // Not: Gerçekte şifreli saklanmalı

        // Varsayılan Bayi Ayarları
        public decimal DefaultCreditLimit { get; set; } = 10000;
        public int DefaultTaxRate { get; set; } = 20;
    }
}