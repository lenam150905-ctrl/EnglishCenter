using EnglishCenter.API.Models;

namespace EnglishCenter.Application.Abstractions.Persistence;

public interface IAuditLogRepository
{
    Task CreateAsync(AuditLog auditLog, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuditLog>> GetAllAsync(CancellationToken cancellationToken = default);
}
