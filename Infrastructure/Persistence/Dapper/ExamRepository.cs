using System.Text;
using Dapper;
using EnglishCenter.API.Models;
using EnglishCenter.Application.Abstractions.Persistence;

namespace EnglishCenter.Infrastructure.Persistence.Dapper;

public sealed class ExamRepository(IDbConnectionFactory connectionFactory)
    : DapperRepository(connectionFactory), IExamRepository
{
    public async Task<Exam?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT e.Id, e.ExamName, e.ExamType, e.ExamDate, e.CourseId, e.IsDeleted,
                   c.Id, c.CourseCode, c.CourseName, c.Description, c.Duration, c.TuitionFee, c.Status, c.IsDeleted
            FROM Exams e
            LEFT JOIN Courses c ON e.CourseId = c.Id
            WHERE e.Id = @Id AND e.IsDeleted = 0;
            """;

        var list = await QueryAsync<Exam, Course, Exam>(
            sql,
            (exam, course) =>
            {
                exam.Course = course;
                return exam;
            },
            new { Id = id },
            splitOn: "Id",
            cancellationToken: cancellationToken);

        return list.FirstOrDefault();
    }

    public async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
    {
        var count = await ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM Exams WHERE Id = @Id AND IsDeleted = 0;",
            new { Id = id },
            cancellationToken);
        return count > 0;
    }

    public async Task<(IReadOnlyList<Exam> Exams, int TotalCount)> GetAllAsync(
        string? search,
        int? courseId,
        string? examType,
        string? sortBy,
        bool sortDesc,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var whereClause = new StringBuilder("WHERE e.IsDeleted = 0");
        var parameters = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(search))
        {
            whereClause.Append(" AND (e.ExamName LIKE @Search OR e.ExamType LIKE @Search OR c.CourseName LIKE @Search)");
            parameters.Add("Search", $"%{search}%");
        }

        if (courseId.HasValue)
        {
            whereClause.Append(" AND e.CourseId = @CourseId");
            parameters.Add("CourseId", courseId.Value);
        }

        if (!string.IsNullOrWhiteSpace(examType))
        {
            whereClause.Append(" AND e.ExamType = @ExamType");
            parameters.Add("ExamType", examType);
        }

        var orderColumn = (sortBy?.ToLower()) switch
        {
            "examname" => "e.ExamName",
            "examtype" => "e.ExamType",
            "examdate" => "e.ExamDate",
            _ => "e.Id"
        };
        var orderDirection = sortDesc ? "DESC" : "ASC";

        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 20 : pageSize;
        var offset = (page - 1) * pageSize;

        parameters.Add("Offset", offset);
        parameters.Add("PageSize", pageSize);

        var countSql = $"""
            SELECT COUNT(1)
            FROM Exams e
            LEFT JOIN Courses c ON e.CourseId = c.Id
            {whereClause};
            """;

        var querySql = $"""
            SELECT e.Id, e.ExamName, e.ExamType, e.ExamDate, e.CourseId, e.IsDeleted,
                   c.Id, c.CourseCode, c.CourseName, c.Description, c.Duration, c.TuitionFee, c.Status, c.IsDeleted
            FROM Exams e
            LEFT JOIN Courses c ON e.CourseId = c.Id
            {whereClause}
            ORDER BY {orderColumn} {orderDirection}
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
            """;

        using var connection = CreateConnection();
        var totalCount = await connection.ExecuteScalarAsync<int>(new CommandDefinition(countSql, parameters, cancellationToken: cancellationToken));
        var exams = (await connection.QueryAsync<Exam, Course, Exam>(
            new CommandDefinition(querySql, parameters, cancellationToken: cancellationToken),
            (exam, course) =>
            {
                exam.Course = course;
                return exam;
            },
            splitOn: "Id")).AsList();

        return (exams, totalCount);
    }

    public async Task CreateAsync(Exam exam, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO Exams (ExamName, ExamType, ExamDate, CourseId, IsDeleted)
            OUTPUT INSERTED.Id
            VALUES (@ExamName, @ExamType, @ExamDate, @CourseId, @IsDeleted);
            """;
        exam.Id = await ExecuteScalarAsync<int>(sql, exam, cancellationToken);
    }

    public async Task<bool> UpdateAsync(Exam exam, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE Exams
            SET ExamName = @ExamName,
                ExamType = @ExamType,
                ExamDate = @ExamDate,
                CourseId = @CourseId
            WHERE Id = @Id AND IsDeleted = 0;
            """;
        return await ExecuteAsync(sql, exam, cancellationToken) > 0;
    }

    public async Task<bool> SoftDeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = "UPDATE Exams SET IsDeleted = 1 WHERE Id = @Id AND IsDeleted = 0;";
        return await ExecuteAsync(sql, new { Id = id }, cancellationToken) > 0;
    }
}
