namespace EnglishCenter.API.DTOs
{
    public class RegisterDto
    {
        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Vui lòng nhập tên đăng nhập."), System.ComponentModel.DataAnnotations.StringLength(50, MinimumLength = 3, ErrorMessage = "Tên đăng nhập phải có từ 3 đến 50 ký tự.")]
        public string UserName { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Vui lòng nhập mật khẩu."), System.ComponentModel.DataAnnotations.StringLength(128, MinimumLength = 6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự.")]
        public string Password { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Vui lòng nhập email."), System.ComponentModel.DataAnnotations.EmailAddress(ErrorMessage = "Email không hợp lệ."), System.ComponentModel.DataAnnotations.StringLength(254)]
        public string Email { get; set; } = string.Empty;
    }
}
