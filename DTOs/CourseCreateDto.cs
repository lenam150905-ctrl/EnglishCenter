using System.ComponentModel.DataAnnotations;

namespace EnglishCenter.API.DTOs
{
    public class CourseCreateDto
    {
        [Required(ErrorMessage = "Mã khóa học không được để trống")]
        [StringLength(20, ErrorMessage = "Mã khóa học tối đa 20 ký tự")]
        public string CourseCode { get; set; }

        [Required(ErrorMessage = "Tên khóa học không được để trống")]
        [StringLength(100, ErrorMessage = "Tên khóa học tối đa 100 ký tự")]
        public string CourseName { get; set; }

        [Range(0, 1000000000, ErrorMessage = "Học phí phải từ 0 đến 1 tỷ")]
        public decimal TuitionFee { get; set; }
    }
}