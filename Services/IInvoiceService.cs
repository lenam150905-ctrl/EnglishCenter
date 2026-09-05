using EnglishCenter.API.DTOs;

namespace EnglishCenter.API.Services
{
    public interface IInvoiceService
    {
        Task<PagedResultDto<InvoiceDto>> GetAllAsync(
    string? search,
    int? studentId,
    int? enrollmentId,
    string? status,
    decimal? minAmount,
    decimal? maxAmount,
    string? sortBy,
    bool sortDesc,
    int page,
    int pageSize);

        Task<InvoiceDto?> GetByIdAsync(int id);

        Task<InvoiceDto> CreateAsync(
            InvoiceCreateDto dto);

        Task<bool> UpdateAsync(
            int id,
            InvoiceUpdateDto dto);

        Task<bool> DeleteAsync(int id);
    }
}