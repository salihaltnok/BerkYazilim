namespace BerkYazilim.Models
{
    public class User
    {
        public int Id { get; set; }
        public string DealerCode { get; set; } = string.Empty; // Bayi Kodu (Örn: BAYI-2025-001)
        public string Password { get; set; } = string.Empty; // Şimdilik düz tutalım, sonra hashleyeceğiz.
        public string FullName { get; set; } = string.Empty; // Örn: Ahmet Bilişim
        public string Role { get; set; } = "Dealer"; // Bayi
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? TaxOffice { get; set; } // Vergi Dairesi
        public string? TaxNumber { get; set; } // Vergi No
        public string? Address { get; set; }   // Adres
    }
}