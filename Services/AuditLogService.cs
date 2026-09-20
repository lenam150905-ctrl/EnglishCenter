using EnglishCenter.API.DTOs;
using EnglishCenter.API.Models;
using EnglishCenter.Application.Abstractions.Persistence;

namespace EnglishCenter.API.Services
{
    public class AuditLogService : IAuditLogService
    {
        private readonly IAuditLogRepository _auditLogRepository;

        public AuditLogService(IAuditLogRepository auditLogRepository)
        {
            _auditLogRepository = auditLogRepository;
        }

        public async Task<AuditLogDto> CreateAsync(
            int? userId,
            string action,
            string entityName,
            int? entityId,
            string description,
            string? ipAddress)
        {
            var auditLog = new AuditLog
            {
                UserId = userId,
                Action = action,
                EntityName = entityName,
                EntityId = entityId,
                Description = description,
                CreatedAt = DateTime.Now,
                IpAddress = ipAddress
            };

            await _auditLogRepository.CreateAsync(auditLog);

            return new AuditLogDto
            {
                Id = auditLog.Id,
                UserId = auditLog.UserId,
                Action = auditLog.Action,
                EntityName = auditLog.EntityName,
                EntityId = auditLog.EntityId,
                Description = auditLog.Description,
                CreatedAt = auditLog.CreatedAt,
                IpAddress = auditLog.IpAddress
            };
        }

        public async Task<List<AuditLogDto>> GetAllAsync()
        {
            var logs = await _auditLogRepository.GetAllAsync();

            return logs.Select(x => new AuditLogDto
            {
                Id = x.Id,
                UserId = x.UserId,
                Action = x.Action,
                EntityName = x.EntityName,
                EntityId = x.EntityId,
                Description = x.Description,
                CreatedAt = x.CreatedAt,
                IpAddress = x.IpAddress
            }).ToList();
        }
    }
}