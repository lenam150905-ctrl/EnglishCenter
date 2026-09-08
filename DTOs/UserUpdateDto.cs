using System.ComponentModel.DataAnnotations;

namespace EnglishCenter.API.DTOs
{
    public class UserUpdateDto
    {
        [Required]
        public string UserName { get; set; } = string.Empty;

        public string? Password { get; set; }
        public string Email { get; set; } = string.Empty;
        [Required]
        public string Role { get; set; } = string.Empty;
    }
}