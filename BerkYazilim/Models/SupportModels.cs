using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BerkYazilim.Models
{
    // Destek Talebi Başlığı
    public class SupportTicket
    {
        public int Id { get; set; }
        public string TicketNumber { get; set; } // TKT-2026-001
        public string Subject { get; set; }
        public string Priority { get; set; } // Düşük, Orta, Yüksek
        public string Status { get; set; } = "Open"; // Open, Answered, Closed
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime LastUpdate { get; set; } = DateTime.Now;

        public int UserId { get; set; }
        public User User { get; set; }

        public List<SupportMessage> Messages { get; set; }
    }

    // Talebin içindeki mesajlar
    public class SupportMessage
    {
        public int Id { get; set; }
        public string Message { get; set; }
        public DateTime SentDate { get; set; } = DateTime.Now;
        public bool IsAgent { get; set; } // True ise yetkili, False ise bayi yazmıştır.

        public int SupportTicketId { get; set; }
        public SupportTicket SupportTicket { get; set; }
    }

    // Sıkça Sorulan Sorular
    public class FAQ
    {
        public int Id { get; set; }
        public string Question { get; set; }
        public string Answer { get; set; }
        public string Category { get; set; }
    }
}