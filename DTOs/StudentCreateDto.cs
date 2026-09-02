using System.ComponentModel.DataAnnotations;

namespace EnglishCenter.API.DTOs
{
    public class StudentCreateDto
    {
        [Required]
        public string FullName { get; set; } = string.Empty;

        [Required]
        public DateTime DateOfBirth { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Phone { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;

        public int? UserId { get; set; }
    }
}