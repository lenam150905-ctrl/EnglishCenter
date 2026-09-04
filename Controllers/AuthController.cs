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
        public async Task<IActionResult> Login(
            LoginDto dto)
        {
            var result =
                await _authService.LoginAsync(dto);

            if (result == null)
            {
                return Unauthorized(
                    "Tên đăng nhập hoặc mật khẩu không đúng.");
            }

            return Ok(result);
        }
    }
}