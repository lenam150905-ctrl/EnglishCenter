using EnglishCenter.API.Models;

namespace EnglishCenter.Application.Abstractions.Persistence;

public interface IInvoiceRepository
{
    Task<Invoice?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> ExistsByEnrollmentAsync(int enrollmentId, int? excludeId = null, CancellationToken cancellationToken = default);
    Task<bool> HasPaidInvoiceForEnrollmentAsync(int enrollmentId, CancellationToken cancellationToken = default);
    Task<bool> HasPaidInvoiceForStudentAndCourseAsync(int studentId, int courseId, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Invoice> Invoices, int TotalCount)> GetAllAsync(
        string? search,
        int? studentId,
        int? enrollmentId,
        string? status,
        decimal? minAmount,
        decimal? maxAmount,
        string? sortBy,
        bool sortDesc,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task CreateAsync(Invoice invoice, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(Invoice invoice, CancellationToken cancellationToken = default);
    Task<bool> UpdateStatusAsync(int id, string status, CancellationToken cancellationToken = default);
    Task<bool> SoftDeleteAsync(int id, CancellationToken cancellationToken = default);
}
