using EnglishCenter.API.DTOs;

namespace EnglishCenter.API.Services
{
    public interface ICourseService
    {
        Task<PagedResultDto<CourseDto>> GetAllAsync(
    string? search,
    decimal? minTuitionFee,
    decimal? maxTuitionFee,
    string? sortBy,
    bool sortDesc,
    int page,
    int pageSize);

        Task<CourseDto?> GetByIdAsync(int id);

        Task<CourseDto> CreateAsync(CourseCreateDto dto);

        Task<bool> UpdateAsync(int id, CourseUpdateDto dto);

        Task<bool> DeleteAsync(int id);
    }
}