using EnglishCenter.API.DTOs;
using EnglishCenter.API.Middleware;
using EnglishCenter.API.Models;
using EnglishCenter.Application.Abstractions.Persistence;

namespace EnglishCenter.API.Services
{
    public class EnrollmentService : IEnrollmentService
    {
        private readonly IEnrollmentRepository _enrollmentRepository;
        private readonly IStudentRepository _studentRepository;
        private readonly ICourseRepository _courseRepository;
        private readonly IInvoiceRepository _invoiceRepository;
        private readonly IAuditLogService _auditLogService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly INotificationService _notificationService;

        public EnrollmentService(
            IEnrollmentRepository enrollmentRepository,
            IStudentRepository studentRepository,
            ICourseRepository courseRepository,
            IInvoiceRepository invoiceRepository,
            IAuditLogService auditLogService,
            IHttpContextAccessor httpContextAccessor,
            INotificationService notificationService)
        {
            _enrollmentRepository = enrollmentRepository;
            _studentRepository = studentRepository;
            _courseRepository = courseRepository;
            _invoiceRepository = invoiceRepository;
            _auditLogService = auditLogService;
            _httpContextAccessor = httpContextAccessor;
            _notificationService = notificationService;
        }

        private int? userid => AuditContext.GetUserId(_httpContextAccessor.HttpContext!);
        private string? ipaddress => AuditContext.GetIPAddress(_httpContextAccessor.HttpContext!);

        public async Task<PagedResultDto<EnrollmentDto>> GetAllAsync(
            string? search,
            int? studentId,
            int? courseId,
            string? status,
            string? sortBy,
            bool sortDesc,
            int page,
            int pageSize)
        {
            var (enrollments, totalItems) = await _enrollmentRepository.GetAllAsync(
                search, studentId, courseId, status, sortBy, sortDesc, page, pageSize);

            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 20 : pageSize;
            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            return new PagedResultDto<EnrollmentDto>
            {
                Data = enrollments.Select(e => new EnrollmentDto
                {
                    Id = e.Id,
                    StudentId = e.StudentId,
                    StudentName = e.Student?.FullName ?? string.Empty,
                    CourseId = e.CourseId,
                    CourseName = e.Course?.CourseName ?? string.Empty,
                    EnrollmentDate = e.EnrollmentDate,
                    Status = e.Status
                }).ToList(),
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages
            };
        }

        public async Task<EnrollmentDto?> GetByIdAsync(int id)
        {
            var enrollment = await _enrollmentRepository.GetByIdAsync(id);
            if (enrollment == null)
            {
                return null;
            }

            return new EnrollmentDto
            {
                Id = enrollment.Id,
                StudentId = enrollment.StudentId,
                StudentName = enrollment.Student?.FullName ?? string.Empty,
                CourseId = enrollment.CourseId,
                CourseName = enrollment.Course?.CourseName ?? string.Empty,
                EnrollmentDate = enrollment.EnrollmentDate,
                Status = enrollment.Status
            };
        }

        public async Task<EnrollmentDto> CreateAsync(EnrollmentCreateDto dto)
        {
            var studentExists = await _studentRepository.ExistsAsync(dto.StudentId);
            if (!studentExists)
            {
                throw new ArgumentException("Student không tồn tại.");
            }

            var course = await _courseRepository.GetByIdAsync(dto.CourseId);
            if (course == null)
            {
                throw new ArgumentException("Course không tồn tại.");
            }

            if (dto.EnrollmentDate > DateTime.Now)
            {
                throw new ArgumentException("Ngày đăng ký không được lớn hơn ngày hiện tại.");
            }

            var existed = await _enrollmentRepository.ExistsAsync(dto.StudentId, dto.CourseId);
            if (existed)
            {
                throw new ArgumentException("Student đã đăng ký khóa học này.");
            }

            var enrollment = new Enrollment
            {
                StudentId = dto.StudentId,
                CourseId = dto.CourseId,
                EnrollmentDate = dto.EnrollmentDate,
                Status = "Pending"
            };

            await _enrollmentRepository.CreateAsync(enrollment);

            var invoice = new Invoice
            {
                EnrollmentId = enrollment.Id,
                StudentId = dto.StudentId,
                Amount = course.TuitionFee,
                Status = "Unpaid",
                InvoiceDate = DateTime.Now
            };
            await _invoiceRepository.CreateAsync(invoice);

            var created = await _enrollmentRepository.GetByIdAsync(enrollment.Id);

            await _auditLogService.CreateAsync(
                userid,
                "CREATE",
                "Enrollment",
                enrollment.Id,
                $"Student đăng ký khóa học ID {dto.CourseId}",
                ipaddress);

            if (userid.HasValue)
            {
                await _notificationService.CreateAsync(new NotificationCreateDto
                {
                    UserId = userid.Value,
                    Title = "Đăng ký khóa học",
                    Message = $"Bạn đã tạo đăng ký khóa học cho học sinh {created?.Student?.FullName}.",
                    Type = "ENROLLMENT"
                });
            }

            if (created?.Student?.UserId.HasValue == true && created.Student.UserId != userid)
            {
                await _notificationService.CreateAsync(new NotificationCreateDto
                {
                    UserId = created.Student.UserId.Value,
                    Title = "Đăng ký khóa học thành công",
                    Message = $"Bạn đã được đăng ký vào khóa học {course.CourseName}. Vui lòng thanh toán học phí.",
                    Type = "ENROLLMENT"
                });
            }

            return new EnrollmentDto
            {
                Id = enrollment.Id,
                StudentId = enrollment.StudentId,
                StudentName = created?.Student?.FullName ?? string.Empty,
                CourseId = enrollment.CourseId,
                CourseName = created?.Course?.CourseName ?? string.Empty,
                EnrollmentDate = enrollment.EnrollmentDate,
                Status = enrollment.Status
            };
        }

