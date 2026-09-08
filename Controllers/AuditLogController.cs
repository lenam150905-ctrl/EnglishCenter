using EnglishCenter.API.DTOs;
using EnglishCenter.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnglishCenter.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AuditLogController : ControllerBase
    {
        private readonly IAuditLogService _auditLogService;

        public AuditLogController(
            IAuditLogService auditLogService)
        {
            _auditLogService = auditLogService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _auditLogService.GetAllAsync();

            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] AuditLogDto dto)
        {
            var ipAddress =
                HttpContext.Connection.RemoteIpAddress?
                    .ToString();

            var result = await _auditLogService.CreateAsync(
                dto.UserId,
                dto.Action,
                dto.EntityName,
                dto.EntityId,
                dto.Description,
                ipAddress);

            return Ok(result);
        }
    }
}