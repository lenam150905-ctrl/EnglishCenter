namespace EnglishCenter.API.DTOs
{
    public class GradeDto
    {
        public int Id { get; set; }

        public int ExamId { get; set; }

        public string ExamName { get; set; } = string.Empty;

        public int StudentId { get; set; }

        public string StudentName { get; set; } = string.Empty;

        public decimal Score { get; set; }

        public string Comment { get; set; } = string.Empty;
    }
}