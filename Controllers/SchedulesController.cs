using EnglishCenter.API.DTOs;
using EnglishCenter.API.Models;
using EnglishCenter.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnglishCenter.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SchedulesController : ControllerBase
    {
        private readonly IScheduleService _scheduleService;
        private readonly ISoftDeleteService _softDeleteService;

        public SchedulesController(IScheduleService scheduleService, ISoftDeleteService softDeleteService)
        {
            _scheduleService = scheduleService;
            _softDeleteService = softDeleteService;
        }

        // GET: api/Schedules
        // Admin + Teacher + Student
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

        // GET: api/Schedules/1
        // Admin + Teacher + Student
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Teacher,Student")]
        public async Task<ActionResult<ScheduleDto>> GetSchedule(int id)
        {
            var schedule = await _scheduleService.GetByIdAsync(id);

            if (schedule == null)
            {
                return NotFound();
            }

            return Ok(schedule);
        }

        // POST: api/Schedules
        // Admin + Teacher
        [HttpPost]
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<ActionResult<ScheduleDto>> CreateSchedule(
            ScheduleCreateDto dto)
        {
            var schedule = await _scheduleService.CreateAsync(dto);

            return CreatedAtAction(
                nameof(GetSchedule),
                new { id = schedule.Id },
                schedule);
        }

        // PUT: api/Schedules/1
        // Admin + Teacher
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<IActionResult> UpdateSchedule(
            int id,
            ScheduleUpdateDto dto)
        {
            var result = await _scheduleService.UpdateAsync(id, dto);

            if (!result)
            {
                return NotFound();
            }

            return NoContent();
        }

        // DELETE: api/Schedules/1
        // Admin + Teacher
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<IActionResult> DeleteSchedule(int id)
        {
            var result = await _scheduleService.DeleteAsync(id);

            if (!result)
            {
                return NotFound();
            }

            return NoContent();
        }
        [HttpPut("{id}/restore")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Restore(int id)
        {
            var result =
                await _softDeleteService.RestoreAsync<Course>(id);

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
