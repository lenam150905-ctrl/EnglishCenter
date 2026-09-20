using EnglishCenter.API.DTOs;

namespace EnglishCenter.Application.Abstractions.Persistence;

public interface IReportRepository
{
    Task<ReportDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default);
    Task<List<CourseReportDto>> GetCourseReportAsync(CancellationToken cancellationToken = default);
}
