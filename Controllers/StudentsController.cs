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
    public class StudentsController : ControllerBase
    {
        private readonly IStudentService _studentService;
        private readonly ISoftDeleteService _softDeleteService;

        public StudentsController(
            IStudentService studentService, ISoftDeleteService softDeleteService)
        {
            _studentService = studentService;
            _softDeleteService = softDeleteService;
        }

        // GET: api/Students
        // Admin + Teacher
        [HttpGet]
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<ActionResult<PagedResultDto<StudentDto>>> GetStudents(
    string? search,
    DateTime? fromDateOfBirth,
    DateTime? toDateOfBirth,
    string? sortBy,
    bool sortDesc = false,
    int page = 1,
    int pageSize = 20)
        {
            var students = await _studentService.GetAllAsync(
                search,
                fromDateOfBirth,
                toDateOfBirth,
                sortBy,
                sortDesc,
                page,
                pageSize);

            return Ok(students);
        }

        // GET: api/Students/1
        // Admin + Teacher
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<ActionResult<StudentDto>>
            GetStudent(int id)
        {
            var student =
                await _studentService.GetByIdAsync(id);

            if (student == null)
            {
                return NotFound();
            }

            return Ok(student);
        }

        // POST: api/Students
        // Chỉ Admin
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<StudentDto>>
            CreateStudent(StudentCreateDto dto)
        {
            var student =
                await _studentService.CreateAsync(dto);

            return CreatedAtAction(
                nameof(GetStudent),
                new { id = student.Id },
                student);
        }

        // PUT: api/Students/1
        // Chỉ Admin
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult>
            UpdateStudent(
                int id,
                StudentUpdateDto dto)
        {
            var result =
                await _studentService.UpdateAsync(id, dto);

            if (!result)
            {
                return NotFound();
            }

            return NoContent();
        }

        // DELETE: api/Students/1
        // Chỉ Admin
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult>
            DeleteStudent(int id)
        {
            var result =
                await _studentService.DeleteAsync(id);

            if (!result)
            {
                return NotFound();
            }

            return NoContent();
        }
        [HttpPost("import-excel")]
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<IActionResult> ImportExcel(IFormFile file)
        {
            var result = await _studentService.ImportExcelAsync(file);

            return Ok(result);
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