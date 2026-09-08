using EnglishCenter.API.DTOs;

namespace EnglishCenter.API.Services
{
    public interface IUserService
    {
        Task<PagedResultDto<UserDto>> GetAllAsync(
            string? search,
            string? role,
            string? sortBy,
            bool sortDesc,
            int page,
            int pageSize);

        Task<UserDto?> GetByIdAsync(int id);

        Task<UserDto> CreateAsync(UserCreateDto dto);

        Task<bool> UpdateAsync(
            int id,
            UserUpdateDto dto);

        Task<bool> DeleteAsync(int id);
    }
}