using EnglishCenter.API.Data;
using EnglishCenter.API.DTOs;
using EnglishCenter.API.Middleware;
using EnglishCenter.API.Models;
using Microsoft.EntityFrameworkCore;

namespace EnglishCenter.API.Services
{
    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public NotificationService(
            ApplicationDbContext context,
            IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        // LẤY USER ID TỪ AUDIT CONTEXT
        private int? userid =>
AuditContext.GetUserId(
_httpContextAccessor.HttpContext!);

        // =========================
        // CREATE
        // =========================
        public async Task<NotificationDto> CreateAsync(
            NotificationCreateDto dto)
        {
           

            if (!userid.HasValue)
            {
                throw new UnauthorizedAccessException(
                    "Không xác định được UserId.");
            }

            if (string.IsNullOrWhiteSpace(dto.Title))
            {
                throw new ArgumentException(
                    "Tiêu đề thông báo không được để trống.");
            }

            if (string.IsNullOrWhiteSpace(dto.Message))
            {
                throw new ArgumentException(
                    "Nội dung thông báo không được để trống.");
            }

            var notification = new Notification
            {
                UserId = dto.UserId,
                Title = dto.Title,
                Message = dto.Message,
                Type = dto.Type,
                IsRead = false,
                CreatedAt = DateTime.Now
            };

            _context.Notifications.Add(notification);

            await _context.SaveChangesAsync();

            return new NotificationDto
            {
                Id = notification.Id,
                UserId = notification.UserId,
                Title = notification.Title,
                Message = notification.Message,
                Type = notification.Type,
                IsRead = notification.IsRead,
                CreatedAt = notification.CreatedAt
            };
        }

        // =========================
        // GET NOTIFICATION
        // =========================
        public async Task<List<NotificationDto>>
            GetByUserIdAsync(int userId)
        {
            return await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new NotificationDto
                {
                    Id = n.Id,
                    UserId = n.UserId,
                    Title = n.Title,
                    Message = n.Message,
                    Type = n.Type,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt
                })
                .ToListAsync();
        }

        // =========================
        // READ ONE
        // =========================
        public async Task<bool> MarkAsReadAsync(
            int id,
            int userId)
        {
            var notification =
                await _context.Notifications
                    .FirstOrDefaultAsync(n =>
                        n.Id == id &&
                        n.UserId == userId);

            if (notification == null)
            {
                return false;
            }

            notification.IsRead = true;

            await _context.SaveChangesAsync();

            return true;
        }

        // =========================
        // READ ALL
        // =========================
        public async Task<bool> MarkAllAsReadAsync(
            int userId)
        {
            var notifications =
                await _context.Notifications
                    .Where(n =>
                        n.UserId == userId &&
                        !n.IsRead)
                    .ToListAsync();

            if (!notifications.Any())
            {
                return false;
            }

            foreach (var notification in notifications)
            {
                notification.IsRead = true;
            }

            await _context.SaveChangesAsync();

            return true;
        }

        // =========================
        // DELETE
        // =========================
        public async Task<bool> DeleteAsync(
            int id,
            int userId)
        {
            var notification =
                await _context.Notifications
                    .FirstOrDefaultAsync(n =>
                        n.Id == id &&
                        n.UserId == userId);

            if (notification == null)
            {
                return false;
            }

            _context.Notifications.Remove(notification);

            await _context.SaveChangesAsync();

            return true;
        }
    }
}