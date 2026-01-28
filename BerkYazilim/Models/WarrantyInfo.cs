using System;
using System.ComponentModel.DataAnnotations;

namespace BerkYazilim.Models
{
    public class WarrantyInfo
    {
        public int Id { get; set; }

        [Required]
        public string SerialNumber { get; set; } // Sorgulama Anahtarı (Örn: SN-12345)

        public string ProductName { get; set; }  // Örn: MSI RTX 4090

        public DateTime PurchaseDate { get; set; } // Fatura Tarihi

        public int WarrantyPeriodMonths { get; set; } // Garanti Süresi (Ay cinsinden, örn: 24, 36)

        // Veritabanına kaydedilmeyen, anlık hesaplanan özellikler
        public DateTime ExpiryDate => PurchaseDate.AddMonths(WarrantyPeriodMonths);

        public bool IsActive => DateTime.Now < ExpiryDate;

        public string RemainingTime
        {
            get
            {
                if (!IsActive) return "Süre Doldu";
                var span = ExpiryDate - DateTime.Now;
                int years = (int)(span.Days / 365.25);
                int months = (int)((span.Days % 365.25) / 30);
                return $"{(years > 0 ? years + " Yıl " : "")}{months} Ay";
            }
        }
    }
}