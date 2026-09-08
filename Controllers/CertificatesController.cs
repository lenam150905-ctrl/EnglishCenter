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
    public class CertificatesController : ControllerBase
    {
        private readonly ICertificateService _certificateService;
        private readonly ICertificatePdfService _certificatePdfService;
        private readonly ISoftDeleteService _softDeleteService;


        public CertificatesController(
    ICertificateService certificateService,
    ICertificatePdfService certificatePdfService,
    ISoftDeleteService softDeleteService)
        {
            _certificateService = certificateService;
            _certificatePdfService = certificatePdfService;
            _softDeleteService = softDeleteService;
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
        // Chỉ Admin
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
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
        [HttpPost("{id}/export-pdf")]
        public async Task<IActionResult> ExportPdf(int id)
        {
            try
            {
                var path = await _certificatePdfService
                    .GenerateCertificatePdfAsync(id);

                return Ok(new
                {
                    message = "Xuất chứng chỉ PDF thành công.",
                    pdfFilePath = path
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    statusCode = 400,
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