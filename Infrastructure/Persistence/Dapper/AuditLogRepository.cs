using EnglishCenter.API.Models;
using EnglishCenter.Application.Abstractions.Persistence;

namespace EnglishCenter.Infrastructure.Persistence.Dapper;

public sealed class AuditLogRepository(IDbConnectionFactory connectionFactory)
    : DapperRepository(connectionFactory), IAuditLogRepository
{
    public async Task CreateAsync(AuditLog auditLog, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO AuditLogs (UserId, Action, EntityName, EntityId, Description, CreatedAt, IpAddress)
            OUTPUT INSERTED.Id
            VALUES (@UserId, @Action, @EntityName, @EntityId, @Description, @CreatedAt, @IpAddress);
            """;

        auditLog.Id = await ExecuteScalarAsync<int>(sql, auditLog, cancellationToken);
    }

    public Task<IReadOnlyList<AuditLog>> GetAllAsync(CancellationToken cancellationToken = default) =>
        QueryAsync<AuditLog>("SELECT Id, UserId, Action, EntityName, EntityId, Description, CreatedAt, IpAddress FROM AuditLogs ORDER BY CreatedAt DESC;", null, cancellationToken);
}
