using EnglishCenter.API.Models;

namespace EnglishCenter.Application.Abstractions.Persistence;

public interface IGradeRepository
{
    Task<Grade?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Grade?> GetByStudentAndCourseAsync(int studentId, int courseId, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(int examId, int studentId, int? excludeId = null, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Grade> Grades, int TotalCount)> GetAllAsync(
        string? search,
        int? examId,
        int? studentId,
        decimal? minScore,
        decimal? maxScore,
        string? sortBy,
        bool sortDesc,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task CreateAsync(Grade grade, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(Grade grade, CancellationToken cancellationToken = default);
    Task<bool> SoftDeleteAsync(int id, CancellationToken cancellationToken = default);
}
