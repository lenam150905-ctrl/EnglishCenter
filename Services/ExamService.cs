using EnglishCenter.API.DTOs;
using EnglishCenter.API.Middleware;
using EnglishCenter.API.Models;
using EnglishCenter.Application.Abstractions.Persistence;
using EnglishCenter.Infrastructure.Persistence.Dapper;

namespace EnglishCenter.API.Services
{
    public class ExamService : IExamService
    {
        private readonly IExamRepository _examRepository;
        private readonly ICourseRepository _courseRepository;
        private readonly IAuditLogService _auditLogService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly INotificationService _notificationService;
        private readonly IStudentRepository _studentRepository;
        private readonly IEnrollmentRepository _enrollmentRepository;
        private readonly IGradeRepository _gradeRepository;
        private readonly IInvoiceRepository _invoiceRepository;

        public ExamService(
            IExamRepository examRepository,
            ICourseRepository courseRepository,
            IAuditLogService auditLogService,
            IHttpContextAccessor httpContextAccessor,
            INotificationService notificationService, IGradeRepository gradeRepository,
    IStudentRepository studentRepository,
    IEnrollmentRepository enrollmentRepository,
    IInvoiceRepository invoiceRepository)
        {
            _examRepository = examRepository;
            _courseRepository = courseRepository;
            _auditLogService = auditLogService;
            _httpContextAccessor = httpContextAccessor;
            _notificationService = notificationService;
            _studentRepository = studentRepository;
            _enrollmentRepository = enrollmentRepository;
            _gradeRepository = gradeRepository;
            _invoiceRepository = invoiceRepository;
        }

        private int? userid => AuditContext.GetUserId(_httpContextAccessor.HttpContext!);
        private string? ipaddress => AuditContext.GetIPAddress(_httpContextAccessor.HttpContext!);

        public async Task<PagedResultDto<ExamDto>> GetAllAsync(
            string? search,
            int? courseId,
            string? examType,
            string? sortBy,
            bool sortDesc,
            int page,
            int pageSize)
        {
            var (exams, totalItems) = await _examRepository.GetAllAsync(
                search, courseId, examType, sortBy, sortDesc, page, pageSize);

            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 20 : pageSize;
            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            return new PagedResultDto<ExamDto>
            {
                Data = exams.Select(e => new ExamDto
                {
                    Id = e.Id,
                    ExamName = e.ExamName,
                    ExamType = e.ExamType,
                    ExamDate = e.ExamDate,
                    CourseId = e.CourseId,
                    CourseName = e.Course?.CourseName
                }).ToList(),
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages
            };
        }

        public async Task<ExamDto?> GetByIdAsync(int id)
        {
            var exam = await _examRepository.GetByIdAsync(id);
            if (exam == null)
            {
                return null;
            }

            return new ExamDto
            {
                Id = exam.Id,
                ExamName = exam.ExamName,
                ExamType = exam.ExamType,
                ExamDate = exam.ExamDate,
                CourseId = exam.CourseId,
                CourseName = exam.Course?.CourseName
            };
        }

        public async Task<ExamDto> CreateAsync(ExamCreateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.ExamName))
            {
                throw new ArgumentException("Tên kỳ thi không được để trống.");
            }

            if (dto.ExamName.Length > 100)
            {
                throw new ArgumentException("Tên kỳ thi không được vượt quá 100 ký tự.");
            }

            if (string.IsNullOrWhiteSpace(dto.ExamType))
            {
                throw new ArgumentException("Loại kỳ thi không được để trống.");
            }

            if (dto.CourseId.HasValue)
            {
                var courseExists = await _courseRepository.ExistsAsync(dto.CourseId.Value);
                if (!courseExists)
                {
                    throw new ArgumentException("Course không tồn tại.");
                }
            }

            var exam = new Exam
            {
                ExamName = dto.ExamName,
                ExamType = dto.ExamType,
                ExamDate = dto.ExamDate,
                CourseId = dto.CourseId
            };

            await _examRepository.CreateAsync(exam);

            var created = await _examRepository.GetByIdAsync(exam.Id);

