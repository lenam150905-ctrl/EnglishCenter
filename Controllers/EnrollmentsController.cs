using EnglishCenter.API.DTOs;
using EnglishCenter.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace EnglishCenter.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EnrollmentsController : ControllerBase
    {
        private readonly IEnrollmentService _enrollmentService;

        public EnrollmentsController(
            IEnrollmentService enrollmentService)
        {
            _enrollmentService = enrollmentService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<EnrollmentDto>>> GetEnrollments()
        {
            var enrollments =
                await _enrollmentService.GetAllAsync();

            return Ok(enrollments);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<EnrollmentDto>> GetEnrollment(
            int id)
        {
            var enrollment =
                await _enrollmentService.GetByIdAsync(id);

            if (enrollment == null)
            {
                return NotFound();
            }

            return Ok(enrollment);
        }

        [HttpPost]
        public async Task<ActionResult<EnrollmentDto>> CreateEnrollment(
            EnrollmentCreateDto dto)
        {
            var enrollment =
                await _enrollmentService.CreateAsync(dto);

            return CreatedAtAction(
                nameof(GetEnrollment),
                new { id = enrollment.Id },
                enrollment);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEnrollment(
            int id,
            EnrollmentUpdateDto dto)
        {
            var result =
                await _enrollmentService.UpdateAsync(id, dto);

            if (!result)
            {
                return NotFound();
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEnrollment(
            int id)
        {
            var result =
                await _enrollmentService.DeleteAsync(id);

            if (!result)
            {
                return NotFound();
            }

            return NoContent();
        }
    }
}