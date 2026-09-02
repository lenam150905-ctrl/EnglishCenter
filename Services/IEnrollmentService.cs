using EnglishCenter.API.DTOs;

namespace EnglishCenter.API.Services
{
    public interface IEnrollmentService
    {
        Task<List<EnrollmentDto>> GetAllAsync();

        Task<EnrollmentDto?> GetByIdAsync(int id);

        Task<EnrollmentDto> CreateAsync(
            EnrollmentCreateDto dto);

        Task<bool> UpdateAsync(
            int id,
            EnrollmentUpdateDto dto);

        Task<bool> DeleteAsync(int id);
    }
}