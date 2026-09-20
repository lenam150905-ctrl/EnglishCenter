using EnglishCenter.API.Models;

namespace EnglishCenter.Application.Abstractions.Persistence;

public interface IEnrollmentRepository
{
    Task<Enrollment?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(int studentId, int courseId, int? excludeId = null, CancellationToken cancellationToken = default);
    Task<bool> HasActiveEnrollmentForCourseAsync(int courseId, CancellationToken cancellationToken = default);
    Task<int?> GetStudentUserIdByEnrollmentIdAsync(int enrollmentId, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Enrollment> Enrollments, int TotalCount)> GetAllAsync(
        string? search,
        int? studentId,
        int? courseId,
        string? status,
        string? sortBy,
        bool sortDesc,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task CreateAsync(Enrollment enrollment, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(Enrollment enrollment, CancellationToken cancellationToken = default);
    Task<bool> UpdateStatusAsync(int id, string status, CancellationToken cancellationToken = default);
    Task<bool> SoftDeleteAsync(int id, CancellationToken cancellationToken = default);

    Task<Enrollment?> GetByStudentAndCourseAsync(
        int studentId,
        int courseId);
}
