using EnglishCenter.API.DTOs;

namespace EnglishCenter.API.Services
{
    public interface IExamService
    {
        Task<PagedResultDto<ExamDto>> GetAllAsync(
        string? search,
        int? courseId,
        string? examType,
        string? sortBy,
        bool sortDesc,
        int page,
        int pageSize);

        Task<ExamDto?> GetByIdAsync(int id);

        Task<ExamDto> CreateAsync(
            ExamCreateDto dto);

        Task<bool> UpdateAsync(
            int id,
            ExamUpdateDto dto);

        Task<bool> DeleteAsync(int id);
        Task<bool> CanStartExamAsync(
    int studentId,
    int examId);
    }
}