using EnglishCenter.API.DTOs;
using EnglishCenter.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace EnglishCenter.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CertificatesController : ControllerBase
    {
        private readonly ICertificateService _certificateService;

        public CertificatesController(
            ICertificateService certificateService)
        {
            _certificateService = certificateService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<CertificateDto>>>
            GetCertificates()
        {
            var certificates =
                await _certificateService.GetAllAsync();

            return Ok(certificates);
        }

        [HttpGet("{id}")]
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

        [HttpPost]
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

        [HttpPut("{id}")]
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

        [HttpDelete("{id}")]
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