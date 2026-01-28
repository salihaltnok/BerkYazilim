namespace BerkYazilim.Models
{
    // Ödeme İsteği
    public class PaymentRequest
    {
        public string DealerCode { get; set; }
        public decimal Amount { get; set; }
        public string Method { get; set; }
    }

    // Servis Talebi İsteği
    public class ServiceRequestDTO
    {
        public string DealerCode { get; set; }
        public string Category { get; set; }
        public string Product { get; set; }
        public string Serial { get; set; }
        public string Description { get; set; }
    }

    // Yeni Destek Talebi İsteği
    public class CreateTicketDto
    {
        public string DealerCode { get; set; }
        public string Subject { get; set; }
        public string Priority { get; set; }
        public string Message { get; set; }
    }

    // Talebe Cevap Yazma İsteği
    public class ReplyTicketDto
    {
        public string TicketNumber { get; set; }
        public string Message { get; set; }
    }

    // Servis Durumu Güncelleme İsteği (Admin İçin)
    public class UpdateServiceDto
    {
        public string ServiceCode { get; set; }
        public string NewStatus { get; set; }
    }

    // Sipariş Durumu Güncelleme İsteği (Admin İçin)
    public class UpdateStatusRequest
    {
        public string Status { get; set; }
        public string? TrackingNumber { get; set; }
    }
}