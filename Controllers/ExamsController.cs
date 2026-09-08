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
    public class ExamsController : ControllerBase
    {
        private readonly IExamService _examService;
        private readonly ISoftDeleteService _softDeleteService;

        public ExamsController(
            IExamService examService, ISoftDeleteService softDeleteService)
        {
            _examService = examService;
            _softDeleteService = softDeleteService;
        }

        // GET: api/Exams
        // Admin + Teacher + Student
        [HttpGet]
        [Authorize(Roles = "Admin,Teacher,Student")]
        public async Task<ActionResult<PagedResultDto<ExamDto>>> GetExams(
    string? search,
    int? courseId,
    string? examType,
    string? sortBy,
    bool sortDesc = false,
    int page = 1,
    int pageSize = 20)
        {
            var exams = await _examService.GetAllAsync(
                search,
                courseId,
                examType,
                sortBy,
                sortDesc,
                page,
                pageSize);

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
        // POST: api/Exams/1/start
        // Chỉ Student
        [HttpGet("{examId}/can-start")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> CanStartExam(
    int examId,
    int studentId)
        {
            try
            {
                var result = await _examService
                    .CanStartExamAsync(studentId, examId);

                return Ok(new
                {
                    canStart = result
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    canStart = false,
                    message = ex.Message
                });
            }
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