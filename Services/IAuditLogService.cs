using EnglishCenter.API.DTOs;

namespace EnglishCenter.API.Services
{
    public interface IAuditLogService
    {
        Task<AuditLogDto> CreateAsync(
            int? userId,
            string action,
            string entityName,
            int? entityId,
            string description,
            string? ipAddress);

        Task<List<AuditLogDto>> GetAllAsync();
    }
}