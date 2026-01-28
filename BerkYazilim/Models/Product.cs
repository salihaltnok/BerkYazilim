namespace BerkYazilim.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty; // Ürün adı
        public string Category { get; set; } = string.Empty;
        public string CategorySlug { get; set; } = string.Empty; // Filtreleme için (örn: ekran-karti)
        public string Brand { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal OldPrice { get; set; }
        public string? Image { get; set; }
        public int Stock { get; set; }
        public string StockLocations { get; set; } = string.Empty; // "İst:20, Ank:15" gibi
        public bool IsNew { get; set; }
        public bool IsHot { get; set; }
        // Teknik özellikleri virgülle ayrılmış string olarak tutabiliriz veya ayrı tablo yapabiliriz.
        // Şimdilik basit olması için string listesi gibi davranacak bir yapı kuralım.
        public string Specs { get; set; } = string.Empty; // JSON formatında veya virgüllü tutabiliriz.
    }
}