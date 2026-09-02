using EnglishCenter.API.DTOs;

namespace EnglishCenter.API.Services
{
    public interface ICertificateService
    {
        Task<List<CertificateDto>> GetAllAsync();

        Task<CertificateDto?> GetByIdAsync(int id);

        Task<CertificateDto> CreateAsync(
            CertificateCreateDto dto);

        Task<bool> UpdateAsync(
            int id,
            CertificateUpdateDto dto);

        Task<bool> DeleteAsync(int id);
    }
}