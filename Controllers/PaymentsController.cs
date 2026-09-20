using EnglishCenter.API.DTOs;
using EnglishCenter.API.Jobs;
using EnglishCenter.API.Middleware;
using EnglishCenter.API.Services;
using EnglishCenter.Application.Abstractions.Persistence;
using EnglishCenter.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnglishCenter.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin,Student")]
    public class PaymentsController : ControllerBase
    {
        private readonly IInvoiceRepository _invoiceRepository;
        private readonly IEnrollmentRepository _enrollmentRepository;
        private readonly IVNPayService _vnPayService;
        private readonly IAuditLogService _auditLogService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly INotificationService _notificationService;
        private readonly IBackgroundJobQueue _queue;
        private readonly IConfiguration _configuration;

        public PaymentsController(
            IInvoiceRepository invoiceRepository,
            IEnrollmentRepository enrollmentRepository,
            IVNPayService vnPayService,
            IAuditLogService auditLogService,
            IHttpContextAccessor httpContextAccessor,
            INotificationService notificationService,
            IBackgroundJobQueue queue,
            IConfiguration configuration)
        {
            _invoiceRepository = invoiceRepository;
            _enrollmentRepository = enrollmentRepository;
            _vnPayService = vnPayService;
            _auditLogService = auditLogService;
            _httpContextAccessor = httpContextAccessor;
            _notificationService = notificationService;
            _queue = queue;
            _configuration = configuration;
        }

        private int? userid => AuditContext.GetUserId(_httpContextAccessor.HttpContext!);
        private string? ipaddress => AuditContext.GetIPAddress(_httpContextAccessor.HttpContext!);

        // Tạo link thanh toán VNPAY
        [HttpPost("create-vnpay/{invoiceId}")]
        public async Task<IActionResult> CreateVNPayPayment(int invoiceId)
        {
            var invoice = await _invoiceRepository.GetByIdAsync(invoiceId);

            if (invoice == null)
            {
                return NotFound(new
                {
                    message = "Hóa đơn không tồn tại."
                });
            }

            if (User.IsInRole("Student") &&
                (!userid.HasValue || invoice.Student?.UserId != userid.Value))
            {
                return Forbid();
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

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

            var paymentUrl = _vnPayService.CreatePaymentUrl(
                invoice.Id,
                invoice.Amount,
                $"Thanh toan hoa don {invoice.Id}",
                ipAddress);

            await _auditLogService.CreateAsync(
                userid,
                "PAYMENT_CREATED",
                "Invoice",
                invoice.Id,
                $"Tạo link thanh toán VNPay cho Invoice ID {invoice.Id}, số tiền {invoice.Amount:0.00}",
                ipaddress);

            if (userid.HasValue)
            {
                await _notificationService.CreateAsync(
                    new NotificationCreateDto
                    {
                        UserId = userid.Value,
                        Title = "Tạo thanh toán",
                        Message = $"Bạn đã tạo link thanh toán cho hóa đơn #{invoice.Id}.",
                        Type = "PAYMENT"
                    });
            }

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
            var isValid = _vnPayService.ValidateResponse(Request.Query);

            if (!isValid)
            {
                return BadRequest(new
                {
                    message = "Chữ ký VNPAY không hợp lệ."
                });
            }

            var responseCode = Request.Query["vnp_ResponseCode"].FirstOrDefault();
            var transactionStatus = Request.Query["vnp_TransactionStatus"].FirstOrDefault();
            var txnRef = Request.Query["vnp_TxnRef"].FirstOrDefault();

            if (string.IsNullOrWhiteSpace(txnRef))
            {
                return BadRequest(new
                {
                    message = "Không tìm thấy mã giao dịch."
                });
            }

            // txnRef có dạng: InvoiceId_yyyyMMddHHmmss
            var invoiceIdText = txnRef.Split('_')[0];

            if (!int.TryParse(invoiceIdText, out var invoiceId))
            {
                return BadRequest(new
                {
                    message = "Mã hóa đơn không hợp lệ."
                });
            }

            var invoice = await _invoiceRepository.GetByIdAsync(invoiceId);

            if (invoice == null)
            {
                return NotFound(new
                {
                    message = "Không tìm thấy hóa đơn."
                });
            }

            var studentUserId = invoice.EnrollmentId.HasValue
                ? await _enrollmentRepository.GetStudentUserIdByEnrollmentIdAsync(invoice.EnrollmentId.Value)
                : null;

            // Thanh toán thành công
            if (responseCode == "00" && transactionStatus == "00")
            {
                await _invoiceRepository.UpdateStatusAsync(invoice.Id, "Paid");

                if (invoice.EnrollmentId.HasValue)
                {
                    await _enrollmentRepository.UpdateStatusAsync(invoice.EnrollmentId.Value, "Active");
                }

                await _auditLogService.CreateAsync(
                    userid,
                    "PAYMENT_SUCCESS",
                    "Invoice",
                    invoice.Id,
                    $"Thanh toán VNPay thành công Invoice ID {invoice.Id}, số tiền {invoice.Amount:0.00}",
                    ipaddress);

                if (studentUserId.HasValue)
                {
                    var job = new PaymentNotificationJob(
                        _notificationService,
                        studentUserId.Value,
                        "Thanh toán thành công",
                        $"Hóa đơn #{invoice.Id} đã được thanh toán thành công. Số tiền: {invoice.Amount:0.00}."
                    );

                    _queue.Enqueue(job);
                }

                return RedirectToPaymentResult("success", invoice.Id, "Thanh toán thành công.");
            }

            await _auditLogService.CreateAsync(
                userid,
                "PAYMENT_FAILED",
                "Invoice",
                invoice.Id,
                $"Thanh toán VNPay thất bại Invoice ID {invoice.Id}, ResponseCode: {responseCode}, TransactionStatus: {transactionStatus}",
                ipaddress);

            if (studentUserId.HasValue)
            {
                var job = new PaymentNotificationJob(
                    _notificationService,
                    studentUserId.Value,
                    "Thanh toán thất bại",
                    $"Thanh toán hóa đơn #{invoice.Id} không thành công."
                );

                _queue.Enqueue(job);
            }

            return RedirectToPaymentResult("failed", invoice.Id, "Thanh toán không thành công.");
        }

        private IActionResult RedirectToPaymentResult(string status, int invoiceId, string message)
        {
            var baseUrl = _configuration["Frontend:PaymentResultUrl"]
                ?? "http://127.0.0.1:5500/pages/payment/result.html";
            var separator = baseUrl.Contains('?') ? "&" : "?";
            return Redirect($"{baseUrl}{separator}status={Uri.EscapeDataString(status)}&invoiceId={invoiceId}&message={Uri.EscapeDataString(message)}");
        }
    }
}
