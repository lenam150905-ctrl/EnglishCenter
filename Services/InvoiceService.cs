using EnglishCenter.API.DTOs;
using EnglishCenter.API.Middleware;
using EnglishCenter.API.Models;
using EnglishCenter.Application.Abstractions.Persistence;

namespace EnglishCenter.API.Services
{
    public class InvoiceService : IInvoiceService
    {
        private readonly IInvoiceRepository _invoiceRepository;
        private readonly IStudentRepository _studentRepository;
        private readonly IEnrollmentRepository _enrollmentRepository;
        private readonly IAuditLogService _auditLogService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly INotificationService _notificationService;

        public InvoiceService(
            IInvoiceRepository invoiceRepository,
            IStudentRepository studentRepository,
            IEnrollmentRepository enrollmentRepository,
            IAuditLogService auditLogService,
            INotificationService notificationService,
            IHttpContextAccessor httpContextAccessor)
        {
            _invoiceRepository = invoiceRepository;
            _studentRepository = studentRepository;
            _enrollmentRepository = enrollmentRepository;
            _auditLogService = auditLogService;
            _notificationService = notificationService;
            _httpContextAccessor = httpContextAccessor;
        }

        private int? userid => AuditContext.GetUserId(_httpContextAccessor.HttpContext!);
        private string? ipaddress => AuditContext.GetIPAddress(_httpContextAccessor.HttpContext!);

        public async Task<PagedResultDto<InvoiceDto>> GetAllAsync(
            string? search,
            int? studentId,
            int? enrollmentId,
            string? status,
            decimal? minAmount,
            decimal? maxAmount,
            string? sortBy,
            bool sortDesc,
            int page,
            int pageSize)
        {
            var (invoices, totalItems) = await _invoiceRepository.GetAllAsync(
                search, studentId, enrollmentId, status, minAmount, maxAmount, sortBy, sortDesc, page, pageSize);

            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 20 : pageSize;
            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            return new PagedResultDto<InvoiceDto>
            {
                Data = invoices.Select(i => new InvoiceDto
                {
                    Id = i.Id,
                    StudentId = i.StudentId,
                    StudentName = i.Student?.FullName ?? string.Empty,
                    EnrollmentId = i.EnrollmentId,
                    CourseName = i.Enrollment?.Course?.CourseName,
                    Amount = i.Amount,
                    InvoiceDate = i.InvoiceDate,
                    Status = i.Status
                }).ToList(),
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages
            };
        }

        public async Task<InvoiceDto?> GetByIdAsync(int id)
        {
            var invoice = await _invoiceRepository.GetByIdAsync(id);
            if (invoice == null)
            {
                return null;
            }

            return new InvoiceDto
            {
                Id = invoice.Id,
                StudentId = invoice.StudentId,
                StudentName = invoice.Student?.FullName ?? string.Empty,
                EnrollmentId = invoice.EnrollmentId,
                CourseName = invoice.Enrollment?.Course?.CourseName,
                Amount = invoice.Amount,
                InvoiceDate = invoice.InvoiceDate,
                Status = invoice.Status
            };
        }