            await _auditLogService.CreateAsync(
                userid,
                "CREATE",
                "Exam",
                exam.Id,
                $"Tạo kỳ thi {exam.ExamName} - Loại: {exam.ExamType}",
                ipaddress);

            if (userid.HasValue)
            {
                await _notificationService.CreateAsync(new NotificationCreateDto
                {
                    UserId = userid.Value,
                    Title = "Tạo kỳ thi",
                    Message = $"Bạn đã tạo kỳ thi {exam.ExamName}.",
                    Type = "EXAM"
                });
            }

            return new ExamDto
            {
                Id = exam.Id,
                ExamName = exam.ExamName,
                ExamType = exam.ExamType,
                ExamDate = exam.ExamDate,
                CourseId = exam.CourseId,
                CourseName = created?.Course?.CourseName
            };
        }

        public async Task<bool> UpdateAsync(int id, ExamUpdateDto dto)
        {
            var exam = await _examRepository.GetByIdAsync(id);
            if (exam == null)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(dto.ExamName))
            {
                throw new ArgumentException("Tên kỳ thi không được để trống.");
            }

            if (dto.ExamName.Length > 100)
            {
                throw new ArgumentException("Tên kỳ thi không được vượt quá 100 ký tự.");
            }

            if (string.IsNullOrWhiteSpace(dto.ExamType))
            {
                throw new ArgumentException("Loại kỳ thi không được để trống.");
            }

            if (dto.CourseId.HasValue)
            {
                var courseExists = await _courseRepository.ExistsAsync(dto.CourseId.Value);
                if (!courseExists)
                {
                    throw new ArgumentException("Course không tồn tại.");
                }
            }

            exam.ExamName = dto.ExamName;
            exam.ExamType = dto.ExamType;
            exam.ExamDate = dto.ExamDate;
            exam.CourseId = dto.CourseId;

            await _examRepository.UpdateAsync(exam);

            await _auditLogService.CreateAsync(
                userid,
                "UPDATE",
                "Exam",
                exam.Id,
                $"Cập nhật kỳ thi {exam.ExamName} - Loại: {exam.ExamType}",
                ipaddress);

            if (userid.HasValue)
            {
                await _notificationService.CreateAsync(new NotificationCreateDto
                {
                    UserId = userid.Value,
                    Title = "Cập nhật kỳ thi",
                    Message = $"Bạn đã cập nhật kỳ thi {exam.ExamName}.",
                    Type = "EXAM"
                });
            }

            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var exam = await _examRepository.GetByIdAsync(id);
            if (exam == null)
            {
                return false;
            }

            var examName = exam.ExamName;
            var examType = exam.ExamType;

            await _examRepository.SoftDeleteAsync(id);

            await _auditLogService.CreateAsync(
                userid,
                "DELETE",
                "Exam",
                id,
                $"Xóa kỳ thi {examName} - Loại: {examType}",
                ipaddress);

            if (userid.HasValue)
            {
                await _notificationService.CreateAsync(new NotificationCreateDto
                {
                    UserId = userid.Value,
                    Title = "Xóa kỳ thi",
                    Message = $"Bạn đã xóa kỳ thi {examName}.",
                    Type = "EXAM"
                });
            }

            return true;
        }
        public async Task<bool> CanStartExamAsync(
      int studentId,
      int examId)
        {
            var exam = await _examRepository.GetByIdAsync(examId);

            if (exam == null || exam.IsDeleted)
            {
                return false;
            }

            var student = await _studentRepository.GetByIdAsync(studentId);

            if (student == null)
            {
                return false;
            }

            if (!exam.CourseId.HasValue)
            {
                return false;
            }

            var hasPaidInvoice = await _invoiceRepository.HasPaidInvoiceForStudentAndCourseAsync(
                studentId, exam.CourseId.Value);
            if (!hasPaidInvoice) return false;

            // Kết quả thi được lưu ở bảng Grades. Nếu đã có điểm thì học viên
            // không thể bắt đầu lại bài thi này.
            var hasCompletedAttempt = await _gradeRepository.ExistsAsync(
                examId,
                studentId);

            if (hasCompletedAttempt)
            {
                return false;
            }

            return true;
        }
    }
}
