using EnglishCenter.API.DTOs;
using EnglishCenter.API.Models;
using EnglishCenter.API.Middleware;
using EnglishCenter.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnglishCenter.API.Controllers
{
    [Asp.Versioning.ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SchedulesController : ControllerBase
    {
        private readonly IScheduleService _scheduleService;
        private readonly ISoftDeleteService _softDeleteService;
        private readonly ITeacherScopeService _teacherScopeService;

        public SchedulesController(IScheduleService scheduleService, ISoftDeleteService softDeleteService, ITeacherScopeService teacherScopeService)
        {
            _scheduleService = scheduleService;
            _softDeleteService = softDeleteService;
            _teacherScopeService = teacherScopeService;
        }

[HttpGet]
        [Authorize(Roles = "Admin,Teacher,Student")]
        public async Task<ActionResult<PagedResultDto<ScheduleDto>>> GetSchedules(
    string? search,
    int? courseId,
    int? teacherId,
    string? sortBy,
    bool sortDesc = false,
    int page = 1,
    int pageSize = 20)
        {
            if (User.IsInRole("Teacher"))
            {
                var ownTeacherId = await GetCurrentTeacherIdAsync();
                if (!ownTeacherId.HasValue) return Forbid();
                if (teacherId.HasValue && teacherId != ownTeacherId) return Forbid();
                teacherId = ownTeacherId;
            }
            var schedules = await _scheduleService.GetAllAsync(
                search,
                courseId,
                teacherId,
                sortBy,
                sortDesc,
                page,
                pageSize);

            return Ok(schedules);
        }

[HttpGet("{id}")]
        [Authorize(Roles = "Admin,Teacher,Student")]
        public async Task<ActionResult<ScheduleDto>> GetSchedule(int id)
        {
            var schedule = await _scheduleService.GetByIdAsync(id);

            if (schedule == null)
            {
                return NotFound();
            }
            if (User.IsInRole("Teacher") && !await IsOwnScheduleAsync(schedule.TeacherId)) return Forbid();

            return Ok(schedule);
        }

[HttpPost]
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<ActionResult<ScheduleDto>> CreateSchedule(
            ScheduleCreateDto dto)
        {
            if (User.IsInRole("Teacher"))
            {
                var ownTeacherId = await GetCurrentTeacherIdAsync();
                if (!ownTeacherId.HasValue) return Forbid();
                dto.TeacherId = ownTeacherId.Value;
            }
            var schedule = await _scheduleService.CreateAsync(dto);

            return CreatedAtAction(
                nameof(GetSchedule),
                new { id = schedule.Id },
                schedule);
        }

[HttpPut("{id}")]
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<IActionResult> UpdateSchedule(
            int id,
            ScheduleUpdateDto dto)
        {
            if (User.IsInRole("Teacher"))
            {
                var existing = await _scheduleService.GetByIdAsync(id);
                if (existing is null) return NotFound();
                if (!await IsOwnScheduleAsync(existing.TeacherId)) return Forbid();
                dto.TeacherId = existing.TeacherId;
            }
            var result = await _scheduleService.UpdateAsync(id, dto);

            if (!result)
            {
                return NotFound();
            }

            return NoContent();
        }

[HttpDelete("{id}")]
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<IActionResult> DeleteSchedule(int id)
        {
            if (User.IsInRole("Teacher"))
            {
                var existing = await _scheduleService.GetByIdAsync(id);
                if (existing is null) return NotFound();
                if (!await IsOwnScheduleAsync(existing.TeacherId)) return Forbid();
            }
            var result = await _scheduleService.DeleteAsync(id);

            if (!result)
            {
                return NotFound();
            }

            return NoContent();
        }

        private async Task<int?> GetCurrentTeacherIdAsync()
        {
            var userId = AuditContext.GetUserId(HttpContext);
            return userId.HasValue ? await _teacherScopeService.GetTeacherIdAsync(userId.Value) : null;
        }

        private async Task<bool> IsOwnScheduleAsync(int teacherId)
        {
            var ownTeacherId = await GetCurrentTeacherIdAsync();
            return ownTeacherId.HasValue && ownTeacherId.Value == teacherId;
        }
        [HttpPut("{id}/restore")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Restore(int id)
        {
            var result =
                await _softDeleteService.RestoreAsync<Schedule>(id);

            if (!result)
            {
                return NotFound(new
                {
                    message = "Không tìm thấy khóa học đã bị xóa."
                });
            }

            return Ok(new
            {
                message = "Khôi phục khóa học thành công."
            });
        }
    }
}

