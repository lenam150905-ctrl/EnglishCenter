namespace EnglishCenter.API.Models
{
    public class User
    {
        public int Id { get; set; }

        public string UserName { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;

        public string Role { get; set; } = string.Empty;

        public Teacher? Teacher { get; set; }

        public Student? Student { get; set; }
    }
}