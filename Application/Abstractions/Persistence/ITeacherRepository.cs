using EnglishCenter.API.Models;

namespace EnglishCenter.Application.Abstractions.Persistence;

public interface ITeacherRepository
{
    Task<Teacher?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> ExistsByEmailAsync(string email, int? excludeId = null, CancellationToken cancellationToken = default);
    Task<bool> ExistsByPhoneAsync(string phone, int? excludeId = null, CancellationToken cancellationToken = default);
    Task<bool> ExistsByUserIdAsync(int userId, int? excludeId = null, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Teacher> Teachers, int TotalCount)> GetAllAsync(
        string? search,
        string? specialization,
        string? sortBy,
        bool sortDesc,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task CreateAsync(Teacher teacher, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(Teacher teacher, CancellationToken cancellationToken = default);
    Task<bool> SoftDeleteAsync(int id, CancellationToken cancellationToken = default);
}
