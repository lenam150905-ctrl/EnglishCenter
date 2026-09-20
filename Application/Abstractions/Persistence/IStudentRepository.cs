using EnglishCenter.API.Models;

namespace EnglishCenter.Application.Abstractions.Persistence;

public interface IStudentRepository
{
    Task<Student?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> ExistsByEmailAsync(string email, int? excludeId = null, CancellationToken cancellationToken = default);
    Task<bool> ExistsByPhoneAsync(string phone, int? excludeId = null, CancellationToken cancellationToken = default);
    Task<bool> ExistsByUserIdAsync(int userId, int? excludeId = null, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Student> Students, int TotalCount)> GetAllAsync(
        string? search,
        DateTime? fromDateOfBirth,
        DateTime? toDateOfBirth,
        string? sortBy,
        bool sortDesc,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task CreateAsync(Student student, CancellationToken cancellationToken = default);
    Task CreateRangeAsync(IEnumerable<Student> students, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(Student student, CancellationToken cancellationToken = default);
    Task<bool> SoftDeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<Student?> GetByUserIdAsync(
    int userId,
    CancellationToken cancellationToken = default);
}
