using EnglishCenter.API.DTOs;
using EnglishCenter.API.Services;
using EnglishCenter.API.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Authorization;

namespace EnglishCenter.API.Controllers
{
    [Asp.Versioning.ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Route("api/[controller]")]
    [ApiController]
    [EnableRateLimiting("auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;

        public AuthController(
            IAuthService authService,
            IWebHostEnvironment environment,
            IConfiguration configuration)
        {
            _authService = authService;
            _environment = environment;
            _configuration = configuration;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(
            RegisterDto dto)
        {
            var result =
                await _authService.RegisterAsync(dto);

            if (!result)
            {
                return BadRequest(new
                {
                    message = "Tên đăng nhập đã tồn tại. Vui lòng chọn tên khác."
                });
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

            return Ok(ToClientLoginResult(result));
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
                message = "Mã OTP đã được gửi đến email."

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
    [FromBody] SendLoginOtpDto dto)
        {
            await _authService.SendLoginOtpAsync(dto.UserName);

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

            if (result is null)
            {
                return Unauthorized(new { message = "Không thể xác thực đăng nhập. Vui lòng thử lại." });
            }

            if (result.Auth is not null)
            {
                SetAuthCookies(result.Auth);
            }

            return Ok(ToClientLoginResult(result));
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh(RefreshTokenRequestDto? dto)
        {
            var refreshToken = Request.Cookies[AuthCookies.RefreshToken]
                ?? dto?.RefreshToken;
            var result = await _authService.RefreshAsync(refreshToken ?? string.Empty);
            if (result is null)
            {
                ClearAuthCookies();
                return Unauthorized(new { message = "Refresh token không hợp lệ hoặc đã hết hạn." });
            }

            SetAuthCookies(result);
            return Ok(ToClientAuth(result));
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout(RefreshTokenRequestDto? dto)
        {
            var refreshToken = Request.Cookies[AuthCookies.RefreshToken]
                ?? dto?.RefreshToken;
            await _authService.RevokeRefreshTokenAsync(refreshToken ?? string.Empty);
            ClearAuthCookies();
            return Ok(new { message = "Đăng xuất thành công." });
        }

        private object ToClientLoginResult(LoginResultDto result)
        {
            return new
            {
                result.RequiresTwoFactor,
                result.UserName,
                Auth = result.Auth is null ? null : ToClientAuth(result.Auth)
            };
        }

        private static object ToClientAuth(AuthResponseDto auth)
        {
            return new
            {
                auth.Id,
                auth.UserName,
                auth.Role
            };
        }

        private void SetAuthCookies(AuthResponseDto auth)
        {
            var accessMinutes = _configuration.GetValue<int?>("Jwt:ExpireMinutes") ?? 60;
            var refreshDays = _configuration.GetValue<int?>("Jwt:RefreshTokenDays") ?? 30;

            Response.Cookies.Append(AuthCookies.AccessToken, auth.Token, CreateCookieOptions(
                DateTimeOffset.UtcNow.AddMinutes(accessMinutes)));
            Response.Cookies.Append(AuthCookies.RefreshToken, auth.RefreshToken, CreateCookieOptions(
                DateTimeOffset.UtcNow.AddDays(refreshDays)));
        }

        private void ClearAuthCookies()
        {
            var options = CreateCookieOptions(DateTimeOffset.UnixEpoch);
            Response.Cookies.Delete(AuthCookies.AccessToken, options);
            Response.Cookies.Delete(AuthCookies.RefreshToken, options);
        }

        private CookieOptions CreateCookieOptions(DateTimeOffset expires)
        {
            return new CookieOptions
            {
                HttpOnly = true,
                Secure = !_environment.IsDevelopment(),
                SameSite = SameSiteMode.Strict,
                IsEssential = true,
                Path = "/",
                Expires = expires
            };
        }
    }
}
