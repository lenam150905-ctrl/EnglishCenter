using EnglishCenter.API.DTOs;

namespace EnglishCenter.API.Services
{
    public interface ITeacherService
    {
        Task<List<TeacherDto>> GetAllAsync();

        Task<TeacherDto?> GetByIdAsync(int id);

        Task<TeacherDto> CreateAsync(TeacherCreateDto dto);

        Task<bool> UpdateAsync(int id, TeacherUpdateDto dto);

        Task<bool> DeleteAsync(int id);
    }
}