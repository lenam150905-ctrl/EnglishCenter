using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Net.Http;
using System.Net.Security;
using System.Security.Claims;

const string AccessTokenCookie = "ec_access";

var builder = WebApplication.CreateBuilder(args);

// Bypass SSL certificate validation for local development
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddHttpClient()
        .ConfigureHttpClientDefaults(http =>
        {
            http.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            });
        });
}

// Giữ cổng local HTTPS khi chạy F5; Docker sẽ cung cấp ASPNETCORE_URLS=http://+:8080.
if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ASPNETCORE_URLS")))
{
    builder.WebHost.UseUrls("https://localhost:7300;http://localhost:5300");
}

builder.Logging
    .ClearProviders()
    .AddConsole()
    .AddDebug();

builder.Configuration
    .SetBasePath(builder.Environment.ContentRootPath)
    .AddOcelot();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.SetIsOriginAllowed(origin =>
            {
                if (Uri.TryCreate(origin, UriKind.Absolute, out var uri))
                {
                    if (uri.Host is "localhost" or "127.0.0.1")
                    {
                        return true;
                    }
                }
                return origin is "http://20.89.171.41" or "https://englishcenter-namle.duckdns.org";
            })
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Gateway chưa được cấu hình Jwt:Key.");

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = "GatewayJwt";
        options.DefaultChallengeScheme = "GatewayJwt";
    })
    .AddJwtBearer("GatewayJwt", options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.Zero
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var cookieToken = context.Request.Cookies[AccessTokenCookie];
                if (!string.IsNullOrEmpty(cookieToken))
                {
                    context.Token = cookieToken;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddOcelot(builder.Configuration);

var app = builder.Build();

app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();

// Middleware kiểm tra phân quyền chặt chẽ ngay tại Gateway:
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value ?? string.Empty;

    var isAdminRoute = path.StartsWith("/gateway/v1/admin", StringComparison.OrdinalIgnoreCase) ||
                       path.StartsWith("/gateway/admin", StringComparison.OrdinalIgnoreCase);

    if (isAdminRoute)
    {
        var isAuthPath = path.Contains("/Auth/", StringComparison.OrdinalIgnoreCase);

        if (!isAuthPath)
        {
            if (context.User.Identity?.IsAuthenticated != true)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json; charset=utf-8";
                await context.Response.WriteAsync("{\"message\":\"Bạn cần đăng nhập để truy cập khu vực quản trị.\"}");
                return;
            }

            var role = context.User.FindFirst(ClaimTypes.Role)?.Value
                ?? context.User.FindFirst("role")?.Value;

            // Chỉ Admin và Giáo viên mới được vào cổng Admin
            if (role != "Admin" && role != "Teacher")
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json; charset=utf-8";
                await context.Response.WriteAsync("{\"message\":\"Học viên không có quyền truy cập khu vực quản trị (Admin).\"}");
                return;
            }

            // Chức năng đặc quyền riêng của Admin: Users, Trash, AuditLog, Report
            var isAdminExclusive = path.Contains("/Users", StringComparison.OrdinalIgnoreCase) ||
                                  path.Contains("/Trash", StringComparison.OrdinalIgnoreCase) ||
                                  path.Contains("/AuditLog", StringComparison.OrdinalIgnoreCase) ||
                                  path.Contains("/Report", StringComparison.OrdinalIgnoreCase);

            if (isAdminExclusive && role != "Admin")
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json; charset=utf-8";
                await context.Response.WriteAsync("{\"message\":\"Chức năng này chỉ dành cho Quản trị viên (Admin). Giáo viên không có quyền thao tác.\"}");
                return;
            }
        }
    }

    await next();
});

await app.UseOcelot();
await app.RunAsync();