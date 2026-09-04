using EnglishCenter.API.DTOs;

namespace EnglishCenter.API.Services
{
    public interface IAuthService
    {
        Task<bool> RegisterAsync(RegisterDto dto);

        Task<AuthResponseDto?> LoginAsync(LoginDto dto);
    }
}