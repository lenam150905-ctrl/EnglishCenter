using System.Text;
using Dapper;
using EnglishCenter.API.Models;
using EnglishCenter.Application.Abstractions.Persistence;

namespace EnglishCenter.Infrastructure.Persistence.Dapper;

public sealed class CourseRepository(IDbConnectionFactory connectionFactory)
    : DapperRepository(connectionFactory), ICourseRepository
{
    public Task<Course?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        QueryFirstOrDefaultAsync<Course>(
            "SELECT Id, CourseCode, CourseName, Description, Duration, TuitionFee, Status, IsDeleted FROM Courses WHERE Id = @Id AND IsDeleted = 0;",
            new { Id = id },
            cancellationToken);

    public async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
    {
        var count = await ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM Courses WHERE Id = @Id AND IsDeleted = 0;",
            new { Id = id },
            cancellationToken);
        return count > 0;
    }

    public async Task<(IReadOnlyList<Course> Courses, int TotalCount)> GetAllAsync(
        string? search,
        decimal? minTuitionFee,
        decimal? maxTuitionFee,
        string? sortBy,
        bool sortDesc,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var whereClause = new StringBuilder("WHERE IsDeleted = 0");
        var parameters = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(search))
        {
            whereClause.Append(" AND (CourseName LIKE @Search OR Description LIKE @Search OR CourseCode LIKE @Search)");
            parameters.Add("Search", $"%{search}%");
        }

        if (minTuitionFee.HasValue)
        {
            whereClause.Append(" AND TuitionFee >= @MinTuitionFee");
            parameters.Add("MinTuitionFee", minTuitionFee.Value);
        }

        if (maxTuitionFee.HasValue)
        {
            whereClause.Append(" AND TuitionFee <= @MaxTuitionFee");
            parameters.Add("MaxTuitionFee", maxTuitionFee.Value);
        }

        var orderColumn = (sortBy?.ToLower()) switch
        {
            "coursename" => "CourseName",
            "tuitionfee" => "TuitionFee",
            "duration" => "Duration",
            _ => "Id"
        };
        var orderDirection = sortDesc ? "DESC" : "ASC";

        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 20 : pageSize;
        var offset = (page - 1) * pageSize;

        parameters.Add("Offset", offset);
        parameters.Add("PageSize", pageSize);

        var countSql = $"SELECT COUNT(1) FROM Courses {whereClause};";
        var querySql = $"""
            SELECT Id, CourseCode, CourseName, Description, Duration, TuitionFee, Status, IsDeleted
            FROM Courses
            {whereClause}
            ORDER BY {orderColumn} {orderDirection}
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
            """;

        using var connection = CreateConnection();
        var totalCount = await connection.ExecuteScalarAsync<int>(new CommandDefinition(countSql, parameters, cancellationToken: cancellationToken));
        var courses = (await connection.QueryAsync<Course>(new CommandDefinition(querySql, parameters, cancellationToken: cancellationToken))).AsList();

        return (courses, totalCount);
    }

    public async Task CreateAsync(Course course, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO Courses (CourseCode, CourseName, Description, Duration, TuitionFee, Status, IsDeleted)
            OUTPUT INSERTED.Id
            VALUES (@CourseCode, @CourseName, @Description, @Duration, @TuitionFee, @Status, @IsDeleted);
            """;
        course.Id = await ExecuteScalarAsync<int>(sql, course, cancellationToken);
    }

    public async Task<bool> UpdateAsync(Course course, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE Courses
            SET CourseCode = @CourseCode,
                CourseName = @CourseName,
                Description = @Description,
                Duration = @Duration,
                TuitionFee = @TuitionFee,
                Status = @Status
            WHERE Id = @Id AND IsDeleted = 0;
            """;
        return await ExecuteAsync(sql, course, cancellationToken) > 0;
    }

    public async Task<bool> SoftDeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = "UPDATE Courses SET IsDeleted = 1 WHERE Id = @Id AND IsDeleted = 0;";
        return await ExecuteAsync(sql, new { Id = id }, cancellationToken) > 0;
    }
}