        public async Task<InvoiceDto> CreateAsync(InvoiceCreateDto dto)
        {
            var student = await _studentRepository.GetByIdAsync(dto.StudentId);
            if (student == null)
            {
                throw new ArgumentException("Student không tồn tại.");
            }

            if (dto.EnrollmentId.HasValue)
            {
                var enrollment = await _enrollmentRepository.GetByIdAsync(dto.EnrollmentId.Value);
                if (enrollment == null)
                {
                    throw new ArgumentException("Enrollment không tồn tại.");
                }

                if (enrollment.StudentId != dto.StudentId)
                {
                    throw new ArgumentException("Enrollment không thuộc Student này.");
                }

                var existed = await _invoiceRepository.ExistsByEnrollmentAsync(dto.EnrollmentId.Value);
                if (existed)
                {
                    throw new ArgumentException("Enrollment này đã có hóa đơn.");
                }
            }

            if (dto.Amount <= 0)
            {
                throw new ArgumentException("Số tiền phải lớn hơn 0.");
            }

            if (dto.InvoiceDate > DateTime.Now)
            {
                throw new ArgumentException("Ngày lập hóa đơn không được lớn hơn ngày hiện tại.");
            }

            if (dto.Status != "Unpaid" && dto.Status != "Paid" && dto.Status != "Cancelled")
            {
                throw new ArgumentException("Status không hợp lệ.");
            }

            var invoice = new Invoice
            {
                StudentId = dto.StudentId,
                EnrollmentId = dto.EnrollmentId,
                Amount = dto.Amount,
                InvoiceDate = dto.InvoiceDate,
                Status = dto.Status
            };

            await _invoiceRepository.CreateAsync(invoice);

            await _auditLogService.CreateAsync(
                userid,
                "CREATE",
                "Invoice",
                invoice.Id,
                $"Tạo hóa đơn cho Student ID {invoice.StudentId}, số tiền {invoice.Amount:0.00}, Status: {invoice.Status}",
                ipaddress);

            if (userid.HasValue)
            {
                await _notificationService.CreateAsync(new NotificationCreateDto
                {
                    UserId = userid.Value,
                    Title = "Tạo hóa đơn",
                    Message = $"Bạn đã tạo hóa đơn {invoice.Id} với số tiền {invoice.Amount:0.00}.",
                    Type = "INVOICE"
                });
            }

            if (student.UserId.HasValue && student.UserId != userid)
            {
                await _notificationService.CreateAsync(new NotificationCreateDto
                {
                    UserId = student.UserId.Value,
                    Title = "Có hóa đơn mới",
                    Message = $"Bạn có hóa đơn mới #{invoice.Id} với số tiền {invoice.Amount:0.00}.",
                    Type = "INVOICE"
                });
            }

            return new InvoiceDto
            {
                Id = invoice.Id,
                StudentId = invoice.StudentId,
                StudentName = student.FullName,
                EnrollmentId = invoice.EnrollmentId,
                Amount = invoice.Amount,
                InvoiceDate = invoice.InvoiceDate,
                Status = invoice.Status
            };
        }

