using System.ComponentModel.DataAnnotations;

namespace BerkYazilim.Models
{
    public class CreateOrderRequest
    {
        [Required(ErrorMessage = "Bayi kodu zorunludur.")]
        public string DealerCode { get; set; } = string.Empty; // <-- Hatayı çözen satır bu

        [Required]
        [MinLength(1, ErrorMessage = "Sepette en az 1 ürün olmalıdır.")]
        public List<OrderItemDto> Items { get; set; } = new();
    }

    public class OrderItemDto
    {
        [Required]
        public int ProductId { get; set; }

        [Range(1, 1000, ErrorMessage = "Sipariş adedi en az 1 olmalıdır.")]
        public int Quantity { get; set; }
    }
}