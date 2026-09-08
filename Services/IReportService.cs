using EnglishCenter.API.DTOs;

namespace EnglishCenter.API.Services
{
    public interface IReportService
    {
        Task<ReportDashboardDto> GetDashboardAsync();

        Task<List<CourseReportDto>> GetCourseReportAsync();
    }
}