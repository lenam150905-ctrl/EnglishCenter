namespace EnglishCenter.API.DTOs
{
    public class VerifyOtpDto
    {
        public string UserName { get; set; } = string.Empty;

        public string Otp { get; set; } = string.Empty;
    }
}