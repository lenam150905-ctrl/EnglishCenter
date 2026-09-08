namespace EnglishCenter.API.Models
{
    public class User
    {
        public int Id { get; set; }

        public string UserName { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;

        public string Role { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool IsDeleted { get; set; } = false;
        public Teacher? Teacher { get; set; }

        public Student? Student { get; set; }
    }
}