using EnglishCenter.API.Models;

namespace EnglishCenter.Application.Abstractions.Persistence;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<User?> GetByUserNameOrEmailAsync(string identifier, CancellationToken cancellationToken = default);
    Task<bool> ExistsByUserNameAsync(string userName, int? excludeId = null, CancellationToken cancellationToken = default);
    Task<bool> ExistsByEmailAsync(string email, int? excludeId = null, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<User> Users, int TotalCount)> GetAllAsync(
        string? search,
        string? role,
        string? sortBy,
        bool sortDesc,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task CreateAsync(User user, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(User user, CancellationToken cancellationToken = default);
    Task<bool> SoftDeleteAsync(int id, CancellationToken cancellationToken = default);

    // Login OTP operations
    Task<LoginOtp?> GetLatestLoginOtpAsync(int userId, CancellationToken cancellationToken = default);
    Task InvalidateOldLoginOtpsAsync(int userId, CancellationToken cancellationToken = default);
    Task CreateLoginOtpAsync(LoginOtp otp, CancellationToken cancellationToken = default);
    Task UpdateLoginOtpAsync(LoginOtp otp, CancellationToken cancellationToken = default);

    // Password Reset OTP operations
    Task<PasswordResetOtp?> GetLatestPasswordResetOtpAsync(int userId, bool onlyVerified = false, CancellationToken cancellationToken = default);
    Task InvalidateOldPasswordResetOtpsAsync(int userId, CancellationToken cancellationToken = default);
    Task CreatePasswordResetOtpAsync(PasswordResetOtp otp, CancellationToken cancellationToken = default);
    Task UpdatePasswordResetOtpAsync(PasswordResetOtp otp, CancellationToken cancellationToken = default);
}
