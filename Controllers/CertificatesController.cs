using EnglishCenter.API.DTOs;
using EnglishCenter.API.Jobs;
using EnglishCenter.API.Middleware;
using EnglishCenter.API.Models;
using EnglishCenter.API.Services;
using EnglishCenter.Application.Abstractions.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnglishCenter.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CertificatesController : ControllerBase
    {
        private readonly ICertificateService _certificateService;
        private readonly ICertificatePdfService _certificatePdfService;
        private readonly ISoftDeleteService _softDeleteService;
        private readonly IBackgroundJobQueue _queue;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IStudentRepository _studentRepository;
        private readonly IWebHostEnvironment _environment;

        public CertificatesController(
            ICertificateService certificateService,
            ICertificatePdfService certificatePdfService,
            ISoftDeleteService softDeleteService,
            IBackgroundJobQueue queue,
            IServiceScopeFactory scopeFactory,
            IStudentRepository studentRepository,
            IWebHostEnvironment environment)
        {
            _certificateService = certificateService;
            _certificatePdfService = certificatePdfService;
            _softDeleteService = softDeleteService;
            _queue = queue;
            _scopeFactory = scopeFactory;
            _studentRepository = studentRepository;
            _environment = environment;
        }

        [HttpGet("mine")]
        [Authorize(Roles = "Student")]
        public async Task<ActionResult<PagedResultDto<CertificateDto>>> GetMyCertificates(
            string? search,
            int? courseId,
            string? sortBy,
            bool sortDesc = false,
            int page = 1,
            int pageSize = 20)
        {
            var userId = AuditContext.GetUserId(HttpContext);
            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            var student = await _studentRepository.GetByUserIdAsync(userId.Value);
            if (student == null)
            {
                return NotFound(new { message = "Tài khoản chưa liên kết với hồ sơ học viên." });
            }

            return Ok(await _certificateService.GetAllAsync(
                search, student.Id, courseId, sortBy, sortDesc, page, pageSize));
        }

        // GET: api/Certificates
        // Admin + Teacher + Student
        [HttpGet]
        [Authorize(Roles = "Admin,Teacher,Student")]
        public async Task<ActionResult<PagedResultDto<CertificateDto>>> GetCertificates(
            string? search,
            int? studentId,
            int? courseId,
            string? sortBy,
            bool sortDesc = false,
            int page = 1,
            int pageSize = 20)
        {
            if (User.IsInRole("Student"))
            {
                var userId = AuditContext.GetUserId(HttpContext);
                var student = userId.HasValue
                    ? await _studentRepository.GetByUserIdAsync(userId.Value)
                    : null;

                if (student == null)
                {
                    return NotFound(new { message = "Tài khoản chưa liên kết với hồ sơ học viên." });
                }

                // Student không được truyền StudentId của người khác.
                studentId = student.Id;
            }

            var certificates = await _certificateService.GetAllAsync(
                search,
                studentId,
                courseId,
                sortBy,
                sortDesc,
                page,
                pageSize);

            return Ok(certificates);
        }

        // GET: api/Certificates/1
        // Admin + Teacher + Student
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Teacher,Student")]
        public async Task<ActionResult<CertificateDto>>
            GetCertificate(int id)
        {
            var certificate =
                await _certificateService.GetByIdAsync(id);

            if (certificate == null)
            {
                return NotFound();
            }

            if (User.IsInRole("Student"))
            {
                var userId = AuditContext.GetUserId(HttpContext);
                var student = userId.HasValue
                    ? await _studentRepository.GetByUserIdAsync(userId.Value)
                    : null;

                if (student == null || certificate.StudentId != student.Id)
                {
                    return Forbid();
                }
            }

            return Ok(certificate);
        }

        // POST: api/Certificates
        // Admin + Teacher
        [HttpPost]
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<ActionResult<CertificateDto>>
            CreateCertificate(
                CertificateCreateDto dto)
        {
            var certificate =
                await _certificateService.CreateAsync(dto);

            return CreatedAtAction(
                nameof(GetCertificate),
                new { id = certificate.Id },
                certificate);
        }

        // PUT: api/Certificates/1
        // Admin + Teacher
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<IActionResult>
            UpdateCertificate(
                int id,
                CertificateUpdateDto dto)
        {
            var result =
                await _certificateService
                    .UpdateAsync(id, dto);

            if (!result)
            {
                return NotFound();
            }

            return NoContent();
        }

        // DELETE: api/Certificates/1
        // Admin + Teacher
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<IActionResult>
            DeleteCertificate(int id)
        {
            var result =
                await _certificateService
                    .DeleteAsync(id);

            if (!result)
            {
                return NotFound();
            }

            return NoContent();
        }

        // ==========================================
        // EXPORT PDF
        // Admin + Teacher + Student
        // ==========================================

        [Authorize(Roles = "Admin,Teacher,Student")]
        [HttpGet("{id}/download")]
        public async Task<IActionResult> DownloadPdf(int id)
        {
            var userId = AuditContext.GetUserId(HttpContext);
            if (!userId.HasValue) return Unauthorized();

            var certificate = await _certificateService.GetByIdAsync(id);
            if (certificate == null) return NotFound(new { message = "Không tìm thấy chứng chỉ." });

            if (User.IsInRole("Student"))
            {
                var student = await _studentRepository.GetByUserIdAsync(userId.Value);
                if (student == null || certificate.StudentId != student.Id) return Forbid();
            }

            var relativePath = certificate.PdfFilePath;
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                relativePath = await _certificatePdfService.GenerateCertificatePdfAsync(id, userId, AuditContext.GetIPAddress(HttpContext));
            }

            var filePath = Path.Combine(_environment.WebRootPath, relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (!System.IO.File.Exists(filePath))
            {
                relativePath = await _certificatePdfService.GenerateCertificatePdfAsync(id, userId, AuditContext.GetIPAddress(HttpContext));
                filePath = Path.Combine(_environment.WebRootPath, relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            }

            return PhysicalFile(filePath, "application/pdf", $"{certificate.CertificateCode}.pdf");
        }

        [HttpPost("{id}/export-pdf")]
        public async Task<IActionResult> ExportPdf(int id)
        {
            // Kiểm tra UserId hiện tại
            var userId = AuditContext.GetUserId(
                HttpContext);

            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            // Lấy IP
            var ipAddress = AuditContext.GetIPAddress(
                HttpContext);

            // Kiểm tra Certificate có tồn tại
            var certificate =
                await _certificateService.GetByIdAsync(id);

            if (certificate == null)
            {
                return NotFound(new
                {
                    message = "Không tìm thấy chứng chỉ."
                });
            }

            if (User.IsInRole("Student"))
            {
                var student = await _studentRepository.GetByUserIdAsync(userId.Value);
                if (student == null || certificate.StudentId != student.Id)
                {
                    return Forbid();
                }
            }

            // Tạo Background Job
            var job = new CertificatePdfJob(
                _scopeFactory,
                id,
                userId,
                ipAddress);

            // Đưa Job vào Queue
            _queue.Enqueue(job);

            return Accepted(new
            {
                message =
                    "Yêu cầu xuất PDF đã được đưa vào hàng đợi.",
                certificateId = id
            });
        }

        // ==========================================
        // RESTORE
        // Chỉ Admin
        // ==========================================

        [HttpPut("{id}/restore")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Restore(int id)
        {
            var result =
                await _softDeleteService
                    .RestoreAsync<Certificate>(id);

            if (!result)
            {
                return NotFound(new
                {
                    message =
                        "Không tìm thấy chứng chỉ đã bị xóa."
                });
            }

            return Ok(new
            {
                message =
                    "Khôi phục chứng chỉ thành công."
            });
        }
    }
}
