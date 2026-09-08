namespace EnglishCenter.API.DTOs
{
    public class CourseReportDto
    {
        public int CourseId { get; set; }

        public string CourseName { get; set; } = string.Empty;

        public int TotalStudents { get; set; }

        public int TotalExams { get; set; }

        public int TotalGrades { get; set; }

        public int TotalCertificates { get; set; }

        public decimal AverageScore { get; set; }
    }
}