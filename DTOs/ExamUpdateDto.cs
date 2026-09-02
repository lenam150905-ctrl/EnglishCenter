using System.ComponentModel.DataAnnotations;

namespace EnglishCenter.API.DTOs
{
    public class ExamUpdateDto
    {
        [Required]
        public string ExamName { get; set; } = string.Empty;

        [Required]
        public string ExamType { get; set; } = string.Empty;

        [Required]
        public DateTime ExamDate { get; set; }

        public int? CourseId { get; set; }
    }
}