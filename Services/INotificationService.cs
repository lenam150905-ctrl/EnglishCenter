using EnglishCenter.API.DTOs;

namespace EnglishCenter.API.Services
{
    public interface INotificationService
    {
        Task<NotificationDto> CreateAsync(
            NotificationCreateDto dto);

        Task<List<NotificationDto>> GetByUserIdAsync(
            int userId);

        Task<bool> MarkAsReadAsync(
            int id,
            int userId);

        Task<bool> MarkAllAsReadAsync(
            int userId);

        Task<bool> DeleteAsync(
            int id,
            int userId);
    }
}