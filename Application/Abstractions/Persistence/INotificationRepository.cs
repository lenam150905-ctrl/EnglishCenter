using EnglishCenter.API.Models;

namespace EnglishCenter.Application.Abstractions.Persistence;

public interface INotificationRepository
{
    Task CreateAsync(Notification notification, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Notification>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);
    Task<bool> MarkAsReadAsync(int id, int userId, CancellationToken cancellationToken = default);
    Task<bool> MarkAllAsReadAsync(int userId, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, int userId, CancellationToken cancellationToken = default);
}
