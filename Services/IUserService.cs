using EnglishCenter.API.DTOs;

namespace EnglishCenter.API.Services
{
    public interface IUserService
    {
        Task<List<UserDto>> GetAllAsync();

        Task<UserDto?> GetByIdAsync(int id);

        Task<UserDto> CreateAsync(UserCreateDto dto);

        Task<bool> UpdateAsync(int id, UserUpdateDto dto);

        Task<bool> DeleteAsync(int id);
    }
}