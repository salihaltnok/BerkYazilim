using System.ComponentModel.DataAnnotations; 

namespace BerkYazilim.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Bayi kodu zorunludur.")]
        [StringLength(50, ErrorMessage = "Bayi kodu en fazla 50 karakter olabilir.")]
        public string DealerCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Şifre zorunludur.")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ad Soyad / Firma Adı zorunludur.")]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        public string Role { get; set; } = "Dealer";

        // Güvenlik Alanları
        public bool IsActive { get; set; } = true;
        public DateTime? SubscriptionEndDate { get; set; }

        [EmailAddress(ErrorMessage = "Geçerli bir E-posta adresi giriniz.")]
        public string? Email { get; set; }

        [Phone(ErrorMessage = "Geçerli bir telefon numarası giriniz.")]
        public string? Phone { get; set; }

        public string? TaxOffice { get; set; }
        public string? TaxNumber { get; set; }
        public string? Address { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Kredi limiti 0'dan küçük olamaz.")]
        public decimal CreditLimit { get; set; } = 0;
    }
}