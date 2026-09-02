namespace EnglishCenter.API.DTOs
{
    public class EnrollmentDto
    {
        public int Id { get; set; }

        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;

        public int CourseId { get; set; }
        public string CourseName { get; set; } = string.Empty;

        public DateTime EnrollmentDate { get; set; }

        public string Status { get; set; } = string.Empty;
    }
}