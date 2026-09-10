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
    public class InvoicesController : ControllerBase
    {
        private readonly IInvoiceService _invoiceService;
        private readonly ISoftDeleteService _softDeleteService;

        public InvoicesController(
            IInvoiceService invoiceService, ISoftDeleteService softDeleteService)
        {
            _invoiceService = invoiceService;
            _softDeleteService = softDeleteService;
        }

        // GET: api/Invoices
        // Chỉ Admin
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<PagedResultDto<InvoiceDto>>> GetInvoices(
    string? search,
    int? studentId,
    int? enrollmentId,
    string? status,
    decimal? minAmount,
    decimal? maxAmount,
    string? sortBy,
    bool sortDesc = false,
    int page = 1,
    int pageSize = 20)
        {
            var invoices = await _invoiceService.GetAllAsync(
                search,
                studentId,
                enrollmentId,
                status,
                minAmount,
                maxAmount,
                sortBy,
                sortDesc,
                page,
                pageSize);

            return Ok(invoices);
        }

        // GET: api/Invoices/1
        // Chỉ Admin
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<InvoiceDto>>
            GetInvoice(int id)
        {
            var invoice =
                await _invoiceService.GetByIdAsync(id);

            if (invoice == null)
            {
                return NotFound();
            }

            return Ok(invoice);
        }

        // POST: api/Invoices
        // Chỉ Admin
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<InvoiceDto>>
            CreateInvoice(InvoiceCreateDto dto)
        {
            var invoice =
                await _invoiceService.CreateAsync(dto);

            return CreatedAtAction(
                nameof(GetInvoice),
                new { id = invoice.Id },
                invoice);
        }

        // PUT: api/Invoices/1
        // Chỉ Admin
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult>
            UpdateInvoice(
                int id,
                InvoiceUpdateDto dto)
        {
            var result =
                await _invoiceService.UpdateAsync(id, dto);

            if (!result)
            {
                return NotFound();
            }

            return NoContent();
        }

        // DELETE: api/Invoices/1
        // Chỉ Admin
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult>
            DeleteInvoice(int id)
        {
            var result =
                await _invoiceService.DeleteAsync(id);

            if (!result)
            {
                return NotFound();
            }

            return NoContent();
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
        [HttpPut("{id}/cancel")]
        [Authorize(Roles = "Admin,Student")]
        public async Task<IActionResult> Cancel(int id)
        {
            try
            {
                var result =
                    await _invoiceService.CancelAsync(id);

                if (!result)
                {
                    return NotFound(new
                    {
                        message = "Hóa đơn không tồn tại."
                    });
                }

                return Ok(new
                {
                    message = "Hủy hóa đơn thành công."
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }
    }
}