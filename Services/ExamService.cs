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
        private readonly ICertificateRepository _certificateRepository;

        public ExamService(
            IExamRepository examRepository,
            ICourseRepository courseRepository,
            IAuditLogService auditLogService,
            IHttpContextAccessor httpContextAccessor,
            INotificationService notificationService, IGradeRepository gradeRepository,
    IStudentRepository studentRepository,
    IEnrollmentRepository enrollmentRepository,
    IInvoiceRepository invoiceRepository,
    ICertificateRepository certificateRepository)
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
            _certificateRepository = certificateRepository;
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
                throw new ArgumentException("TÃªn ká»³ thi khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng.");
            }

            if (dto.ExamName.Length > 100)
            {
                throw new ArgumentException("TÃªn ká»³ thi khÃ´ng Ä‘Æ°á»£c vÆ°á»£t quÃ¡ 100 kÃ½ tá»±.");
            }

            if (string.IsNullOrWhiteSpace(dto.ExamType))
            {
                throw new ArgumentException("Loáº¡i ká»³ thi khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng.");
            }

            if (dto.CourseId.HasValue)
            {
                var courseExists = await _courseRepository.ExistsAsync(dto.CourseId.Value);
                if (!courseExists)
                {
                    throw new ArgumentException("Course khÃ´ng tá»“n táº¡i.");
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
                $"Táº¡o ká»³ thi {exam.ExamName} - Loáº¡i: {exam.ExamType}",
                ipaddress);

            if (userid.HasValue)
            {
                await _notificationService.CreateAsync(new NotificationCreateDto
                {
                    UserId = userid.Value,
                    Title = "Táº¡o ká»³ thi",
                    Message = $"Báº¡n Ä‘Ã£ táº¡o ká»³ thi {exam.ExamName}.",
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
                throw new ArgumentException("TÃªn ká»³ thi khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng.");
            }

            if (dto.ExamName.Length > 100)
            {
                throw new ArgumentException("TÃªn ká»³ thi khÃ´ng Ä‘Æ°á»£c vÆ°á»£t quÃ¡ 100 kÃ½ tá»±.");
            }

            if (string.IsNullOrWhiteSpace(dto.ExamType))
            {
                throw new ArgumentException("Loáº¡i ká»³ thi khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng.");
            }

            if (dto.CourseId.HasValue)
            {
                var courseExists = await _courseRepository.ExistsAsync(dto.CourseId.Value);
                if (!courseExists)
                {
                    throw new ArgumentException("Course khÃ´ng tá»“n táº¡i.");
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
                $"Cáº­p nháº­t ká»³ thi {exam.ExamName} - Loáº¡i: {exam.ExamType}",
                ipaddress);

            if (userid.HasValue)
            {
                await _notificationService.CreateAsync(new NotificationCreateDto
                {
                    UserId = userid.Value,
                    Title = "Cáº­p nháº­t ká»³ thi",
                    Message = $"Báº¡n Ä‘Ã£ cáº­p nháº­t ká»³ thi {exam.ExamName}.",
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
                $"XÃ³a ká»³ thi {examName} - Loáº¡i: {examType}",
                ipaddress);

            if (userid.HasValue)
            {
                await _notificationService.CreateAsync(new NotificationCreateDto
                {
                    UserId = userid.Value,
                    Title = "XÃ³a ká»³ thi",
                    Message = $"Báº¡n Ä‘Ã£ xÃ³a ká»³ thi {examName}.",
                    Type = "EXAM"
                });
            }

            return true;
        }
        public async Task<ExamStartCheckDto> CanStartExamAsync(
            int studentId,
            int examId)
        {
            var exam = await _examRepository.GetByIdAsync(examId);

            if (exam == null || exam.IsDeleted)
            {
                return new ExamStartCheckDto
                {
                    CanStart = false,
                    ExamId = examId,
                    StudentId = studentId,
                    Message = "Kỳ thi không tồn tại hoặc đã bị xóa."
                };
            }

            var student = await _studentRepository.GetByIdAsync(studentId);

            if (student == null || student.IsDeleted)
            {
                return new ExamStartCheckDto
                {
                    CanStart = false,
                    ExamId = examId,
                    StudentId = studentId,
                    Message = "Học viên không tồn tại hoặc tài khoản đã bị khóa."
                };
            }

            if (!exam.CourseId.HasValue)
            {
                return new ExamStartCheckDto
                {
                    CanStart = false,
                    ExamId = examId,
                    StudentId = studentId,
                    Message = "Kỳ thi chưa được gán vào khóa học."
                };
            }

            var courseId = exam.CourseId.Value;
            var enrollment = await _enrollmentRepository.GetByStudentAndCourseAsync(studentId, courseId);
            if (enrollment == null)
            {
                return new ExamStartCheckDto
                {
                    CanStart = false,
                    ExamId = examId,
                    StudentId = studentId,
                    Message = "Bạn chưa đăng ký khóa học của kỳ thi này."
                };
            }

            if (string.Equals(enrollment.Status, "Cancelled", StringComparison.OrdinalIgnoreCase))
            {
                return new ExamStartCheckDto
                {
                    CanStart = false,
                    ExamId = examId,
                    StudentId = studentId,
                    EnrollmentId = enrollment.Id,
                    Message = "Đăng ký khóa học của bạn đã bị hủy."
                };
            }

            var (grades, _) = await _gradeRepository.GetAllAsync(null, examId, studentId, null, null, null, false, 1, 1);
            var grade = grades.FirstOrDefault() ?? await _gradeRepository.GetByStudentAndCourseAsync(studentId, courseId);

            var (certificates, _) = await _certificateRepository.GetAllAsync(null, studentId, courseId, null, false, 1, 1);
            var certificate = certificates.FirstOrDefault();

            if (certificate != null)
            {
                return new ExamStartCheckDto
                {
                    CanStart = false,
                    ExamId = examId,
                    StudentId = studentId,
                    EnrollmentId = enrollment.Id,
                    HasAttempted = true,
                    AttemptCount = 1,
                    Score = grade?.Score,
                    HasCertificate = true,
                    CertificateCode = certificate.CertificateCode,
                    Message = grade != null
                        ? $"Bạn đã hoàn thành bài thi và được cấp chứng chỉ (Mã: {certificate.CertificateCode}, Điểm: {grade.Score})."
                        : $"Bạn đã được cấp chứng chỉ cho khóa học này (Mã: {certificate.CertificateCode})."
                };
            }

            if (grade != null)
            {
                return new ExamStartCheckDto
                {
                    CanStart = false,
                    ExamId = examId,
                    StudentId = studentId,
                    EnrollmentId = enrollment.Id,
                    HasAttempted = true,
                    AttemptCount = 1,
                    Score = grade.Score,
                    HasCertificate = false,
                    Message = $"Bạn đã hoàn thành bài thi này (Điểm: {grade.Score})."
                };
            }

            var hasPaidInvoice = await _invoiceRepository.HasPaidInvoiceForStudentAndCourseAsync(
                studentId, courseId);
            if (!hasPaidInvoice)
            {
                return new ExamStartCheckDto
                {
                    CanStart = false,
                    ExamId = examId,
                    StudentId = studentId,
                    EnrollmentId = enrollment.Id,
                    Message = "Bạn chưa hoàn tất đóng học phí cho khóa học này nên chưa đủ điều kiện thi."
                };
            }

            return new ExamStartCheckDto
            {
                CanStart = true,
                ExamId = examId,
                StudentId = studentId,
                EnrollmentId = enrollment.Id,
                AttemptCount = 0,
                HasAttempted = false,
                Message = "Bạn đủ điều kiện bắt đầu bài kiểm tra."
            };
        }
    }
}

