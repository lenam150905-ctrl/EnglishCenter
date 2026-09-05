using EnglishCenter.API.DTOs;

namespace EnglishCenter.API.Services
{
    public interface ICertificateService
    {
        Task<PagedResultDto<CertificateDto>> GetAllAsync(
    string? search,
    int? studentId,
    int? courseId,
    string? sortBy,
    bool sortDesc,
    int page,
    int pageSize);

        Task<CertificateDto?> GetByIdAsync(int id);

        Task<CertificateDto> CreateAsync(
            CertificateCreateDto dto);

        Task<bool> UpdateAsync(
            int id,
            CertificateUpdateDto dto);

        Task<bool> DeleteAsync(int id);
    }
}