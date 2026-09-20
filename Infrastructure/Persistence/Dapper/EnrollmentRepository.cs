using System.Text;
using Dapper;
using EnglishCenter.API.Models;
using EnglishCenter.Application.Abstractions.Persistence;

namespace EnglishCenter.Infrastructure.Persistence.Dapper;

public sealed class EnrollmentRepository(IDbConnectionFactory connectionFactory)
    : DapperRepository(connectionFactory), IEnrollmentRepository
{
    public async Task<Enrollment?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT e.Id, e.StudentId, e.CourseId, e.EnrollmentDate, e.Status, e.IsDeleted,
                   s.Id, s.FullName, s.DateOfBirth, s.Email, s.Phone, s.Address, s.UserId, s.IsDeleted,
                   c.Id, c.CourseCode, c.CourseName, c.Description, c.Duration, c.TuitionFee, c.Status, c.IsDeleted
            FROM Enrollments e
            LEFT JOIN Students s ON e.StudentId = s.Id
            LEFT JOIN Courses c ON e.CourseId = c.Id
            WHERE e.Id = @Id AND e.IsDeleted = 0;
            """;

        var list = await QueryAsync<Enrollment, Student, Course, Enrollment>(
            sql,
            (enrollment, student, course) =>
            {
                enrollment.Student = student;
                enrollment.Course = course;
                return enrollment;
            },
            new { Id = id },
            splitOn: "Id,Id",
            cancellationToken: cancellationToken);

        return list.FirstOrDefault();
    }

    public async Task<bool> ExistsAsync(int studentId, int courseId, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        var sql = excludeId.HasValue
            ? "SELECT COUNT(1) FROM Enrollments WHERE StudentId = @StudentId AND CourseId = @CourseId AND Id <> @ExcludeId AND IsDeleted = 0;"
            : "SELECT COUNT(1) FROM Enrollments WHERE StudentId = @StudentId AND CourseId = @CourseId AND IsDeleted = 0;";

        var count = await ExecuteScalarAsync<int>(sql, new { StudentId = studentId, CourseId = courseId, ExcludeId = excludeId }, cancellationToken);
        return count > 0;
    }

    public async Task<bool> HasActiveEnrollmentForCourseAsync(int courseId, CancellationToken cancellationToken = default)
    {
        var count = await ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM Enrollments WHERE CourseId = @CourseId AND Status = 'Active' AND IsDeleted = 0;",
            new { CourseId = courseId },
            cancellationToken);
        return count > 0;
    }

    public Task<int?> GetStudentUserIdByEnrollmentIdAsync(int enrollmentId, CancellationToken cancellationToken = default) =>
        ExecuteScalarAsync<int?>(
            """
            SELECT s.UserId
            FROM Enrollments e
            INNER JOIN Students s ON e.StudentId = s.Id
            WHERE e.Id = @EnrollmentId AND e.IsDeleted = 0;
            """,
            new { EnrollmentId = enrollmentId },
            cancellationToken);

    public async Task<(IReadOnlyList<Enrollment> Enrollments, int TotalCount)> GetAllAsync(
        string? search,
        int? studentId,
        int? courseId,
        string? status,
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
            whereClause.Append(" AND (s.FullName LIKE @Search OR s.Email LIKE @Search OR c.CourseName LIKE @Search)");
            parameters.Add("Search", $"%{search}%");
        }

        if (studentId.HasValue)
        {
            whereClause.Append(" AND e.StudentId = @StudentId");
            parameters.Add("StudentId", studentId.Value);
        }

        if (courseId.HasValue)
        {
            whereClause.Append(" AND e.CourseId = @CourseId");
            parameters.Add("CourseId", courseId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            whereClause.Append(" AND e.Status = @Status");
            parameters.Add("Status", status);
        }

        var orderColumn = (sortBy?.ToLower()) switch
        {
            "enrollmentdate" => "e.EnrollmentDate",
            "status" => "e.Status",
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
            FROM Enrollments e
            LEFT JOIN Students s ON e.StudentId = s.Id
            LEFT JOIN Courses c ON e.CourseId = c.Id
            {whereClause};
            """;

        var querySql = $"""
            SELECT e.Id, e.StudentId, e.CourseId, e.EnrollmentDate, e.Status, e.IsDeleted,
                   s.Id, s.FullName, s.DateOfBirth, s.Email, s.Phone, s.Address, s.UserId, s.IsDeleted,
                   c.Id, c.CourseCode, c.CourseName, c.Description, c.Duration, c.TuitionFee, c.Status, c.IsDeleted
            FROM Enrollments e
            LEFT JOIN Students s ON e.StudentId = s.Id
            LEFT JOIN Courses c ON e.CourseId = c.Id
            {whereClause}
            ORDER BY {orderColumn} {orderDirection}
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
            """;

        using var connection = CreateConnection();
        var totalCount = await connection.ExecuteScalarAsync<int>(new CommandDefinition(countSql, parameters, cancellationToken: cancellationToken));
        var enrollments = (await connection.QueryAsync<Enrollment, Student, Course, Enrollment>(
            new CommandDefinition(querySql, parameters, cancellationToken: cancellationToken),
            (enrollment, student, course) =>
            {
                enrollment.Student = student;
                enrollment.Course = course;
                return enrollment;
            },
            splitOn: "Id,Id")).AsList();

        return (enrollments, totalCount);
    }

    public async Task CreateAsync(Enrollment enrollment, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO Enrollments (StudentId, CourseId, EnrollmentDate, Status, IsDeleted)
            OUTPUT INSERTED.Id
            VALUES (@StudentId, @CourseId, @EnrollmentDate, @Status, @IsDeleted);
            """;
        enrollment.Id = await ExecuteScalarAsync<int>(sql, enrollment, cancellationToken);
    }

    public async Task<bool> UpdateAsync(Enrollment enrollment, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE Enrollments
            SET StudentId = @StudentId,
                CourseId = @CourseId,
                EnrollmentDate = @EnrollmentDate,
                Status = @Status
            WHERE Id = @Id AND IsDeleted = 0;
            """;
        return await ExecuteAsync(sql, enrollment, cancellationToken) > 0;
    }

    public async Task<bool> UpdateStatusAsync(int id, string status, CancellationToken cancellationToken = default)
    {
        const string sql = "UPDATE Enrollments SET Status = @Status WHERE Id = @Id AND IsDeleted = 0;";
        return await ExecuteAsync(sql, new { Id = id, Status = status }, cancellationToken) > 0;
    }

    public async Task<bool> SoftDeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = "UPDATE Enrollments SET IsDeleted = 1 WHERE Id = @Id AND IsDeleted = 0;";
        return await ExecuteAsync(sql, new { Id = id }, cancellationToken) > 0;
    }
    public async Task<Enrollment?> GetByStudentAndCourseAsync(
    int studentId,
    int courseId)
    {
        const string sql = """
        SELECT *
        FROM Enrollments
        WHERE StudentId = @StudentId
          AND CourseId = @CourseId
          AND IsDeleted = 0;
        """;

        using var connection = CreateConnection();

        return await connection.QueryFirstOrDefaultAsync<Enrollment>(
            sql,
            new
            {
                StudentId = studentId,
                CourseId = courseId
            });
    }
}
