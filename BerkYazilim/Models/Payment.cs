using System.ComponentModel.DataAnnotations;

namespace BerkYazilim.Models
{
    public class Payment
    {
        public int Id { get; set; }
        public int UserId { get; set; }

        // DÜZELTME: User? (soru işareti) ekledik. Artık nullable.
        public virtual User? User { get; set; }

        public DateTime Date { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Ödeme tutarı 0'dan büyük olmalıdır.")]
        public decimal Amount { get; set; }

        [Required]
        public string Method { get; set; } = "Havale";

        public string Description { get; set; } = string.Empty;

        public string Status { get; set; } = "Pending";

        public string Type { get; set; } = "Credit";
    }
}