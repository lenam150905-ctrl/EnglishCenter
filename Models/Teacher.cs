namespace EnglishCenter.API.Models
{
    public class Teacher
    {
        public int Id { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;

        public string Specialization { get; set; } = string.Empty;

        public int? UserId { get; set; }

        public User? User { get; set; }

        public ICollection<Schedule> Schedules { get; set; }
            = new List<Schedule>();
    }
}