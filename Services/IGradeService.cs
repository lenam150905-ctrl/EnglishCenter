using EnglishCenter.API.DTOs;

namespace EnglishCenter.API.Services
{
    public interface IGradeService
    {
        Task<List<GradeDto>> GetAllAsync();

        Task<GradeDto?> GetByIdAsync(int id);

        Task<GradeDto> CreateAsync(
            GradeCreateDto dto);

        Task<bool> UpdateAsync(
            int id,
            GradeUpdateDto dto);

        Task<bool> DeleteAsync(int id);
    }
}