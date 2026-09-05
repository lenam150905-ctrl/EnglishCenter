using EnglishCenter.API.DTOs;

namespace EnglishCenter.API.Services
{
    public interface IEnrollmentService
    {
        Task<PagedResultDto<EnrollmentDto>> GetAllAsync(
      string? search,
      int? studentId,
      int? courseId,
      string? status,
      string? sortBy,
      bool sortDesc,
      int page,
      int pageSize);

        Task<EnrollmentDto?> GetByIdAsync(int id);

        Task<EnrollmentDto> CreateAsync(
            EnrollmentCreateDto dto);

        Task<bool> UpdateAsync(
            int id,
            EnrollmentUpdateDto dto);

        Task<bool> DeleteAsync(int id);
    }
}