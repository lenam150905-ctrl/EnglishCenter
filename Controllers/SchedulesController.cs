using Microsoft.AspNetCore.Mvc;
using EnglishCenter.API.DTOs;
using EnglishCenter.API.Services;

namespace EnglishCenter.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SchedulesController: ControllerBase
    {
        private readonly IScheduleService _scheduleService;
        public SchedulesController(IScheduleService scheduleService)
        {
            _scheduleService = scheduleService;
        }
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ScheduleDto>>> GetSchedules()
        {
            var schedules = await _scheduleService.GetAllAsync();
            return Ok(schedules);
        }
        [HttpGet("{id}")]
        public async Task<ActionResult<ScheduleDto>> GetSchedule(int id)
        {
            var schedule = await _scheduleService.GetByIdAsync(id);
            if (schedule == null)
            {
                return NotFound();
            }
            return Ok(schedule);
        }
        [HttpPost]
        public async Task<ActionResult<ScheduleDto>> CreateSchedule(
            ScheduleCreateDto dto)
        {
            var schedule = await _scheduleService.CreateAsync(dto);
            return CreatedAtAction(
                nameof(GetSchedule),
                new { id = schedule.Id },
                schedule);
        }
        [HttpPut("{id}")]
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
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSchedule(int id)
        {
            var result = await _scheduleService.DeleteAsync(id);
            if (!result)
            {
                return NotFound();
            }
            return NoContent();
        }

    }
}
