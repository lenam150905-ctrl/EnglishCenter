namespace EnglishCenter.API.DTOs
{
    public class ExamStartCheckDto
    {
        public bool CanStart { get; set; }

        public string Message { get; set; } = string.Empty;

        public int ExamId { get; set; }

        public int StudentId { get; set; }

        public int? EnrollmentId { get; set; }

        public int AttemptCount { get; set; }

        public bool HasAttempted { get; set; }
    }
}