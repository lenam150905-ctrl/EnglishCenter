namespace EnglishCenter.API.DTOs
{
    public class ResetPasswordDto
    {
        public string UserName { get; set; } = string.Empty;
        public string Otp { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;

        public string ConfirmPassword { get; set; } = string.Empty;
    }
}