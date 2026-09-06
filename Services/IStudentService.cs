using EnglishCenter.API.DTOs;
namespace EnglishCenter.API.Services
{
    public interface IStudentService
    {
        Task<PagedResultDto<StudentDto>> GetAllAsync(
    string? search,
    DateTime? fromDateOfBirth,
    DateTime? toDateOfBirth,
    string? sortBy,
    bool sortDesc,
    int page,
    int pageSize);

        Task<StudentDto?> GetByIdAsync(int id);

        Task<StudentDto> CreateAsync(StudentCreateDto dto);

        Task<bool> UpdateAsync(
            int id,
            StudentUpdateDto dto);

        Task<bool> DeleteAsync(int id);
        Task<ExcelImportResultDto> ImportExcelAsync(IFormFile file);
    }
}