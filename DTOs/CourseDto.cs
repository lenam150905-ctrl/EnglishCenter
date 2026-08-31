using System.ComponentModel.DataAnnotations;

namespace EnglishCenter.API.DTOs
{
    public class CourseDto
    {
        public int Id { get; set; }

        public string CourseCode { get; set; }

        public string CourseName { get; set; }

        public decimal TuitionFee { get; set; }
    }
}