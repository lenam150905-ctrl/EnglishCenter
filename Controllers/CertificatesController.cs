using EnglishCenter.API.DTOs;
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

        public CertificatesController(
            ICertificateService certificateService)
        {
            _certificateService = certificateService;
        }

        // GET: api/Certificates
        // Admin + Teacher + Student
        [HttpGet]
        [Authorize(Roles = "Admin,Teacher,Student")]
        public async Task<ActionResult<IEnumerable<CertificateDto>>>
            GetCertificates()
        {
            var certificates =
                await _certificateService.GetAllAsync();

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
    }
}