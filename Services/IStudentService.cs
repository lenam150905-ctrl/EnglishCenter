using EnglishCenter.API.DTOs;

namespace EnglishCenter.API.Services
{
    public interface IStudentService
    {
        Task<List<StudentDto>> GetAllAsync();

        Task<StudentDto?> GetByIdAsync(int id);

        Task<StudentDto> CreateAsync(StudentCreateDto dto);

        Task<bool> UpdateAsync(
            int id,
            StudentUpdateDto dto);

        Task<bool> DeleteAsync(int id);
    }
}