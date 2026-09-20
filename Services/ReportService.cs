using EnglishCenter.API.DTOs;
using EnglishCenter.Application.Abstractions.Persistence;

namespace EnglishCenter.API.Services
{
    public class ReportService : IReportService
    {
        private readonly IReportRepository _reportRepository;

        public ReportService(IReportRepository reportRepository)
        {
            _reportRepository = reportRepository;
        }

        public Task<ReportDashboardDto> GetDashboardAsync()
        {
            return _reportRepository.GetDashboardAsync();
        }

        public Task<List<CourseReportDto>> GetCourseReportAsync()
        {
            return _reportRepository.GetCourseReportAsync();
        }
    }
}