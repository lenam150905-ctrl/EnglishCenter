using EnglishCenter.API.Data;
using EnglishCenter.API.DTOs;
using EnglishCenter.API.Models;
using Microsoft.EntityFrameworkCore;

namespace EnglishCenter.API.Services
{
    public class AuditLogService : IAuditLogService
    {
        private readonly ApplicationDbContext _context;

        public AuditLogService(ApplicationDbContext context)
        {
            _context = context;
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

            _context.AuditLogs.Add(auditLog);

            await _context.SaveChangesAsync();

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
            return await _context.AuditLogs
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new AuditLogDto
                {
                    Id = x.Id,
                    UserId = x.UserId,
                    Action = x.Action,
                    EntityName = x.EntityName,
                    EntityId = x.EntityId,
                    Description = x.Description,
                    CreatedAt = x.CreatedAt,
                    IpAddress = x.IpAddress
                })
                .ToListAsync();
        }
    }
}