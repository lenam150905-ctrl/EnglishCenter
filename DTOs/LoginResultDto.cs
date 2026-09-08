namespace EnglishCenter.API.DTOs
{
    public class LoginResultDto
    {
        public bool RequiresTwoFactor { get; set; }

        public string UserName { get; set; } = string.Empty;

        public AuthResponseDto? Auth { get; set; }
    }
}