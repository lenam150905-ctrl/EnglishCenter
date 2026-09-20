using EnglishCenter.Application.Abstractions.Persistence;

namespace EnglishCenter.Infrastructure.Persistence.Dapper;

public sealed class SoftDeleteRepository(IDbConnectionFactory connectionFactory)
    : DapperRepository(connectionFactory), ISoftDeleteRepository
{
    private static readonly HashSet<string> AllowedTables = new(StringComparer.OrdinalIgnoreCase)
    {
        "Courses",
        "Students",
        "Teachers",
        "Exams",
        "Schedules",
        "Users",
        "Enrollments",
        "Grades",
        "Invoices",
        "Certificates"
    };

    public async Task<bool> RestoreAsync(string tableName, int id, CancellationToken cancellationToken = default)
    {
        if (!AllowedTables.Contains(tableName))
        {
            throw new ArgumentException($"Bảng '{tableName}' không hỗ trợ khôi phục xóa mềm.");
        }

        var sql = $"UPDATE [{tableName}] SET IsDeleted = 0 WHERE Id = @Id;";
        return await ExecuteAsync(sql, new { Id = id }, cancellationToken) > 0;
    }
}
