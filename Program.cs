using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Cố định địa chỉ Gateway khi chạy bằng Start/Ctrl+F5 hoặc từ file .exe.
builder.WebHost.UseUrls("https://localhost:7300");

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
        policy.WithOrigins(
                "http://127.0.0.1:5500",
                "http://localhost:5500"
            )
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});


builder.Services.AddOcelot(builder.Configuration);

var app = builder.Build();

app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();

await app.UseOcelot();
await app.RunAsync();
