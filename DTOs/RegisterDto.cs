namespace EnglishCenter.API.DTOs
{
    public class RegisterDto
    {
        [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.StringLength(50, MinimumLength = 3)]
        public string UserName { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.StringLength(128, MinimumLength = 8)]
        public string Password { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.EmailAddress, System.ComponentModel.DataAnnotations.StringLength(254)]
        public string Email { get; set; } = string.Empty;
    }
}
