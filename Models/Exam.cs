using System.Diagnostics;

namespace EnglishCenter.API.Models
{
    public class Exam
    {
        public int Id { get; set; }

        public string ExamName { get; set; } = string.Empty;

        public string ExamType { get; set; } = string.Empty;

        public DateTime ExamDate { get; set; }

        public int? CourseId { get; set; }

        public Course? Course { get; set; }

        public ICollection<Grade> Grades { get; set; }
            = new List<Grade>();
    }
}