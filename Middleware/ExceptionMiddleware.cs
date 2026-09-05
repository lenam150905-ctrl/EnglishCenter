using System.Net;
using System.Text.Json;

namespace EnglishCenter.API.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;

        public ExceptionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (ArgumentException ex)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;

                context.Response.ContentType = "application/json";

                var response = new
                {
                    statusCode = 400,
                    message = ex.Message
                };

                await context.Response.WriteAsJsonAsync(response);
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;

                context.Response.ContentType = "application/json";

                var response = new
                {
                    statusCode = 500,
                    message = "Đã xảy ra lỗi trong hệ thống."
                };

                await context.Response.WriteAsJsonAsync(response);
            }
        }

      
    }
}