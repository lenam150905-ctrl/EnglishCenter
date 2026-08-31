namespace EnglishCenter.API.Models
{
    public class Course
    {
        public int Id { get; set; }

        public string CourseCode { get; set; } = string.Empty;

        public string CourseName { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public int Duration { get; set; }

        public decimal TuitionFee { get; set; }

        public bool IsActive { get; set; } = true;

        public ICollection<Enrollment> Enrollments { get; set; }
            = new List<Enrollment>();

        public ICollection<Schedule> Schedules { get; set; }
            = new List<Schedule>();

        public ICollection<Exam> Exams { get; set; }
            = new List<Exam>();
    }
}