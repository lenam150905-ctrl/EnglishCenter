using EnglishCenter.API.DTOs;
using EnglishCenter.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace EnglishCenter.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ExamsController : ControllerBase
    {
        private readonly IExamService _examService;

        public ExamsController(
            IExamService examService)
        {
            _examService = examService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ExamDto>>>
            GetExams()
        {
            var exams =
                await _examService.GetAllAsync();

            return Ok(exams);
        }

        [HttpGet("{id}")]
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

        [HttpPost]
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

        [HttpPut("{id}")]
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

        [HttpDelete("{id}")]
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