using System.Collections.Generic;
using System;

namespace BerkYazilim.Models
{
    public class Order
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty; // SIP-2025-0024
        public DateTime OrderDate { get; set; }
        public string Status { get; set; } = "Pending"; // pending, processing, shipped...
        public decimal TotalAmount { get; set; }
        public string Address { get; set; } = string.Empty;
        public string? TrackingNumber { get; set; } // Null olabilir

        // Hangi Bayiye ait olduğu
        public int UserId { get; set; }
        public User User { get; set; }

        public List<OrderItem> Items { get; set; } = new List<OrderItem>();
    }
}
