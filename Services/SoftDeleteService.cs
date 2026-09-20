using EnglishCenter.API.DTOs;
using EnglishCenter.API.Middleware;
using EnglishCenter.Application.Abstractions.Persistence;

namespace EnglishCenter.API.Services
{
    public class SoftDeleteService : ISoftDeleteService
    {
        private readonly ISoftDeleteRepository _softDeleteRepository;
        private readonly IAuditLogService _auditLogService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly INotificationService _notificationService;

        public SoftDeleteService(
            ISoftDeleteRepository softDeleteRepository,
            IAuditLogService auditLogService,
            IHttpContextAccessor httpContextAccessor,
            INotificationService notificationService)
        {
            _softDeleteRepository = softDeleteRepository;
            _auditLogService = auditLogService;
            _httpContextAccessor = httpContextAccessor;
            _notificationService = notificationService;
        }

        public async Task<bool> RestoreAsync<TEntity>(int id)
            where TEntity : class
        {
            var entityName = typeof(TEntity).Name;
            var tableName = entityName.EndsWith("s") ? entityName : entityName + "s";

            var restored = await _softDeleteRepository.RestoreAsync(tableName, id);
            if (!restored)
            {
                return false;
            }

            var httpContext = _httpContextAccessor.HttpContext;
            var userId = httpContext != null ? AuditContext.GetUserId(httpContext) : null;
            var ipAddress = httpContext != null ? AuditContext.GetIPAddress(httpContext) : null;

            await _auditLogService.CreateAsync(
                userId,
                "RESTORE",
                entityName,
                id,
                $"Khôi phục {entityName} có Id = {id}",
                ipAddress);

            if (userId.HasValue)
            {
                await _notificationService.CreateAsync(new NotificationCreateDto
                {
                    UserId = userId.Value,
                    Title = "Khôi phục dữ liệu",
                    Message = $"Bạn đã khôi phục {entityName} có Id = {id}.",
                    Type = "RESTORE"
                });
            }

            return true;
        }
    }
}