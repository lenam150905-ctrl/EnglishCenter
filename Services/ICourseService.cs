using EnglishCenter.API.DTOs;

namespace EnglishCenter.API.Services
{
    public interface ICourseService
    {
        Task<List<CourseDto>> GetAllAsync();

        Task<CourseDto?> GetByIdAsync(int id);

        Task<CourseDto> CreateAsync(CourseCreateDto dto);

        Task<bool> UpdateAsync(int id, CourseUpdateDto dto);

        Task<bool> DeleteAsync(int id);
    }
}