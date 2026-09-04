using EnglishCenter.API.DTOs;
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

        public InvoicesController(
            IInvoiceService invoiceService)
        {
            _invoiceService = invoiceService;
        }

        // GET: api/Invoices
        // Chỉ Admin
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<InvoiceDto>>>
            GetInvoices()
        {
            var invoices =
                await _invoiceService.GetAllAsync();

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
    }
}