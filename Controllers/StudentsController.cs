using EnglishCenter.API.DTOs;
using EnglishCenter.API.Jobs;
using EnglishCenter.API.Middleware;
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
        private readonly IBackgroundJobQueue _queue;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly EnglishCenter.Application.Abstractions.Persistence.IStudentRepository _studentRepository;

        public StudentsController(
            IStudentService studentService, ISoftDeleteService softDeleteService, IBackgroundJobQueue queue,
    IServiceScopeFactory scopeFactory,
    EnglishCenter.Application.Abstractions.Persistence.IStudentRepository studentRepository)
        {
            _studentService = studentService;
            _softDeleteService = softDeleteService;
            _queue = queue;
            _scopeFactory = scopeFactory;
            _studentRepository = studentRepository;
        }

        [HttpGet("me")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> GetMyProfile()
        {
            var userId = AuditContext.GetUserId(HttpContext);
            var student = userId.HasValue
                ? await _studentRepository.GetByUserIdAsync(userId.Value)
                : null;

            return student == null
                ? NotFound(new { message = "Tài khoản chưa có hồ sơ học viên." })
                : Ok(new { student.Id, student.FullName, student.Email, student.Phone, student.Address });
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
        [Authorize(Roles = "Admin,Teacher")]
        [HttpPost("import-excel")]
        public async Task<IActionResult> ImportExcel(
     IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new
                {
                    message = "Vui lòng chọn file Excel."
                });
            }

            var userId =
                AuditContext.GetUserId(HttpContext);

            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            // ==========================================
            // KIỂM TRA FILE NGAY LẬP TỨC
            // ==========================================

            var validation =
                await _studentService
                    .ValidateImportExcelAsync(file);

            // ==========================================
            // CÓ LỖI → TRẢ LỖI NGAY
            // KHÔNG ĐƯA VÀO QUEUE
            // ==========================================

            if (validation.Errors.Count > 0)
            {
                return BadRequest(new
                {
                    message =
                        "File Excel có lỗi, chưa đưa vào hàng đợi.",

                    successRows =
                        validation.SuccessRows,

                    errorRows =
                        validation.Errors.Count,

                    errors =
                        validation.Errors
                });
            }

            // ==========================================
            // FILE HỢP LỆ → LƯU FILE TẠM
            // ==========================================

            var ipAddress =
                HttpContext.Connection.RemoteIpAddress?
                    .ToString();

            var folder = Path.Combine(
                Path.GetTempPath(),
                "EnglishCenter",
                "StudentImport");

            Directory.CreateDirectory(folder);

            var fileName =
                $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";

            var filePath =
                Path.Combine(folder, fileName);

            await using (var stream =
                new FileStream(
                    filePath,
                    FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // ==========================================
            // ĐƯA VÀO BACKGROUND JOB
            // ==========================================

            var job =
                new ImportStudentJob(
                    _scopeFactory,
                    filePath,
                    userId.Value,
                    ipAddress);

            _queue.Enqueue(job);

            return Accepted(new
            {
                message =
                    "File hợp lệ và đã được đưa vào hàng đợi.",

                successRows =
                    validation.SuccessRows
            });
        }
    }
}
