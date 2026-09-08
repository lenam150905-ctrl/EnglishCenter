using EnglishCenter.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnglishCenter.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ReportController : ControllerBase
    {
        private readonly IReportService _reportService;

        public ReportController(
            IReportService reportService)
        {
            _reportService = reportService;
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard()
        {
            var result =
                await _reportService.GetDashboardAsync();

            return Ok(result);
        }

        [HttpGet("courses")]
        public async Task<IActionResult> GetCourseReport()
        {
            var result =
                await _reportService.GetCourseReportAsync();

            return Ok(result);
        }
    }
}