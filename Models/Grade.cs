namespace EnglishCenter.API.Models
{
    public class Grade
    {
        public int Id { get; set; }

        public int ExamId { get; set; }

        public int StudentId { get; set; }

        public decimal Score { get; set; }

        public string Comment { get; set; } = string.Empty;

        public Exam? Exam { get; set; }

        public Student? Student { get; set; }
    }
}