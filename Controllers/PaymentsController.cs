using EnglishCenter.API.Data;
using EnglishCenter.API.Services;
using EnglishCenter.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnglishCenter.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin,Student")]
    public class PaymentsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IVNPayService _vnPayService;

        public PaymentsController(
            ApplicationDbContext context,
            IVNPayService vnPayService)
        {
            _context = context;
            _vnPayService = vnPayService;
        }

        // Tạo link thanh toán VNPAY
        [HttpPost("create-vnpay/{invoiceId}")]
        public async Task<IActionResult> CreateVNPayPayment(
            int invoiceId)
        {
            var invoice = await _context.Invoices
                .FirstOrDefaultAsync(i => i.Id == invoiceId);

            if (invoice == null)
            {
                return NotFound(new
                {
                    message = "Hóa đơn không tồn tại."
                });
            }

            // Chỉ cho thanh toán hóa đơn chưa thanh toán
            if (invoice.Status != "Unpaid")
            {
                return BadRequest(new
                {
                    message = "Hóa đơn không ở trạng thái chưa thanh toán."
                });
            }

            if (invoice.Amount <= 0)
            {
                return BadRequest(new
                {
                    message = "Số tiền hóa đơn không hợp lệ."
                });
            }

            var ipAddress =
                HttpContext.Connection.RemoteIpAddress?.ToString()
                ?? "127.0.0.1";

            var paymentUrl =
                _vnPayService.CreatePaymentUrl(
                    invoice.Id,
                    invoice.Amount,
                    $"Thanh toan hoa don {invoice.Id}",
                    ipAddress);

            return Ok(new
            {
                message = "Tạo link thanh toán thành công.",
                invoiceId = invoice.Id,
                amount = invoice.Amount,
                paymentUrl = paymentUrl
            });
        }

        // VNPAY trả kết quả về đây
        [AllowAnonymous]
        [HttpGet("vnpay-return")]
        public async Task<IActionResult> VNPayReturn()
        {
            var isValid =
                _vnPayService.ValidateResponse(
                    Request.Query);

            if (!isValid)
            {
                return BadRequest(new
                {
                    message = "Chữ ký VNPAY không hợp lệ."
                });
            }

            var responseCode =
                Request.Query["vnp_ResponseCode"]
                    .FirstOrDefault();

            var transactionStatus =
                Request.Query["vnp_TransactionStatus"]
                    .FirstOrDefault();

            var txnRef =
                Request.Query["vnp_TxnRef"]
                    .FirstOrDefault();

            if (string.IsNullOrWhiteSpace(txnRef))
            {
                return BadRequest(new
                {
                    message = "Không tìm thấy mã giao dịch."
                });
            }

            // txnRef có dạng: InvoiceId_yyyyMMddHHmmss
            var invoiceIdText =
                txnRef.Split('_')[0];

            if (!int.TryParse(
                    invoiceIdText,
                    out var invoiceId))
            {
                return BadRequest(new
                {
                    message = "Mã hóa đơn không hợp lệ."
                });
            }

            var invoice = await _context.Invoices
                .FirstOrDefaultAsync(
                    i => i.Id == invoiceId);

            if (invoice == null)
            {
                return NotFound(new
                {
                    message = "Không tìm thấy hóa đơn."
                });
            }

            // Thanh toán thành công
            if (responseCode == "00" &&
     transactionStatus == "00")
            {
                invoice.Status = "Paid";

                if (invoice.EnrollmentId.HasValue)
                {
                    var enrollment = await _context.Enrollments
                        .FirstOrDefaultAsync(e =>
                            e.Id == invoice.EnrollmentId.Value);

                    if (enrollment != null)
                    {
                        enrollment.Status = "Active";
                    }
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Thanh toán thành công.",
                    invoiceId = invoice.Id,
                    status = invoice.Status
                });
            }
        

            return BadRequest(new
            {
                message = "Thanh toán không thành công.",
                responseCode = responseCode,
                transactionStatus = transactionStatus
            });
        }
    }
}