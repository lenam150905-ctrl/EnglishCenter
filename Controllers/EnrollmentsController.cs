using EnglishCenter.API.DTOs;
using EnglishCenter.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnglishCenter.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class EnrollmentsController : ControllerBase
    {
        private readonly IEnrollmentService _enrollmentService;

        public EnrollmentsController(
            IEnrollmentService enrollmentService)
        {
            _enrollmentService = enrollmentService;
        }

        // GET: api/Enrollments
        // Admin + Teacher
        [HttpGet]
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<ActionResult<IEnumerable<EnrollmentDto>>> GetEnrollments()
        {
            var enrollments =
                await _enrollmentService.GetAllAsync();

            return Ok(enrollments);
        }

        // GET: api/Enrollments/1
        // Admin + Teacher
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Teacher")]
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

        // POST: api/Enrollments
        // Admin + Student
        [HttpPost]
        [Authorize(Roles = "Admin,Student")]
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

        // PUT: api/Enrollments/1
        // Chỉ Admin
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
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

        // DELETE: api/Enrollments/1
        // Chỉ Admin
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
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