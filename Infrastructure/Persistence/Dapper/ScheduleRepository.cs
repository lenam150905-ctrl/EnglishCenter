using System.Text;
using Dapper;
using EnglishCenter.API.Models;
using EnglishCenter.Application.Abstractions.Persistence;

namespace EnglishCenter.Infrastructure.Persistence.Dapper;

public sealed class ScheduleRepository(IDbConnectionFactory connectionFactory)
    : DapperRepository(connectionFactory), IScheduleRepository
{
    public async Task<Schedule?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT s.Id, s.CourseId, s.TeacherId, s.StartTime, s.EndTime, s.Room, s.IsDeleted,
                   c.Id, c.CourseCode, c.CourseName, c.Description, c.Duration, c.TuitionFee, c.Status, c.IsDeleted,
                   t.Id, t.FullName, t.Email, t.Phone, t.Specialization, t.UserId, t.IsDeleted
            FROM Schedules s
            LEFT JOIN Courses c ON s.CourseId = c.Id
            LEFT JOIN Teachers t ON s.TeacherId = t.Id
            WHERE s.Id = @Id AND s.IsDeleted = 0;
            """;

        var list = await QueryAsync<Schedule, Course, Teacher, Schedule>(
            sql,
            (schedule, course, teacher) =>
            {
                schedule.Course = course;
                schedule.Teacher = teacher;
                return schedule;
            },
            new { Id = id },
            splitOn: "Id,Id",
            cancellationToken: cancellationToken);

        return list.FirstOrDefault();
    }

    public async Task<(IReadOnlyList<Schedule> Schedules, int TotalCount)> GetAllAsync(
        string? search,
        int? courseId,
        int? teacherId,
        string? sortBy,
        bool sortDesc,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var whereClause = new StringBuilder("WHERE s.IsDeleted = 0");
        var parameters = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(search))
        {
            whereClause.Append(" AND (s.Room LIKE @Search OR c.CourseName LIKE @Search OR t.FullName LIKE @Search)");
            parameters.Add("Search", $"%{search}%");
        }

        if (courseId.HasValue)
        {
            whereClause.Append(" AND s.CourseId = @CourseId");
            parameters.Add("CourseId", courseId.Value);
        }

        if (teacherId.HasValue)
        {
            whereClause.Append(" AND s.TeacherId = @TeacherId");
            parameters.Add("TeacherId", teacherId.Value);
        }

        var orderColumn = (sortBy?.ToLower()) switch
        {
            "starttime" => "s.StartTime",
            "endtime" => "s.EndTime",
            "room" => "s.Room",
            _ => "s.Id"
        };
        var orderDirection = sortDesc ? "DESC" : "ASC";

        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 20 : pageSize;
        var offset = (page - 1) * pageSize;

        parameters.Add("Offset", offset);
        parameters.Add("PageSize", pageSize);

        var countSql = $"""
            SELECT COUNT(1)
            FROM Schedules s
            LEFT JOIN Courses c ON s.CourseId = c.Id
            LEFT JOIN Teachers t ON s.TeacherId = t.Id
            {whereClause};
            """;

        var querySql = $"""
            SELECT s.Id, s.CourseId, s.TeacherId, s.StartTime, s.EndTime, s.Room, s.IsDeleted,
                   c.Id, c.CourseCode, c.CourseName, c.Description, c.Duration, c.TuitionFee, c.Status, c.IsDeleted,
                   t.Id, t.FullName, t.Email, t.Phone, t.Specialization, t.UserId, t.IsDeleted
            FROM Schedules s
            LEFT JOIN Courses c ON s.CourseId = c.Id
            LEFT JOIN Teachers t ON s.TeacherId = t.Id
            {whereClause}
            ORDER BY {orderColumn} {orderDirection}
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
            """;

        using var connection = CreateConnection();
        var totalCount = await connection.ExecuteScalarAsync<int>(new CommandDefinition(countSql, parameters, cancellationToken: cancellationToken));
        var schedules = (await connection.QueryAsync<Schedule, Course, Teacher, Schedule>(
            new CommandDefinition(querySql, parameters, cancellationToken: cancellationToken),
            (schedule, course, teacher) =>
            {
                schedule.Course = course;
                schedule.Teacher = teacher;
                return schedule;
            },
            splitOn: "Id,Id")).AsList();

        return (schedules, totalCount);
    }

    public async Task<bool> HasRoomConflictAsync(
        string room,
        DateTime startTime,
        DateTime endTime,
        int? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        var sql = excludeId.HasValue
            ? "SELECT COUNT(1) FROM Schedules WHERE Room = @Room AND @StartTime < EndTime AND @EndTime > StartTime AND Id <> @ExcludeId AND IsDeleted = 0;"
            : "SELECT COUNT(1) FROM Schedules WHERE Room = @Room AND @StartTime < EndTime AND @EndTime > StartTime AND IsDeleted = 0;";

        var count = await ExecuteScalarAsync<int>(sql, new { Room = room, StartTime = startTime, EndTime = endTime, ExcludeId = excludeId }, cancellationToken);
        return count > 0;
    }

    public async Task<bool> HasTeacherConflictAsync(
        int teacherId,
        DateTime startTime,
        DateTime endTime,
        int? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        var sql = excludeId.HasValue
            ? "SELECT COUNT(1) FROM Schedules WHERE TeacherId = @TeacherId AND @StartTime < EndTime AND @EndTime > StartTime AND Id <> @ExcludeId AND IsDeleted = 0;"
            : "SELECT COUNT(1) FROM Schedules WHERE TeacherId = @TeacherId AND @StartTime < EndTime AND @EndTime > StartTime AND IsDeleted = 0;";

        var count = await ExecuteScalarAsync<int>(sql, new { TeacherId = teacherId, StartTime = startTime, EndTime = endTime, ExcludeId = excludeId }, cancellationToken);
        return count > 0;
    }

    public async Task CreateAsync(Schedule schedule, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO Schedules (CourseId, TeacherId, StartTime, EndTime, Room, IsDeleted)
            OUTPUT INSERTED.Id
            VALUES (@CourseId, @TeacherId, @StartTime, @EndTime, @Room, @IsDeleted);
            """;
        schedule.Id = await ExecuteScalarAsync<int>(sql, schedule, cancellationToken);
    }

    public async Task<bool> UpdateAsync(Schedule schedule, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE Schedules
            SET CourseId = @CourseId,
                TeacherId = @TeacherId,
                StartTime = @StartTime,
                EndTime = @EndTime,
                Room = @Room
            WHERE Id = @Id AND IsDeleted = 0;
            """;
        return await ExecuteAsync(sql, schedule, cancellationToken) > 0;
    }

    public async Task<bool> SoftDeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = "UPDATE Schedules SET IsDeleted = 1 WHERE Id = @Id AND IsDeleted = 0;";
        return await ExecuteAsync(sql, new { Id = id }, cancellationToken) > 0;
    }
}
