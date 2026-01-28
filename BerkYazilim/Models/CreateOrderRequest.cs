using System.Collections.Generic;

namespace BerkYazilim.Models
{
    public class CreateOrderRequest
    {
        // "internal" kelimesini sildik, artık dışarıdan veri alabilir
        public string DealerCode { get; set; } = string.Empty;

        public List<CartItemDto> Items { get; set; } = new List<CartItemDto>();
    }

    public class CartItemDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        // Buradaki DealerCode'a gerek yok aslında ama kalsın zararı yok
        public string DealerCode { get; set; } = string.Empty;
    }
}