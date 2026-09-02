using EnglishCenter.API.DTOs;

namespace EnglishCenter.API.Services
{
    public interface IExamService
    {
        Task<List<ExamDto>> GetAllAsync();

        Task<ExamDto?> GetByIdAsync(int id);

        Task<ExamDto> CreateAsync(
            ExamCreateDto dto);

        Task<bool> UpdateAsync(
            int id,
            ExamUpdateDto dto);

        Task<bool> DeleteAsync(int id);
    }
}