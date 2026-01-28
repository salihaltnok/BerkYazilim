using System;
using System.ComponentModel.DataAnnotations;

namespace BerkYazilim.Models
{
    public class ServiceRequest
    {
        public int Id { get; set; }

        [Required]
        public string ServiceCode { get; set; } // Örn: SRV-2026-105

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public string Category { get; set; } // Ekran Kartı, İşlemci vb.
        public string ProductName { get; set; }
        public string SerialNumber { get; set; }
        public string IssueDescription { get; set; }

        public string Status { get; set; } = "waiting"; // waiting, progress, testing, completed

        // Hangi Bayi açtı?
        public int UserId { get; set; }
        public User User { get; set; }
    }
}