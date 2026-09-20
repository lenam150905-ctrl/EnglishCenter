using EnglishCenter.API.Models;

namespace EnglishCenter.Application.Abstractions.Persistence;

public interface IExamRepository
{
    Task<Exam?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Exam> Exams, int TotalCount)> GetAllAsync(
        string? search,
        int? courseId,
        string? examType,
        string? sortBy,
        bool sortDesc,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task CreateAsync(Exam exam, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(Exam exam, CancellationToken cancellationToken = default);
    Task<bool> SoftDeleteAsync(int id, CancellationToken cancellationToken = default);
}
