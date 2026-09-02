using System.ComponentModel.DataAnnotations;

namespace EnglishCenter.API.DTOs
{
    public class EnrollmentCreateDto
    {
        [Required]
        public int StudentId { get; set; }

        [Required]
        public int CourseId { get; set; }

        public DateTime EnrollmentDate { get; set; } = DateTime.Now;

        [Required]
        public string Status { get; set; } = "Active";
    }
}