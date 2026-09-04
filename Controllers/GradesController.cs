using EnglishCenter.API.DTOs;
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

        public GradesController(
            IGradeService gradeService)
        {
            _gradeService = gradeService;
        }

        // GET: api/Grades
        // Admin + Teacher
        [HttpGet]
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<ActionResult<IEnumerable<GradeDto>>>
            GetGrades()
        {
            var grades =
                await _gradeService.GetAllAsync();

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
    }
}