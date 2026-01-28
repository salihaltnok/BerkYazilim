using System;

namespace BerkYazilim.Models
{
    public class AuditLog
    {
        public int Id { get; set; }

        // İşlemi yapan kişi (Null olabilir, örn: hatalı giriş denemesi)
        public int? UserId { get; set; }
        public string UserName { get; set; } = string.Empty; // O anki adını saklayalım

        public string Action { get; set; } = string.Empty; // Örn: "Login", "OrderCreated", "StatusUpdate"
        public string Details { get; set; } = string.Empty; // Örn: "SIP-2026-1234 onaylandı"
        public string IpAddress { get; set; } = string.Empty; // Güvenlik için IP adresi

        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}