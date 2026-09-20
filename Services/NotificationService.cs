using EnglishCenter.API.DTOs;
using EnglishCenter.API.Middleware;
using EnglishCenter.API.Models;
using EnglishCenter.Application.Abstractions.Persistence;

namespace EnglishCenter.API.Services
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public NotificationService(
            INotificationRepository notificationRepository,
            IHttpContextAccessor httpContextAccessor)
        {
            _notificationRepository = notificationRepository;
            _httpContextAccessor = httpContextAccessor;
        }

        private int? userid =>
            AuditContext.GetUserId(_httpContextAccessor.HttpContext!);

        public async Task<NotificationDto> CreateAsync(NotificationCreateDto dto)
        {
            if (!userid.HasValue)
            {
                throw new UnauthorizedAccessException("Không xác định được UserId.");
            }

            return await CreateNotificationAsync(dto);
        }

        public Task<NotificationDto> CreateForUserAsync(NotificationCreateDto dto)
        {
            return CreateNotificationAsync(dto);
        }

        private async Task<NotificationDto> CreateNotificationAsync(NotificationCreateDto dto)
        {
            if (dto.UserId <= 0)
            {
                throw new ArgumentException("UserId không hợp lệ.");
            }

            if (string.IsNullOrWhiteSpace(dto.Title))
            {
                throw new ArgumentException("Tiêu đề thông báo không được để trống.");
            }

            if (string.IsNullOrWhiteSpace(dto.Message))
            {
                throw new ArgumentException("Nội dung thông báo không được để trống.");
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

            await _notificationRepository.CreateAsync(notification);

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

        public async Task<List<NotificationDto>> GetByUserIdAsync(int userId)
        {
            var notifications = await _notificationRepository.GetByUserIdAsync(userId);

            return notifications.Select(n => new NotificationDto
            {
                Id = n.Id,
                UserId = n.UserId,
                Title = n.Title,
                Message = n.Message,
                Type = n.Type,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt
            }).ToList();
        }

        public Task<bool> MarkAsReadAsync(int id, int userId)
        {
            return _notificationRepository.MarkAsReadAsync(id, userId);
        }

        public Task<bool> MarkAllAsReadAsync(int userId)
        {
            return _notificationRepository.MarkAllAsReadAsync(userId);
        }

        public Task<bool> DeleteAsync(int id, int userId)
        {
            return _notificationRepository.DeleteAsync(id, userId);
        }
    }
}