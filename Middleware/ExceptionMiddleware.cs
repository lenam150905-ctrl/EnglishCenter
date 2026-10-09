using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace EnglishCenter.API.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid request at {Path}", context.Request.Path);
                context.Response.StatusCode = StatusCodes.Status400BadRequest;

                context.Response.ContentType = "application/json";

                var response = new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Dữ liệu không hợp lệ.",
                    Detail = ex.Message,
                    Instance = context.Request.Path
                };

                await context.Response.WriteAsJsonAsync(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception at {Path}", context.Request.Path);
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;

                context.Response.ContentType = "application/json";

                var response = new ProblemDetails
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Title = "Lỗi máy chủ.",
                    Detail = "Đã xảy ra lỗi trong hệ thống.",
                    Instance = context.Request.Path
                };

                await context.Response.WriteAsJsonAsync(response);
            }
        }

}
}

