namespace EnglishCenter.API.Models
{
    public class VNPaySettings
    {
        public string TmnCode { get; set; } = string.Empty;

        public string HashSecret { get; set; } = string.Empty;

        public string PaymentUrl { get; set; } = string.Empty;

        public string ReturnUrl { get; set; } = string.Empty;
    }
}