using System.Text;
using Dapper;
using EnglishCenter.API.Models;
using EnglishCenter.Application.Abstractions.Persistence;

namespace EnglishCenter.Infrastructure.Persistence.Dapper;

public sealed class TeacherRepository(IDbConnectionFactory connectionFactory)
    : DapperRepository(connectionFactory), ITeacherRepository
{
    public async Task<Teacher?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT t.Id, t.FullName, t.Email, t.Phone, t.Specialization, t.UserId, t.IsDeleted,
                   u.Id, u.UserName, u.Email, u.PasswordHash, u.Role, u.IsDeleted
            FROM Teachers t
            LEFT JOIN Users u ON t.UserId = u.Id
            WHERE t.Id = @Id AND t.IsDeleted = 0;
            """;

        var list = await QueryAsync<Teacher, User, Teacher>(
            sql,
            (teacher, user) =>
            {
                teacher.User = user;
                return teacher;
            },
            new { Id = id },
            splitOn: "Id",
            cancellationToken: cancellationToken);

        return list.FirstOrDefault();
    }

    public async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
    {
        var count = await ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM Teachers WHERE Id = @Id AND IsDeleted = 0;",
            new { Id = id },
            cancellationToken);
        return count > 0;
    }

    public async Task<bool> ExistsByEmailAsync(string email, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        var sql = excludeId.HasValue
            ? "SELECT COUNT(1) FROM Teachers WHERE Email = @Email AND Id <> @ExcludeId AND IsDeleted = 0;"
            : "SELECT COUNT(1) FROM Teachers WHERE Email = @Email AND IsDeleted = 0;";
        var count = await ExecuteScalarAsync<int>(sql, new { Email = email, ExcludeId = excludeId }, cancellationToken);
        return count > 0;
    }

    public async Task<bool> ExistsByPhoneAsync(string phone, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        var sql = excludeId.HasValue
            ? "SELECT COUNT(1) FROM Teachers WHERE Phone = @Phone AND Id <> @ExcludeId AND IsDeleted = 0;"
            : "SELECT COUNT(1) FROM Teachers WHERE Phone = @Phone AND IsDeleted = 0;";
        var count = await ExecuteScalarAsync<int>(sql, new { Phone = phone, ExcludeId = excludeId }, cancellationToken);
        return count > 0;
    }

    public async Task<bool> ExistsByUserIdAsync(int userId, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        var sql = excludeId.HasValue
            ? "SELECT COUNT(1) FROM Teachers WHERE UserId = @UserId AND Id <> @ExcludeId AND IsDeleted = 0;"
            : "SELECT COUNT(1) FROM Teachers WHERE UserId = @UserId AND IsDeleted = 0;";
        var count = await ExecuteScalarAsync<int>(sql, new { UserId = userId, ExcludeId = excludeId }, cancellationToken);
        return count > 0;
    }

    public async Task<(IReadOnlyList<Teacher> Teachers, int TotalCount)> GetAllAsync(
        string? search,
        string? specialization,
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
            whereClause.Append(" AND (FullName LIKE @Search OR Email LIKE @Search OR Phone LIKE @Search OR Specialization LIKE @Search)");
            parameters.Add("Search", $"%{search}%");
        }

        if (!string.IsNullOrWhiteSpace(specialization))
        {
            whereClause.Append(" AND Specialization = @Specialization");
            parameters.Add("Specialization", specialization);
        }

        var orderColumn = (sortBy?.ToLower()) switch
        {
            "fullname" => "FullName",
            "email" => "Email",
            "specialization" => "Specialization",
            _ => "Id"
        };
        var orderDirection = sortDesc ? "DESC" : "ASC";

        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 20 : pageSize;
        var offset = (page - 1) * pageSize;

        parameters.Add("Offset", offset);
        parameters.Add("PageSize", pageSize);

        var countSql = $"SELECT COUNT(1) FROM Teachers {whereClause};";
        var querySql = $"""
            SELECT Id, FullName, Email, Phone, Specialization, UserId, IsDeleted
            FROM Teachers
            {whereClause}
            ORDER BY {orderColumn} {orderDirection}
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
            """;

        using var connection = CreateConnection();
        var totalCount = await connection.ExecuteScalarAsync<int>(new CommandDefinition(countSql, parameters, cancellationToken: cancellationToken));
        var teachers = (await connection.QueryAsync<Teacher>(new CommandDefinition(querySql, parameters, cancellationToken: cancellationToken))).AsList();

        return (teachers, totalCount);
    }

    public async Task CreateAsync(Teacher teacher, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO Teachers (FullName, Email, Phone, Specialization, UserId, IsDeleted)
            OUTPUT INSERTED.Id
            VALUES (@FullName, @Email, @Phone, @Specialization, @UserId, @IsDeleted);
            """;
        teacher.Id = await ExecuteScalarAsync<int>(sql, teacher, cancellationToken);
    }

    public async Task<bool> UpdateAsync(Teacher teacher, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE Teachers
            SET FullName = @FullName,
                Email = @Email,
                Phone = @Phone,
                Specialization = @Specialization,
                UserId = @UserId
            WHERE Id = @Id AND IsDeleted = 0;
            """;
        return await ExecuteAsync(sql, teacher, cancellationToken) > 0;
    }

    public async Task<bool> SoftDeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = "UPDATE Teachers SET IsDeleted = 1 WHERE Id = @Id AND IsDeleted = 0;";
        return await ExecuteAsync(sql, new { Id = id }, cancellationToken) > 0;
    }
}
