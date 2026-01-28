using System.ComponentModel.DataAnnotations;

namespace BerkYazilim.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Ürün adı boş olamaz.")]
        [StringLength(200, ErrorMessage = "Ürün adı çok uzun.")]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Category { get; set; } = string.Empty;
        public string CategorySlug { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;

        public string ImageUrl { get; set; } = string.Empty; // <--- Burası ImageUrl olmalı

        [Range(0.01, 1000000, ErrorMessage = "Fiyat 0 veya negatif olamaz.")]
        public decimal Price { get; set; }

        public decimal OldPrice { get; set; }

        [Range(0, 100000, ErrorMessage = "Stok adedi negatif olamaz.")]
        public int Stock { get; set; }

        public string StockLocations { get; set; } = string.Empty;
        public bool IsNew { get; set; }
        public bool IsHot { get; set; }
        public string Specs { get; set; } = string.Empty;
    }
}