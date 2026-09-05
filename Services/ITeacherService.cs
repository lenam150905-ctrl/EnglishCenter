using EnglishCenter.API.DTOs;

namespace EnglishCenter.API.Services
{
    public interface ITeacherService
    {
        Task<PagedResultDto<TeacherDto>> GetAllAsync(
     string? search,
     string? specialization,
     string? sortBy,
     bool sortDesc,
     int page,
     int pageSize);

        Task<TeacherDto?> GetByIdAsync(int id);

        Task<TeacherDto> CreateAsync(TeacherCreateDto dto);

        Task<bool> UpdateAsync(int id, TeacherUpdateDto dto);

        Task<bool> DeleteAsync(int id);
    }
}