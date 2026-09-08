using System.Security.Claims;

namespace EnglishCenter.API.Middleware
{
    public class AuditContextMiddleware
    {
        private readonly RequestDelegate _next;

        public AuditContextMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // UserId từ JWT
            var userIdText = context.User
                .FindFirstValue(ClaimTypes.NameIdentifier);

            int? userId = null;

            if (int.TryParse(userIdText, out var id))
            {
                userId = id;
            }

            // IP
            var ipAddress = context.Connection
                .RemoteIpAddress?
                .ToString();

            // Lưu vào HttpContext
            context.Items["AuditUserId"] = userId;
            context.Items["AuditIPAddress"] = ipAddress;

            await _next(context);
        }
    }
}