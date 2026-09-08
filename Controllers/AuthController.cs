using EnglishCenter.API.DTOs;
using EnglishCenter.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace EnglishCenter.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(
            RegisterDto dto)
        {
            var result =
                await _authService.RegisterAsync(dto);

            if (!result)
            {
                return BadRequest(
                    "Tên tài khoản đã tồn tại.");
            }

            return Ok(new
            {
                message = "Đăng ký thành công."
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            var result = await _authService.LoginAsync(dto);

            if (result == null)
            {
                return Unauthorized(new
                {
                    message = "Tên đăng nhập hoặc mật khẩu không đúng."
                });
            }

            return Ok(result);
        }
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(
    ResetPasswordDto dto)
        {
            await _authService.ResetPasswordAsync(dto);

            return Ok(new
            {
                message = "Đặt lại mật khẩu thành công."
            });
        }
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(
    ForgotPasswordDto dto)
        {
            var otp = await _authService.ForgotPasswordAsync(dto);

            return Ok(new
            {
                message = "Đã tạo mã OTP.",
                otp = otp
            });
        }
        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp(
    VerifyOtpDto dto)
        {
            await _authService.VerifyOtpAsync(dto);

            return Ok(new
            {
                message = "Xác minh OTP thành công."
            });
        }
        [HttpPost("send-login-otp")]
        public async Task<IActionResult> SendLoginOtp(
    string userName)
        {
            await _authService.SendLoginOtpAsync(userName);

            return Ok(new
            {
                message = "Mã OTP đã được gửi đến email."
            });
        }
        [HttpPost("verify-login-otp")]
        public async Task<IActionResult> VerifyLoginOtp(
    VerifyLoginOtpDto dto)
        {
            var result =
                await _authService.VerifyLoginOtpAsync(dto);

            return Ok(result);
        }
    }
}