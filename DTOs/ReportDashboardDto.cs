namespace EnglishCenter.API.DTOs
{
    public class ReportDashboardDto
    {
        public int TotalStudents { get; set; }

        public int TotalCourses { get; set; }

        public int TotalEnrollments { get; set; }

        public int TotalExams { get; set; }

        public int TotalGrades { get; set; }

        public int TotalCertificates { get; set; }

        public decimal TotalRevenue { get; set; }
    }
}