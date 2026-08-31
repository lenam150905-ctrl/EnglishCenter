namespace EnglishCenter.API.Models
{
    public class Schedule
    {
        public int Id { get; set; }

        public int CourseId { get; set; }

        public int TeacherId { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        public string Room { get; set; } = string.Empty;

        public Course? Course { get; set; }

        public Teacher? Teacher { get; set; }
    }
}