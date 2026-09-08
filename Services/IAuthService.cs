using EnglishCenter.API.DTOs;

namespace EnglishCenter.API.Services
{
    public interface IAuthService
    {
        Task<bool> RegisterAsync(RegisterDto dto);

        Task<LoginResultDto?> LoginAsync(LoginDto dto);

        Task<bool> ResetPasswordAsync(ResetPasswordDto dto);

        Task<string> ForgotPasswordAsync(ForgotPasswordDto dto);

        Task<bool> VerifyOtpAsync(VerifyOtpDto dto);

        Task<bool> SendLoginOtpAsync(string userName);

        Task<LoginResultDto?> VerifyLoginOtpAsync(
            VerifyLoginOtpDto dto);
    }
}