        public async Task<bool> UpdateAsync(int id, InvoiceUpdateDto dto)
        {
            var invoice = await _invoiceRepository.GetByIdAsync(id);
            if (invoice == null)
            {
                return false;
            }

            var student = await _studentRepository.GetByIdAsync(dto.StudentId);
            if (student == null)
            {
                throw new ArgumentException("Student không tồn tại.");
            }

            if (dto.EnrollmentId.HasValue)
            {
                var enrollment = await _enrollmentRepository.GetByIdAsync(dto.EnrollmentId.Value);
                if (enrollment == null)
                {
                    throw new ArgumentException("Enrollment không tồn tại.");
                }

                if (enrollment.StudentId != dto.StudentId)
                {
                    throw new ArgumentException("Enrollment không thuộc Student này.");
                }
            }

            if (dto.Amount <= 0)
            {
                throw new ArgumentException("Số tiền phải lớn hơn 0.");
            }

            if (dto.InvoiceDate > DateTime.Now)
            {
                throw new ArgumentException("Ngày lập hóa đơn không được lớn hơn ngày hiện tại.");
            }

            if (dto.Status != "Unpaid" && dto.Status != "Paid" && dto.Status != "Cancelled")
            {
                throw new ArgumentException("Status không hợp lệ.");
            }

            invoice.StudentId = dto.StudentId;
            invoice.EnrollmentId = dto.EnrollmentId;
            invoice.Amount = dto.Amount;
            invoice.InvoiceDate = dto.InvoiceDate;
            invoice.Status = dto.Status;

            await _invoiceRepository.UpdateAsync(invoice);

            if (dto.Status == "Paid" && dto.EnrollmentId.HasValue)
            {
                await _enrollmentRepository.UpdateStatusAsync(dto.EnrollmentId.Value, "Active");
            }

            await _auditLogService.CreateAsync(
                userid,
                "UPDATE",
                "Invoice",
                invoice.Id,
                $"Cập nhật hóa đơn ID {invoice.Id}, số tiền {invoice.Amount:0.00}, Status: {invoice.Status}",
                ipaddress);

            if (userid.HasValue)
            {
                await _notificationService.CreateAsync(new NotificationCreateDto
                {
                    UserId = userid.Value,
                    Title = "Cập nhật hóa đơn",
                    Message = $"Bạn đã cập nhật hóa đơn #{invoice.Id}.",
                    Type = "INVOICE"
                });
            }

            if (student.UserId.HasValue && student.UserId != userid)
            {
                await _notificationService.CreateAsync(new NotificationCreateDto
                {
                    UserId = student.UserId.Value,
                    Title = "Hóa đơn được cập nhật",
                    Message = $"Hóa đơn #{invoice.Id} của bạn đã được cập nhật sang trạng thái: {invoice.Status}.",
                    Type = "INVOICE"
                });
            }

            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var invoice = await _invoiceRepository.GetByIdAsync(id);
            if (invoice == null)
            {
                return false;
            }

            var studentId = invoice.StudentId;
            var amount = invoice.Amount;
            var studentUserId = invoice.Student?.UserId;

            await _invoiceRepository.SoftDeleteAsync(id);

            await _auditLogService.CreateAsync(
                userid,
                "DELETE",
                "Invoice",
                id,
                $"Xóa hóa đơn ID {id} của Student ID {studentId}, số tiền {amount:0.00}",
                ipaddress);

            if (userid.HasValue)
            {
                await _notificationService.CreateAsync(new NotificationCreateDto
                {
                    UserId = userid.Value,
                    Title = "Xóa hóa đơn",
                    Message = $"Bạn đã xóa hóa đơn #{id}.",
                    Type = "INVOICE"
                });
            }

            if (studentUserId.HasValue && studentUserId.Value != userid)
            {
                await _notificationService.CreateAsync(new NotificationCreateDto
                {
                    UserId = studentUserId.Value,
                    Title = "Hóa đơn đã bị xóa",
                    Message = $"Hóa đơn #{id} của bạn đã bị xóa.",
                    Type = "INVOICE"
                });
            }

            return true;
        }

        public async Task<bool> CancelAsync(int id)
        {
            var invoice = await _invoiceRepository.GetByIdAsync(id);
            if (invoice == null) return false;

            if (invoice.Status == "Cancelled")
                throw new ArgumentException("Hóa đơn đã được hủy.");
            if (invoice.Status == "Paid")
                throw new ArgumentException("Hóa đơn đã thanh toán, không thể hủy.");

            var studentUserId = invoice.Student?.UserId;
            await _invoiceRepository.UpdateStatusAsync(id, "Cancelled");

            await _auditLogService.CreateAsync(
                userid,
                "CANCEL",
                "Invoice",
                id,
                $"Hủy hóa đơn #{id} của Student ID {invoice.StudentId}, số tiền {invoice.Amount:0.00}",
                ipaddress);

            if (userid.HasValue)
            {
                await _notificationService.CreateAsync(new NotificationCreateDto
                {
                    UserId = userid.Value,
                    Title = "Hủy hóa đơn",
                    Message = $"Bạn đã hủy hóa đơn #{id}.",
                    Type = "INVOICE"
                });
            }

            if (studentUserId.HasValue && studentUserId.Value != userid)
            {
                await _notificationService.CreateAsync(new NotificationCreateDto
                {
                    UserId = studentUserId.Value,
                    Title = "Hóa đơn đã bị hủy",
                    Message = $"Hóa đơn #{id} của bạn đã bị hủy.",
                    Type = "INVOICE"
                });
            }

            return true;
        }
    }
}