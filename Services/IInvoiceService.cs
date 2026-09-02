using EnglishCenter.API.DTOs;

namespace EnglishCenter.API.Services
{
    public interface IInvoiceService
    {
        Task<List<InvoiceDto>> GetAllAsync();

        Task<InvoiceDto?> GetByIdAsync(int id);

        Task<InvoiceDto> CreateAsync(
            InvoiceCreateDto dto);

        Task<bool> UpdateAsync(
            int id,
            InvoiceUpdateDto dto);

        Task<bool> DeleteAsync(int id);
    }
}