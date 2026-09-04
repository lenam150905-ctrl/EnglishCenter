using EnglishCenter.API.DTOs;
using EnglishCenter.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnglishCenter.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ExamsController : ControllerBase
    {
        private readonly IExamService _examService;

        public ExamsController(
            IExamService examService)
        {
            _examService = examService;
        }

        // GET: api/Exams
        // Admin + Teacher + Student
        [HttpGet]
        [Authorize(Roles = "Admin,Teacher,Student")]
        public async Task<ActionResult<IEnumerable<ExamDto>>>
            GetExams()
        {
            var exams =
                await _examService.GetAllAsync();

            return Ok(exams);
        }

        // GET: api/Exams/1
        // Admin + Teacher + Student
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Teacher,Student")]
        public async Task<ActionResult<ExamDto>>
            GetExam(int id)
        {
            var exam =
                await _examService.GetByIdAsync(id);

            if (exam == null)
            {
                return NotFound();
            }

            return Ok(exam);
        }

        // POST: api/Exams
        // Admin + Teacher
        [HttpPost]
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<ActionResult<ExamDto>>
            CreateExam(ExamCreateDto dto)
        {
            var exam =
                await _examService.CreateAsync(dto);

            return CreatedAtAction(
                nameof(GetExam),
                new { id = exam.Id },
                exam);
        }

        // PUT: api/Exams/1
        // Admin + Teacher
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<IActionResult>
            UpdateExam(
                int id,
                ExamUpdateDto dto)
        {
            var result =
                await _examService.UpdateAsync(id, dto);

            if (!result)
            {
                return NotFound();
            }

            return NoContent();
        }

        // DELETE: api/Exams/1
        // Chỉ Admin
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult>
            DeleteExam(int id)
        {
            var result =
                await _examService.DeleteAsync(id);

            if (!result)
            {
                return NotFound();
            }

            return NoContent();
        }
    }
}