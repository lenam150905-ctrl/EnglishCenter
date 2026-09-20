using EnglishCenter.API.Models;
using EnglishCenter.Application.Abstractions.Persistence;

namespace EnglishCenter.Infrastructure.Persistence.Dapper;

public sealed class NotificationRepository(IDbConnectionFactory connectionFactory)
    : DapperRepository(connectionFactory), INotificationRepository
{
    public async Task CreateAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO Notifications (UserId, Title, Message, Type, IsRead, CreatedAt)
            OUTPUT INSERTED.Id
            VALUES (@UserId, @Title, @Message, @Type, @IsRead, @CreatedAt);
            """;

        notification.Id = await ExecuteScalarAsync<int>(sql, notification, cancellationToken);
    }

    public Task<IReadOnlyList<Notification>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default) =>
        QueryAsync<Notification>("SELECT Id, UserId, Title, Message, Type, IsRead, CreatedAt FROM Notifications WHERE UserId = @UserId ORDER BY CreatedAt DESC;", new { UserId = userId }, cancellationToken);

    public async Task<bool> MarkAsReadAsync(int id, int userId, CancellationToken cancellationToken = default) =>
        await ExecuteAsync("UPDATE Notifications SET IsRead = 1 WHERE Id = @Id AND UserId = @UserId AND IsRead = 0;", new { Id = id, UserId = userId }, cancellationToken) > 0;

    public async Task<bool> MarkAllAsReadAsync(int userId, CancellationToken cancellationToken = default) =>
        await ExecuteAsync("UPDATE Notifications SET IsRead = 1 WHERE UserId = @UserId AND IsRead = 0;", new { UserId = userId }, cancellationToken) > 0;

    public async Task<bool> DeleteAsync(int id, int userId, CancellationToken cancellationToken = default) =>
        await ExecuteAsync("DELETE FROM Notifications WHERE Id = @Id AND UserId = @UserId;", new { Id = id, UserId = userId }, cancellationToken) > 0;
}
