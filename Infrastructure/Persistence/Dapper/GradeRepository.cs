using System.Text;
using Dapper;
using EnglishCenter.API.Models;
using EnglishCenter.Application.Abstractions.Persistence;

namespace EnglishCenter.Infrastructure.Persistence.Dapper;

public sealed class GradeRepository(IDbConnectionFactory connectionFactory)
    : DapperRepository(connectionFactory), IGradeRepository
{
    public async Task<Grade?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT g.Id, g.ExamId, g.StudentId, g.Score, g.Comment, g.IsDeleted,
                   e.Id, e.ExamName, e.ExamType, e.ExamDate, e.CourseId, e.IsDeleted,
                   s.Id, s.FullName, s.DateOfBirth, s.Email, s.Phone, s.Address, s.UserId, s.IsDeleted
            FROM Grades g
            LEFT JOIN Exams e ON g.ExamId = e.Id
            LEFT JOIN Students s ON g.StudentId = s.Id
            WHERE g.Id = @Id AND g.IsDeleted = 0;
            """;

        var list = await QueryAsync<Grade, Exam, Student, Grade>(
            sql,
            (grade, exam, student) =>
            {
                grade.Exam = exam;
                grade.Student = student;
                return grade;
            },
            new { Id = id },
            splitOn: "Id,Id",
            cancellationToken: cancellationToken);

        return list.FirstOrDefault();
    }

    public async Task<Grade?> GetByStudentAndCourseAsync(int studentId, int courseId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT TOP 1 g.Id, g.ExamId, g.StudentId, g.Score, g.Comment, g.IsDeleted,
                   e.Id, e.ExamName, e.ExamType, e.ExamDate, e.CourseId, e.IsDeleted
            FROM Grades g
            INNER JOIN Exams e ON g.ExamId = e.Id
            WHERE g.StudentId = @StudentId AND e.CourseId = @CourseId AND g.IsDeleted = 0 AND e.IsDeleted = 0;
            """;

        var list = await QueryAsync<Grade, Exam, Grade>(
            sql,
            (grade, exam) =>
            {
                grade.Exam = exam;
                return grade;
            },
            new { StudentId = studentId, CourseId = courseId },
            splitOn: "Id",
            cancellationToken: cancellationToken);

        return list.FirstOrDefault();
    }

    public async Task<bool> ExistsAsync(int examId, int studentId, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        var sql = excludeId.HasValue
            ? "SELECT COUNT(1) FROM Grades WHERE ExamId = @ExamId AND StudentId = @StudentId AND Id <> @ExcludeId AND IsDeleted = 0;"
            : "SELECT COUNT(1) FROM Grades WHERE ExamId = @ExamId AND StudentId = @StudentId AND IsDeleted = 0;";

        var count = await ExecuteScalarAsync<int>(sql, new { ExamId = examId, StudentId = studentId, ExcludeId = excludeId }, cancellationToken);
        return count > 0;
    }

    public async Task<(IReadOnlyList<Grade> Grades, int TotalCount)> GetAllAsync(
        string? search,
        int? examId,
        int? studentId,
        decimal? minScore,
        decimal? maxScore,
        string? sortBy,
        bool sortDesc,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var whereClause = new StringBuilder("WHERE g.IsDeleted = 0");
        var parameters = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(search))
        {
            whereClause.Append(" AND (s.FullName LIKE @Search OR e.ExamName LIKE @Search)");
            parameters.Add("Search", $"%{search}%");
        }

        if (examId.HasValue)
        {
            whereClause.Append(" AND g.ExamId = @ExamId");
            parameters.Add("ExamId", examId.Value);
        }

        if (studentId.HasValue)
        {
            whereClause.Append(" AND g.StudentId = @StudentId");
            parameters.Add("StudentId", studentId.Value);
        }

        if (minScore.HasValue)
        {
            whereClause.Append(" AND g.Score >= @MinScore");
            parameters.Add("MinScore", minScore.Value);
        }

        if (maxScore.HasValue)
        {
            whereClause.Append(" AND g.Score <= @MaxScore");
            parameters.Add("MaxScore", maxScore.Value);
        }

        var orderColumn = (sortBy?.ToLower()) switch
        {
            "score" => "g.Score",
            _ => "g.Id"
        };
        var orderDirection = sortDesc ? "DESC" : "ASC";

        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 20 : pageSize;
        var offset = (page - 1) * pageSize;

        parameters.Add("Offset", offset);
        parameters.Add("PageSize", pageSize);

        var countSql = $"""
            SELECT COUNT(1)
            FROM Grades g
            LEFT JOIN Exams e ON g.ExamId = e.Id
            LEFT JOIN Students s ON g.StudentId = s.Id
            {whereClause};
            """;

        var querySql = $"""
            SELECT g.Id, g.ExamId, g.StudentId, g.Score, g.Comment, g.IsDeleted,
                   e.Id, e.ExamName, e.ExamType, e.ExamDate, e.CourseId, e.IsDeleted,
                   s.Id, s.FullName, s.DateOfBirth, s.Email, s.Phone, s.Address, s.UserId, s.IsDeleted
            FROM Grades g
            LEFT JOIN Exams e ON g.ExamId = e.Id
            LEFT JOIN Students s ON g.StudentId = s.Id
            {whereClause}
            ORDER BY {orderColumn} {orderDirection}
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
            """;

        using var connection = CreateConnection();
        var totalCount = await connection.ExecuteScalarAsync<int>(new CommandDefinition(countSql, parameters, cancellationToken: cancellationToken));
        var grades = (await connection.QueryAsync<Grade, Exam, Student, Grade>(
            new CommandDefinition(querySql, parameters, cancellationToken: cancellationToken),
            (grade, exam, student) =>
            {
                grade.Exam = exam;
                grade.Student = student;
                return grade;
            },
            splitOn: "Id,Id")).AsList();

        return (grades, totalCount);
    }

    public async Task CreateAsync(Grade grade, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO Grades (ExamId, StudentId, Score, Comment, IsDeleted)
            OUTPUT INSERTED.Id
            VALUES (@ExamId, @StudentId, @Score, @Comment, @IsDeleted);
            """;
        grade.Id = await ExecuteScalarAsync<int>(sql, grade, cancellationToken);
    }

    public async Task<bool> UpdateAsync(Grade grade, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE Grades
            SET ExamId = @ExamId,
                StudentId = @StudentId,
                Score = @Score,
                Comment = @Comment
            WHERE Id = @Id AND IsDeleted = 0;
            """;
        return await ExecuteAsync(sql, grade, cancellationToken) > 0;
    }

    public async Task<bool> SoftDeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = "UPDATE Grades SET IsDeleted = 1 WHERE Id = @Id AND IsDeleted = 0;";
        return await ExecuteAsync(sql, new { Id = id }, cancellationToken) > 0;
    }
}
