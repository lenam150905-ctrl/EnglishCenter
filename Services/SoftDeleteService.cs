using EnglishCenter.API.Data;
using EnglishCenter.API.DTOs;
using EnglishCenter.API.Middleware;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace EnglishCenter.API.Services
{
    public class SoftDeleteService : ISoftDeleteService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditLogService _auditLogService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly INotificationService _notificationService;
        public SoftDeleteService(
      ApplicationDbContext context,
      IAuditLogService auditLogService,
      IHttpContextAccessor httpContextAccessor,
      INotificationService notificationService)
        {
            _context = context;
            _auditLogService = auditLogService;
            _httpContextAccessor = httpContextAccessor;
            _notificationService = notificationService;
        }

        public async Task<bool> RestoreAsync<TEntity>(int id)
    where TEntity : class
        {
            var entity = await _context.Set<TEntity>()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(e =>
                    EF.Property<int>(e, "Id") == id);

            if (entity == null)
                return false;

            _context.Entry(entity)
                .Property("IsDeleted")
                .CurrentValue = false;

            await _context.SaveChangesAsync();
            var httpContext =
        _httpContextAccessor.HttpContext;

            var userId =
                httpContext != null
                    ? AuditContext.GetUserId(httpContext)
                    : null;

            var ipAddress =
                httpContext != null
                    ? AuditContext.GetIPAddress(httpContext)
                    : null;


            // =========================
            // AUDIT LOG
            // =========================

            await _auditLogService.CreateAsync(
                userId,
                "RESTORE",
                typeof(TEntity).Name,
                id,
                $"Khôi phục {typeof(TEntity).Name} có Id = {id}",
                ipAddress);
            // =========================
            // NOTIFICATION
            // =========================

            if (userId.HasValue)
            {
                await _notificationService.CreateAsync(
                    new NotificationCreateDto
                    {
                        UserId = userId.Value,
                        Title = "Khôi phục dữ liệu",
                        Message =
                            $"Bạn đã khôi phục {typeof(TEntity).Name} có Id = {id}.",
                        Type = "RESTORE"
                    });
            }
            return true;
        }
    }
}