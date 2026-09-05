using EnglishCenter.API.DTOs;

namespace EnglishCenter.API.Services
{
    public interface IGradeService
    {
        Task<PagedResultDto<GradeDto>> GetAllAsync(
     string? search,
     int? examId,
     int? studentId,
     decimal? minScore,
     decimal? maxScore,
     string? sortBy,
     bool sortDesc,
     int page,
     int pageSize);

        Task<GradeDto?> GetByIdAsync(int id);

        Task<GradeDto> CreateAsync(
            GradeCreateDto dto);

        Task<bool> UpdateAsync(
            int id,
            GradeUpdateDto dto);

        Task<bool> DeleteAsync(int id);
    }
}