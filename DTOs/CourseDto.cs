using System.ComponentModel.DataAnnotations;

namespace EnglishCenter.API.DTOs
{
    public class CourseDto
    {
        public int Id { get; set; }

        public string CourseCode { get; set; }
        public int Duration { get; set; }
        public string Description { get; set; }
        public string CourseName { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal TuitionFee { get; set; }
    }
}