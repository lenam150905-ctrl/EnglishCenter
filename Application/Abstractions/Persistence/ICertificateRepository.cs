using EnglishCenter.API.Models;

namespace EnglishCenter.Application.Abstractions.Persistence;

public interface ICertificateRepository
{
    Task<Certificate?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(int studentId, int courseId, int? excludeId = null, CancellationToken cancellationToken = default);
    Task<bool> ExistsByCodeAsync(string code, int? excludeId = null, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Certificate> Certificates, int TotalCount)> GetAllAsync(
        string? search,
        int? studentId,
        int? courseId,
        string? sortBy,
        bool sortDesc,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task CreateAsync(Certificate certificate, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(Certificate certificate, CancellationToken cancellationToken = default);
    Task<bool> UpdatePdfPathAsync(int id, string pdfPath, CancellationToken cancellationToken = default);
    Task<bool> SoftDeleteAsync(int id, CancellationToken cancellationToken = default);
}
