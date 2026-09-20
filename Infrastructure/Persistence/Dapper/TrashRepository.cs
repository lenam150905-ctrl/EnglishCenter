using EnglishCenter.API.DTOs;
using EnglishCenter.Application.Abstractions.Persistence;

namespace EnglishCenter.Infrastructure.Persistence.Dapper;

public sealed class TrashRepository(IDbConnectionFactory connectionFactory)
    : DapperRepository(connectionFactory), ITrashRepository
{
    private static readonly IReadOnlyDictionary<string, string> Tables =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Users"] = "Users", ["Students"] = "Students", ["Teachers"] = "Teachers",
            ["Courses"] = "Courses", ["Schedules"] = "Schedules", ["Enrollments"] = "Enrollments",
            ["Exams"] = "Exams", ["Grades"] = "Grades", ["Invoices"] = "Invoices", ["Certificates"] = "Certificates"
        };

    public Task<IReadOnlyList<DeletedItemDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT 'Users' Entity, Id, UserName Label FROM Users WHERE IsDeleted = 1
            UNION ALL SELECT 'Students', Id, FullName FROM Students WHERE IsDeleted = 1
            UNION ALL SELECT 'Teachers', Id, FullName FROM Teachers WHERE IsDeleted = 1
            UNION ALL SELECT 'Courses', Id, CourseName FROM Courses WHERE IsDeleted = 1
            UNION ALL SELECT 'Schedules', Id, CONCAT('Lịch học #', Id, ' - ', Room) FROM Schedules WHERE IsDeleted = 1
            UNION ALL SELECT 'Enrollments', Id, CONCAT('Ghi danh #', Id) FROM Enrollments WHERE IsDeleted = 1
            UNION ALL SELECT 'Exams', Id, ExamName FROM Exams WHERE IsDeleted = 1
            UNION ALL SELECT 'Grades', Id, CONCAT('Điểm #', Id) FROM Grades WHERE IsDeleted = 1
            UNION ALL SELECT 'Invoices', Id, CONCAT('Hóa đơn #', Id) FROM Invoices WHERE IsDeleted = 1
            UNION ALL SELECT 'Certificates', Id, CertificateCode FROM Certificates WHERE IsDeleted = 1
            ORDER BY Entity, Id DESC;
            """;

        return QueryAsync<DeletedItemDto>(sql, null, cancellationToken);
    }

    public Task<bool> RestoreAsync(string entity, int id, CancellationToken cancellationToken = default)
    {
        if (!Tables.TryGetValue(entity, out var table))
        {
            throw new ArgumentException("Loại dữ liệu không hợp lệ.", nameof(entity));
        }

        return RestoreInternalAsync(table, id, cancellationToken);
    }

    private async Task<bool> RestoreInternalAsync(string table, int id, CancellationToken cancellationToken) =>
        await ExecuteAsync($"UPDATE [{table}] SET IsDeleted = 0 WHERE Id = @Id AND IsDeleted = 1;", new { Id = id }, cancellationToken) > 0;
}
