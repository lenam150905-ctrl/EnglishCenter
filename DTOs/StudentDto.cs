namespace EnglishCenter.API.DTOs
{
    public class StudentDto
    {
        public int Id { get; set; }

        public string FullName { get; set; } = string.Empty;

        public DateTime DateOfBirth { get; set; }

        public string Email { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;

        public int? UserId { get; set; }

        public string? UserName { get; set; }
    }
}
