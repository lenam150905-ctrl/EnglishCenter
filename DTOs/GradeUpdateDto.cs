using System.ComponentModel.DataAnnotations;

namespace EnglishCenter.API.DTOs
{
    public class GradeUpdateDto
    {
        [Required]
        public int ExamId { get; set; }

        [Required]
        public int StudentId { get; set; }

        [Range(0, 10)]
        public decimal Score { get; set; }

        public string Comment { get; set; } = string.Empty;
    }
}