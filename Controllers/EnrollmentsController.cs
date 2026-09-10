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
    public class EnrollmentsController : ControllerBase
    {
        private readonly IEnrollmentService _enrollmentService;
        private readonly ISoftDeleteService _softDeleteService;

        public EnrollmentsController(
            IEnrollmentService enrollmentService, ISoftDeleteService softDeleteService)
        {
            _enrollmentService = enrollmentService;
            _softDeleteService = softDeleteService;
        }

        // GET: api/Enrollments
        // Admin + Teacher
        [HttpGet]
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<ActionResult<PagedResultDto<EnrollmentDto>>> GetEnrollments(
    string? search,
    int? studentId,
    int? courseId,
    string? status,
    string? sortBy,
    bool sortDesc = false,
    int page = 1,
    int pageSize = 20)
        {
            var enrollments = await _enrollmentService.GetAllAsync(
                search,
                studentId,
                courseId,
                status,
                sortBy,
                sortDesc,
                page,
                pageSize);

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
        [Authorize(Roles = "Admin,Student")]
        [HttpPut("{id}/cancel")]
        public async Task<IActionResult> Cancel(int id)
        {
            var result = await _enrollmentService.CancelAsync(id);

            if (!result)
            {
                return NotFound(new
                {
                    message = "Không tìm thấy đăng ký khóa học."
                });
            }

            return Ok(new
            {
                message = "Hủy đăng ký khóa học thành công."
            });
        }
    }
}