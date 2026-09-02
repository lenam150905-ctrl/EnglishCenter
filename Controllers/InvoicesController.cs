using EnglishCenter.API.DTOs;
using EnglishCenter.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace EnglishCenter.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InvoicesController : ControllerBase
    {
        private readonly IInvoiceService _invoiceService;

        public InvoicesController(
            IInvoiceService invoiceService)
        {
            _invoiceService = invoiceService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<InvoiceDto>>>
            GetInvoices()
        {
            var invoices =
                await _invoiceService.GetAllAsync();

            return Ok(invoices);
        }

        [HttpGet("{id}")]
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

        [HttpPost]
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

        [HttpPut("{id}")]
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

        [HttpDelete("{id}")]
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