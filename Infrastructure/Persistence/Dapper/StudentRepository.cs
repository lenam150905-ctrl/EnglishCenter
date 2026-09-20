using Dapper;
using EnglishCenter.API.Models;
using EnglishCenter.Application.Abstractions.Persistence;
using System.Data;
using System.Text;

namespace EnglishCenter.Infrastructure.Persistence.Dapper;

public sealed class StudentRepository(IDbConnectionFactory connectionFactory)
    : DapperRepository(connectionFactory), IStudentRepository
{
    public async Task<Student?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT s.Id, s.FullName, s.DateOfBirth, s.Email, s.Phone, s.Address, s.UserId, s.IsDeleted,
                   u.Id, u.UserName, u.Email, u.PasswordHash, u.Role, u.IsDeleted
            FROM Students s
            LEFT JOIN Users u ON s.UserId = u.Id
            WHERE s.Id = @Id AND s.IsDeleted = 0;
            """;

        var list = await QueryAsync<Student, User, Student>(
            sql,
            (student, user) =>
            {
                student.User = user;
                return student;
            },
            new { Id = id },
            splitOn: "Id",
            cancellationToken: cancellationToken);

        return list.FirstOrDefault();
    }

    public async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
    {
        var count = await ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM Students WHERE Id = @Id AND IsDeleted = 0;",
            new { Id = id },
            cancellationToken);
        return count > 0;
    }

    public async Task<bool> ExistsByEmailAsync(string email, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        var sql = excludeId.HasValue
            ? "SELECT COUNT(1) FROM Students WHERE Email = @Email AND Id <> @ExcludeId AND IsDeleted = 0;"
            : "SELECT COUNT(1) FROM Students WHERE Email = @Email AND IsDeleted = 0;";
        var count = await ExecuteScalarAsync<int>(sql, new { Email = email, ExcludeId = excludeId }, cancellationToken);
        return count > 0;
    }

    public async Task<bool> ExistsByPhoneAsync(string phone, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        var sql = excludeId.HasValue
            ? "SELECT COUNT(1) FROM Students WHERE Phone = @Phone AND Id <> @ExcludeId AND IsDeleted = 0;"
            : "SELECT COUNT(1) FROM Students WHERE Phone = @Phone AND IsDeleted = 0;";
        var count = await ExecuteScalarAsync<int>(sql, new { Phone = phone, ExcludeId = excludeId }, cancellationToken);
        return count > 0;
    }

    public async Task<bool> ExistsByUserIdAsync(int userId, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        var sql = excludeId.HasValue
            ? "SELECT COUNT(1) FROM Students WHERE UserId = @UserId AND Id <> @ExcludeId AND IsDeleted = 0;"
            : "SELECT COUNT(1) FROM Students WHERE UserId = @UserId AND IsDeleted = 0;";
        var count = await ExecuteScalarAsync<int>(sql, new { UserId = userId, ExcludeId = excludeId }, cancellationToken);
        return count > 0;
    }

    public async Task<(IReadOnlyList<Student> Students, int TotalCount)> GetAllAsync(
        string? search,
        DateTime? fromDateOfBirth,
        DateTime? toDateOfBirth,
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
            whereClause.Append(" AND (FullName LIKE @Search OR Email LIKE @Search OR Phone LIKE @Search OR Address LIKE @Search)");
            parameters.Add("Search", $"%{search}%");
        }

        if (fromDateOfBirth.HasValue)
        {
            whereClause.Append(" AND DateOfBirth >= @FromDateOfBirth");
            parameters.Add("FromDateOfBirth", fromDateOfBirth.Value);
        }

        if (toDateOfBirth.HasValue)
        {
            whereClause.Append(" AND DateOfBirth <= @ToDateOfBirth");
            parameters.Add("ToDateOfBirth", toDateOfBirth.Value);
        }

        var orderColumn = (sortBy?.ToLower()) switch
        {
            "fullname" => "FullName",
            "dateofbirth" => "DateOfBirth",
            "email" => "Email",
            _ => "Id"
        };
        var orderDirection = sortDesc ? "DESC" : "ASC";

        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 20 : pageSize;
        var offset = (page - 1) * pageSize;

        parameters.Add("Offset", offset);
        parameters.Add("PageSize", pageSize);

        var countSql = $"SELECT COUNT(1) FROM Students {whereClause};";
        var querySql = $"""
            SELECT Id, FullName, DateOfBirth, Email, Phone, Address, UserId, IsDeleted
            FROM Students
            {whereClause}
            ORDER BY {orderColumn} {orderDirection}
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
            """;

        using var connection = CreateConnection();
        var totalCount = await connection.ExecuteScalarAsync<int>(new CommandDefinition(countSql, parameters, cancellationToken: cancellationToken));
        var students = (await connection.QueryAsync<Student>(new CommandDefinition(querySql, parameters, cancellationToken: cancellationToken))).AsList();

        return (students, totalCount);
    }

    public async Task CreateAsync(Student student, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO Students (FullName, DateOfBirth, Email, Phone, Address, UserId, IsDeleted)
            OUTPUT INSERTED.Id
            VALUES (@FullName, @DateOfBirth, @Email, @Phone, @Address, @UserId, @IsDeleted);
            """;
        student.Id = await ExecuteScalarAsync<int>(sql, student, cancellationToken);
    }

    public async Task CreateRangeAsync(IEnumerable<Student> students, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO Students (FullName, DateOfBirth, Email, Phone, Address, UserId, IsDeleted)
            VALUES (@FullName, @DateOfBirth, @Email, @Phone, @Address, @UserId, @IsDeleted);
            """;

        using var connection = CreateConnection();
        if (connection.State != ConnectionState.Open)
        {
            connection.Open();
        }
        using var transaction = connection.BeginTransaction();
        try
        {
            await connection.ExecuteAsync(new CommandDefinition(sql, students, transaction: transaction, cancellationToken: cancellationToken));
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<bool> UpdateAsync(Student student, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE Students
            SET FullName = @FullName,
                DateOfBirth = @DateOfBirth,
                Email = @Email,
                Phone = @Phone,
                Address = @Address,
                UserId = @UserId
            WHERE Id = @Id AND IsDeleted = 0;
            """;
        return await ExecuteAsync(sql, student, cancellationToken) > 0;
    }

    public async Task<bool> SoftDeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = "UPDATE Students SET IsDeleted = 1 WHERE Id = @Id AND IsDeleted = 0;";
        return await ExecuteAsync(sql, new { Id = id }, cancellationToken) > 0;
    }
    public async Task<Student?> GetByUserIdAsync(
     int userId,
     CancellationToken cancellationToken = default)
    {
        const string sql = """
        SELECT Id,
               FullName,
               DateOfBirth,
               Email,
               Phone,
               Address,
               UserId,
               IsDeleted
        FROM Students
        WHERE UserId = @UserId
          AND IsDeleted = 0;
        """;

        using var connection = CreateConnection();

        return await connection.QueryFirstOrDefaultAsync<Student>(
            new CommandDefinition(
                sql,
                new { UserId = userId },
                cancellationToken: cancellationToken));
    }
}