        public async Task<bool> UpdateAsync(int id, EnrollmentUpdateDto dto)
        {
            var enrollment = await _enrollmentRepository.GetByIdAsync(id);
            if (enrollment == null)
            {
                return false;
            }

            var studentExists = await _studentRepository.ExistsAsync(dto.StudentId);
            if (!studentExists)
            {
                throw new ArgumentException("Student không tồn tại.");
            }

            var courseExists = await _courseRepository.ExistsAsync(dto.CourseId);
            if (!courseExists)
            {
                throw new ArgumentException("Course không tồn tại.");
            }

            if (dto.EnrollmentDate > DateTime.Now)
            {
                throw new ArgumentException("Ngày đăng ký không được lớn hơn ngày hiện tại.");
            }

            var existed = await _enrollmentRepository.ExistsAsync(dto.StudentId, dto.CourseId, id);
            if (existed)
            {
                throw new ArgumentException("Student đã đăng ký khóa học này.");
            }

            enrollment.StudentId = dto.StudentId;
            enrollment.CourseId = dto.CourseId;
            enrollment.EnrollmentDate = dto.EnrollmentDate;
            enrollment.Status = dto.Status;

            await _enrollmentRepository.UpdateAsync(enrollment);

            var updated = await _enrollmentRepository.GetByIdAsync(id);

            await _auditLogService.CreateAsync(
                userid,
                "UPDATE",
                "Enrollment",
                enrollment.Id,
                $"Cập nhật đăng ký khóa học: Student {updated?.Student?.FullName} - Course {updated?.Course?.CourseName}",
                ipaddress);

            if (userid.HasValue)
            {
                await _notificationService.CreateAsync(new NotificationCreateDto
                {
                    UserId = userid.Value,
                    Title = "Cập nhật đăng ký",
                    Message = $"Bạn đã cập nhật thông tin đăng ký khóa học cho học sinh {updated?.Student?.FullName}.",
                    Type = "ENROLLMENT"
                });
            }

            if (updated?.Student?.UserId.HasValue == true && updated.Student.UserId != userid)
            {
                await _notificationService.CreateAsync(new NotificationCreateDto
                {
                    UserId = updated.Student.UserId.Value,
                    Title = "Đăng ký khóa học được cập nhật",
                    Message = $"Thông tin đăng ký khóa học {updated?.Course?.CourseName} của bạn đã được cập nhật.",
                    Type = "ENROLLMENT"
                });
            }

            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var enrollment = await _enrollmentRepository.GetByIdAsync(id);
            if (enrollment == null)
            {
                return false;
            }

            var studentName = enrollment.Student?.FullName;
            var courseName = enrollment.Course?.CourseName;
            var studentUserId = enrollment.Student?.UserId;

            await _enrollmentRepository.SoftDeleteAsync(id);

            await _auditLogService.CreateAsync(
                userid,
                "DELETE",
                "Enrollment",
                id,
                $"Xóa đăng ký: Student {studentName} - Course {courseName}",
                ipaddress);

            if (userid.HasValue)
            {
                await _notificationService.CreateAsync(new NotificationCreateDto
                {
                    UserId = userid.Value,
                    Title = "Xóa đăng ký khóa học",
                    Message = $"Bạn đã xóa đăng ký khóa học của học sinh {studentName}.",
                    Type = "ENROLLMENT"
                });
            }

            if (studentUserId.HasValue && studentUserId.Value != userid)
            {
                await _notificationService.CreateAsync(new NotificationCreateDto
                {
                    UserId = studentUserId.Value,
                    Title = "Đăng ký khóa học đã bị hủy",
                    Message = $"Đăng ký khóa học {courseName} của bạn đã bị hủy.",
                    Type = "ENROLLMENT"
                });
            }

            return true;
        }

        public async Task<bool> CancelAsync(int id)
        {
            var enrollment = await _enrollmentRepository.GetByIdAsync(id);
            if (enrollment == null) return false;

            if (enrollment.Status == "Cancelled")
                throw new ArgumentException("Đăng ký đã được hủy.");
            if (enrollment.Status == "Completed")
                throw new ArgumentException("Khóa học đã hoàn thành, không thể hủy.");

            var studentUserId = await _enrollmentRepository.GetStudentUserIdByEnrollmentIdAsync(id);
            await _enrollmentRepository.UpdateStatusAsync(id, "Cancelled");

            await _auditLogService.CreateAsync(
                userid,
                "CANCEL",
                "Enrollment",
                id,
                $"Hủy đăng ký khóa học ID {enrollment.CourseId}",
                ipaddress);

            if (userid.HasValue)
            {
                await _notificationService.CreateAsync(new NotificationCreateDto
                {
                    UserId = userid.Value,
                    Title = "Hủy đăng ký khóa học",
                    Message = $"Bạn đã hủy đăng ký khóa học ID {enrollment.CourseId}.",
                    Type = "ENROLLMENT"
                });
            }

            if (studentUserId.HasValue && studentUserId.Value != userid)
            {
                await _notificationService.CreateAsync(new NotificationCreateDto
                {
                    UserId = studentUserId.Value,
                    Title = "Đăng ký khóa học đã bị hủy",
                    Message = $"Đăng ký khóa học ID {enrollment.CourseId} của bạn đã bị hủy.",
                    Type = "ENROLLMENT"
                });
            }

            return true;
        }
    }
}