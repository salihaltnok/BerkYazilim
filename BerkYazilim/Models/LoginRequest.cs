using System.Text.Json.Serialization;

namespace BerkYazilim.Models
{
    public class LoginRequest
    {
        [JsonPropertyName("bayi_kodu")]
        public string DealerCode { get; set; } = string.Empty;

        [JsonPropertyName("sifre")]
        public string Password { get; set; } = string.Empty;
    }
}