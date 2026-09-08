using EnglishCenter.API.Data;
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

        public SoftDeleteService(
            ApplicationDbContext context,
            IAuditLogService auditLogService,
            IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _auditLogService = auditLogService;
            _httpContextAccessor = httpContextAccessor;
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

            return true;
        }
    }
}