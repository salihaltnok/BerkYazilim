using System.ComponentModel.DataAnnotations;

namespace BerkYazilim.Models
{
    public class LoginRequest
    {
        [Required(ErrorMessage = "Bayi kodu girmediniz.")]
        public string DealerCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Şifre girmediniz.")]
        public string Password { get; set; } = string.Empty;
    }
}