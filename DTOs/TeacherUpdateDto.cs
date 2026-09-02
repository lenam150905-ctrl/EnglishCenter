using System.ComponentModel.DataAnnotations;

namespace EnglishCenter.API.DTOs
{
    public class TeacherUpdateDto
    {
        [Required(ErrorMessage = "Họ tên không được để trống")]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Số điện thoại không được để trống")]
        [StringLength(20)]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Chuyên môn không được để trống")]
        [StringLength(100)]
        public string Specialization { get; set; } = string.Empty;

        public int? UserId { get; set; }
    }
}