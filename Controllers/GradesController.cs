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
    public class GradesController : ControllerBase
    {
        private readonly IGradeService _gradeService;
        private readonly ISoftDeleteService _softDeleteService;

        public GradesController(
            IGradeService gradeService, ISoftDeleteService softDeleteService)
        {
            _gradeService = gradeService;
            _softDeleteService = softDeleteService;
        }

        // GET: api/Grades
        // Admin + Teacher
        [HttpGet]
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<ActionResult<PagedResultDto<GradeDto>>> GetGrades(
    string? search,
    int? examId,
    int? studentId,
    decimal? minScore,
    decimal? maxScore,
    string? sortBy,
    bool sortDesc = false,
    int page = 1,
    int pageSize = 20)
        {
            var grades = await _gradeService.GetAllAsync(
                search,
                examId,
                studentId,
                minScore,
                maxScore,
                sortBy,
                sortDesc,
                page,
                pageSize);

            return Ok(grades);
        }

        // GET: api/Grades/1
        // Admin + Teacher
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<ActionResult<GradeDto>>
            GetGrade(int id)
        {
            var grade =
                await _gradeService.GetByIdAsync(id);

            if (grade == null)
            {
                return NotFound();
            }

            return Ok(grade);
        }

        // POST: api/Grades
        // Admin + Teacher
        [HttpPost]
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<ActionResult<GradeDto>>
            CreateGrade(GradeCreateDto dto)
        {
            var grade =
                await _gradeService.CreateAsync(dto);

            return CreatedAtAction(
                nameof(GetGrade),
                new { id = grade.Id },
                grade);
        }

        // PUT: api/Grades/1
        // Admin + Teacher
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<IActionResult>
            UpdateGrade(
                int id,
                GradeUpdateDto dto)
        {
            var result =
                await _gradeService.UpdateAsync(id, dto);

            if (!result)
            {
                return NotFound();
            }

            return NoContent();
        }

        // DELETE: api/Grades/1
        // Chỉ Admin
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult>
            DeleteGrade(int id)
        {
            var result =
                await _gradeService.DeleteAsync(id);

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