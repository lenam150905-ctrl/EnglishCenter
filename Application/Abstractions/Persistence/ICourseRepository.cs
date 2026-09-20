using EnglishCenter.API.Models;

namespace EnglishCenter.Application.Abstractions.Persistence;

public interface ICourseRepository
{
    Task<Course?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Course> Courses, int TotalCount)> GetAllAsync(
        string? search,
        decimal? minTuitionFee,
        decimal? maxTuitionFee,
        string? sortBy,
        bool sortDesc,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task CreateAsync(Course course, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(Course course, CancellationToken cancellationToken = default);
    Task<bool> SoftDeleteAsync(int id, CancellationToken cancellationToken = default);
}
