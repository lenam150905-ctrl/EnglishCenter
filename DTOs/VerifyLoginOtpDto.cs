namespace EnglishCenter.API.DTOs
{
    public class VerifyLoginOtpDto
    {
        public string UserName { get; set; } = string.Empty;

        public string Otp { get; set; } = string.Empty;
    }
}