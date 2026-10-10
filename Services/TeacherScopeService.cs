using Dapper;
using EnglishCenter.Application.Abstractions.Persistence;

namespace EnglishCenter.API.Services;

public interface ITeacherScopeService
{
    Task<int?> GetTeacherIdAsync(int userId);
}

public sealed class TeacherScopeService(IDbConnectionFactory connectionFactory) : ITeacherScopeService
{
    public async Task<int?> GetTeacherIdAsync(int userId)
    {
        const string sql = "SELECT TOP 1 Id FROM Teachers WHERE UserId = @UserId AND IsDeleted = 0;";
        using var connection = connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<int?>(sql, new { UserId = userId });
    }
}
