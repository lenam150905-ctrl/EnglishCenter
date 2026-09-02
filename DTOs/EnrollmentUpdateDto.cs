using System.ComponentModel.DataAnnotations;

namespace EnglishCenter.API.DTOs
{
    public class EnrollmentUpdateDto
    {
        [Required]
        public int StudentId { get; set; }

        [Required]
        public int CourseId { get; set; }

        [Required]
        public DateTime EnrollmentDate { get; set; }

        [Required]
        public string Status { get; set; } = string.Empty;
    }
}