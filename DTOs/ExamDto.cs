namespace EnglishCenter.API.DTOs
{
    public class ExamDto
    {
        public int Id { get; set; }

        public string ExamName { get; set; } = string.Empty;

        public string ExamType { get; set; } = string.Empty;

        public DateTime ExamDate { get; set; }

        public int? CourseId { get; set; }

        public string? CourseName { get; set; }
    }
}