using System.Text;
using Dapper;
using EnglishCenter.API.Models;
using EnglishCenter.Application.Abstractions.Persistence;

namespace EnglishCenter.Infrastructure.Persistence.Dapper;

public sealed class UserRepository(IDbConnectionFactory connectionFactory)
    : DapperRepository(connectionFactory), IUserRepository
{
    public Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        QueryFirstOrDefaultAsync<User>(
            "SELECT Id, UserName, Email, PasswordHash, Role, IsDeleted FROM Users WHERE Id = @Id AND IsDeleted = 0;",
            new { Id = id },
            cancellationToken);

    public Task<User?> GetByUserNameOrEmailAsync(string identifier, CancellationToken cancellationToken = default) =>
        QueryFirstOrDefaultAsync<User>(
            "SELECT Id, UserName, Email, PasswordHash, Role, IsDeleted FROM Users WHERE (UserName = @Identifier OR Email = @Identifier) AND IsDeleted = 0;",
            new { Identifier = identifier },
            cancellationToken);

    public async Task<bool> ExistsByUserNameAsync(string userName, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        var sql = excludeId.HasValue
            ? "SELECT COUNT(1) FROM Users WHERE UserName = @UserName AND Id <> @ExcludeId AND IsDeleted = 0;"
            : "SELECT COUNT(1) FROM Users WHERE UserName = @UserName AND IsDeleted = 0;";
        var count = await ExecuteScalarAsync<int>(sql, new { UserName = userName, ExcludeId = excludeId }, cancellationToken);
        return count > 0;
    }

    public async Task<bool> ExistsByEmailAsync(string email, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        var sql = excludeId.HasValue
            ? "SELECT COUNT(1) FROM Users WHERE Email = @Email AND Id <> @ExcludeId AND IsDeleted = 0;"
            : "SELECT COUNT(1) FROM Users WHERE Email = @Email AND IsDeleted = 0;";
        var count = await ExecuteScalarAsync<int>(sql, new { Email = email, ExcludeId = excludeId }, cancellationToken);
        return count > 0;
    }

    public async Task<(IReadOnlyList<User> Users, int TotalCount)> GetAllAsync(
        string? search,
        string? role,
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
            whereClause.Append(" AND (UserName LIKE @Search OR Email LIKE @Search OR Role LIKE @Search)");
            parameters.Add("Search", $"%{search}%");
        }

        if (!string.IsNullOrWhiteSpace(role))
        {
            whereClause.Append(" AND Role = @Role");
            parameters.Add("Role", role);
        }

        var orderColumn = (sortBy?.ToLower()) switch
        {
            "username" => "UserName",
            "role" => "Role",
            _ => "Id"
        };
        var orderDirection = sortDesc ? "DESC" : "ASC";

        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 20 : pageSize;
        var offset = (page - 1) * pageSize;

        parameters.Add("Offset", offset);
        parameters.Add("PageSize", pageSize);

        var countSql = $"SELECT COUNT(1) FROM Users {whereClause};";
        var querySql = $"""
            SELECT Id, UserName, Email, PasswordHash, Role, IsDeleted
            FROM Users
            {whereClause}
            ORDER BY {orderColumn} {orderDirection}
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
            """;

        using var connection = CreateConnection();
        var totalCount = await connection.ExecuteScalarAsync<int>(new CommandDefinition(countSql, parameters, cancellationToken: cancellationToken));
        var users = (await connection.QueryAsync<User>(new CommandDefinition(querySql, parameters, cancellationToken: cancellationToken))).AsList();

        return (users, totalCount);
    }

    public async Task CreateAsync(User user, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO Users (UserName, Email, PasswordHash, Role, IsDeleted)
            OUTPUT INSERTED.Id
            VALUES (@UserName, @Email, @PasswordHash, @Role, @IsDeleted);
            """;
        user.Id = await ExecuteScalarAsync<int>(sql, user, cancellationToken);
    }

    public async Task<bool> UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE Users
            SET UserName = @UserName,
                Email = @Email,
                PasswordHash = @PasswordHash,
                Role = @Role
            WHERE Id = @Id AND IsDeleted = 0;
            """;
        return await ExecuteAsync(sql, user, cancellationToken) > 0;
    }

    public async Task<bool> SoftDeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = "UPDATE Users SET IsDeleted = 1 WHERE Id = @Id AND IsDeleted = 0;";
        return await ExecuteAsync(sql, new { Id = id }, cancellationToken) > 0;
    }

    // Login OTP
    public Task<LoginOtp?> GetLatestLoginOtpAsync(int userId, CancellationToken cancellationToken = default) =>
        QueryFirstOrDefaultAsync<LoginOtp>(
            "SELECT TOP 1 Id, UserId, Otp, CreatedAt, ExpiredAt, IsVerified, FailedAttempts FROM LoginOtps WHERE UserId = @UserId ORDER BY Id DESC;",
            new { UserId = userId },
            cancellationToken);

    public Task InvalidateOldLoginOtpsAsync(int userId, CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            "UPDATE LoginOtps SET IsVerified = 1 WHERE UserId = @UserId AND IsVerified = 0;",
            new { UserId = userId },
            cancellationToken);

    public async Task CreateLoginOtpAsync(LoginOtp otp, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO LoginOtps (UserId, Otp, CreatedAt, ExpiredAt, IsVerified, FailedAttempts)
            OUTPUT INSERTED.Id
            VALUES (@UserId, @Otp, @CreatedAt, @ExpiredAt, @IsVerified, @FailedAttempts);
            """;
        otp.Id = await ExecuteScalarAsync<int>(sql, otp, cancellationToken);
    }

    public Task UpdateLoginOtpAsync(LoginOtp otp, CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            "UPDATE LoginOtps SET IsVerified = @IsVerified, FailedAttempts = @FailedAttempts WHERE Id = @Id;",
            otp,
            cancellationToken);

    // Password Reset OTP
    public Task<PasswordResetOtp?> GetLatestPasswordResetOtpAsync(int userId, bool onlyVerified = false, CancellationToken cancellationToken = default)
    {
        var sql = onlyVerified
            ? "SELECT TOP 1 Id, UserId, Otp, CreatedAt, ExpiredAt, IsVerified, FailedAttempts FROM PasswordResetOtps WHERE UserId = @UserId AND IsVerified = 1 ORDER BY Id DESC;"
            : "SELECT TOP 1 Id, UserId, Otp, CreatedAt, ExpiredAt, IsVerified, FailedAttempts FROM PasswordResetOtps WHERE UserId = @UserId ORDER BY Id DESC;";
        return QueryFirstOrDefaultAsync<PasswordResetOtp>(sql, new { UserId = userId }, cancellationToken);
    }

    public Task InvalidateOldPasswordResetOtpsAsync(int userId, CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            "UPDATE PasswordResetOtps SET IsVerified = 1 WHERE UserId = @UserId AND IsVerified = 0;",
            new { UserId = userId },
            cancellationToken);

    public async Task CreatePasswordResetOtpAsync(PasswordResetOtp otp, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO PasswordResetOtps (UserId, Otp, CreatedAt, ExpiredAt, IsVerified, FailedAttempts)
            OUTPUT INSERTED.Id
            VALUES (@UserId, @Otp, @CreatedAt, @ExpiredAt, @IsVerified, @FailedAttempts);
            """;
        otp.Id = await ExecuteScalarAsync<int>(sql, otp, cancellationToken);
    }

    public Task UpdatePasswordResetOtpAsync(PasswordResetOtp otp, CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            "UPDATE PasswordResetOtps SET IsVerified = @IsVerified, FailedAttempts = @FailedAttempts WHERE Id = @Id;",
            otp,
            cancellationToken);
}
