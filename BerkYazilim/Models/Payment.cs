using System;

namespace BerkYazilim.Models
{
    public class Payment
    {
        public int Id { get; set; }
        public DateTime Date { get; set; } = DateTime.Now;
        public decimal Amount { get; set; }
        public string Method { get; set; } // "Kredi Kartı", "Havale", "Çek"
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending"; // Pending, Approved

        // Hangi Bayi ödedi?
        public int UserId { get; set; }
        public User User { get; set; }
    }
}