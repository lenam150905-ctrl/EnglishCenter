using EnglishCenter.API.DTOs;
using EnglishCenter.API.Middleware;
using EnglishCenter.API.Models;
using EnglishCenter.Application.Abstractions.Persistence;

namespace EnglishCenter.API.Services
{
    public class CertificateService : ICertificateService
    {
        private readonly ICertificateRepository _certificateRepository;
        private readonly IStudentRepository _studentRepository;
        private readonly ICourseRepository _courseRepository;
        private readonly IGradeRepository _gradeRepository;
        private readonly IEnrollmentRepository _enrollmentRepository;
        private readonly IAuditLogService _auditLogService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly INotificationService _notificationService;

        public CertificateService(
            ICertificateRepository certificateRepository,
            IStudentRepository studentRepository,
            ICourseRepository courseRepository,
            IGradeRepository gradeRepository,
            IEnrollmentRepository enrollmentRepository,
            IAuditLogService auditLogService,
            IHttpContextAccessor httpContextAccessor,
            INotificationService notificationService)
        {
            _certificateRepository = certificateRepository;
            _studentRepository = studentRepository;
            _courseRepository = courseRepository;
            _gradeRepository = gradeRepository;
            _enrollmentRepository = enrollmentRepository;
            _auditLogService = auditLogService;
            _httpContextAccessor = httpContextAccessor;
            _notificationService = notificationService;
        }

        private int? userid => AuditContext.GetUserId(_httpContextAccessor.HttpContext!);
        private string? ipaddress => AuditContext.GetIPAddress(_httpContextAccessor.HttpContext!);

        public async Task<PagedResultDto<CertificateDto>> GetAllAsync(
            string? search,
            int? studentId,
            int? courseId,
            string? sortBy,
            bool sortDesc,
            int page,
            int pageSize)
        {
            var (certificates, totalItems) = await _certificateRepository.GetAllAsync(
                search, studentId, courseId, sortBy, sortDesc, page, pageSize);

            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 20 : pageSize;
            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            return new PagedResultDto<CertificateDto>
            {
                Data = certificates.Select(c => new CertificateDto
                {
                    Id = c.Id,
                    StudentId = c.StudentId,
                    StudentName = c.Student?.FullName ?? string.Empty,
                    CourseId = c.CourseId,
                    CourseName = c.Course?.CourseName ?? string.Empty,
                    CertificateCode = c.CertificateCode,
                    IssueDate = c.IssueDate,
                    PdfFilePath = c.PdfFilePath
                }).ToList(),
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages
            };
        }

        public async Task<CertificateDto?> GetByIdAsync(int id)
        {
            var certificate = await _certificateRepository.GetByIdAsync(id);
            if (certificate == null)
            {
                return null;
            }

            return new CertificateDto
            {
                Id = certificate.Id,
                StudentId = certificate.StudentId,
                StudentName = certificate.Student?.FullName ?? string.Empty,
                CourseId = certificate.CourseId,
                CourseName = certificate.Course?.CourseName ?? string.Empty,
                CertificateCode = certificate.CertificateCode,
                IssueDate = certificate.IssueDate,
                PdfFilePath = certificate.PdfFilePath
            };
        }

        public async Task<CertificateDto> CreateAsync(CertificateCreateDto dto)
        {
            var student = await _studentRepository.GetByIdAsync(dto.StudentId);
            if (student == null)
            {
                throw new ArgumentException("Student không tồn tại.");
            }

            var courseExists = await _courseRepository.ExistsAsync(dto.CourseId);
            if (!courseExists)
            {
                throw new ArgumentException("Course không tồn tại.");
            }

            if (string.IsNullOrWhiteSpace(dto.CertificateCode))
            {
                throw new ArgumentException("Mã chứng chỉ không được để trống.");
            }

            if (dto.CertificateCode.Length > 50)
            {
                throw new ArgumentException("Mã chứng chỉ không được vượt quá 50 ký tự.");
            }

            var existedCode = await _certificateRepository.ExistsByCodeAsync(dto.CertificateCode);
            if (existedCode)
            {
                throw new ArgumentException("Mã chứng chỉ đã tồn tại.");
            }

            if (dto.IssueDate > DateTime.Now)
            {
                throw new ArgumentException("Ngày cấp không được lớn hơn ngày hiện tại.");
            }

            var enrollmentExists = await _enrollmentRepository.ExistsAsync(dto.StudentId, dto.CourseId);
            if (!enrollmentExists)
            {
                throw new ArgumentException("Student chưa đăng ký khóa học này.");
            }

            var grade = await _gradeRepository.GetByStudentAndCourseAsync(dto.StudentId, dto.CourseId);
            if (grade == null)
            {
                throw new ArgumentException("Student chưa có điểm thi.");
            }

            if (grade.Score < 5)
            {
                throw new ArgumentException("Student chưa đạt điểm để được cấp chứng chỉ.");
            }

            var existedCertificate = await _certificateRepository.ExistsAsync(dto.StudentId, dto.CourseId);
            if (existedCertificate)
            {
                throw new ArgumentException("Student đã có chứng chỉ cho khóa học này.");
            }

            var certificate = new Certificate
            {
                StudentId = dto.StudentId,
                CourseId = dto.CourseId,
                CertificateCode = dto.CertificateCode,
                IssueDate = dto.IssueDate,
                PdfFilePath = dto.PdfFilePath ?? string.Empty
            };

            await _certificateRepository.CreateAsync(certificate);

            await _auditLogService.CreateAsync(
                userid,
                "CREATE",
                "Certificate",
                certificate.Id,
                $"Cấp chứng chỉ {certificate.CertificateCode} cho Student ID {certificate.StudentId}",
                ipaddress);

            if (userid.HasValue)
            {
                await _notificationService.CreateAsync(new NotificationCreateDto
                {
                    UserId = userid.Value,
                    Title = "Cấp chứng chỉ",
                    Message = $"Bạn đã cấp chứng chỉ {certificate.CertificateCode}.",
                    Type = "CERTIFICATE"
                });
            }

            if (student.UserId.HasValue && student.UserId != userid)
            {
                await _notificationService.CreateAsync(new NotificationCreateDto
                {
                    UserId = student.UserId.Value,
                    Title = "Bạn được cấp chứng chỉ mới",
                    Message = $"Chứng chỉ {certificate.CertificateCode} của bạn đã được cấp.",
                    Type = "CERTIFICATE"
                });
            }

            return new CertificateDto
            {
                Id = certificate.Id,
                StudentId = certificate.StudentId,
                StudentName = student.FullName,
                CourseId = certificate.CourseId,
                CertificateCode = certificate.CertificateCode,
                IssueDate = certificate.IssueDate,
                PdfFilePath = certificate.PdfFilePath
            };
        }

        public async Task<bool> UpdateAsync(int id, CertificateUpdateDto dto)
        {
            var certificate = await _certificateRepository.GetByIdAsync(id);
            if (certificate == null)
            {
                return false;
            }

            var student = await _studentRepository.GetByIdAsync(dto.StudentId);
            if (student == null)
            {
                throw new ArgumentException("Student không tồn tại.");
            }

            var courseExists = await _courseRepository.ExistsAsync(dto.CourseId);
            if (!courseExists)
            {
                throw new ArgumentException("Course không tồn tại.");
            }

            if (string.IsNullOrWhiteSpace(dto.CertificateCode))
            {
                throw new ArgumentException("Mã chứng chỉ không được để trống.");
            }

            if (dto.CertificateCode.Length > 50)
            {
                throw new ArgumentException("Mã chứng chỉ không được vượt quá 50 ký tự.");
            }

            var existedCode = await _certificateRepository.ExistsByCodeAsync(dto.CertificateCode, id);
            if (existedCode)
            {
                throw new ArgumentException("Mã chứng chỉ đã tồn tại.");
            }

            if (dto.IssueDate > DateTime.Now)
            {
                throw new ArgumentException("Ngày cấp không được lớn hơn ngày hiện tại.");
            }

            certificate.StudentId = dto.StudentId;
            certificate.CourseId = dto.CourseId;
            certificate.CertificateCode = dto.CertificateCode;
            certificate.IssueDate = dto.IssueDate;
            if (!string.IsNullOrWhiteSpace(dto.PdfFilePath))
            {
                certificate.PdfFilePath = dto.PdfFilePath;
            }

            await _certificateRepository.UpdateAsync(certificate);

            await _auditLogService.CreateAsync(
                userid,
                "UPDATE",
                "Certificate",
                certificate.Id,
                $"Cập nhật chứng chỉ {certificate.CertificateCode} cho Student ID {certificate.StudentId}",
                ipaddress);

            if (userid.HasValue)
            {
                await _notificationService.CreateAsync(new NotificationCreateDto
                {
                    UserId = userid.Value,
                    Title = "Cập nhật chứng chỉ",
                    Message = $"Bạn đã cập nhật chứng chỉ {certificate.CertificateCode}.",
                    Type = "CERTIFICATE"
                });
            }

            if (student.UserId.HasValue && student.UserId != userid)
            {
                await _notificationService.CreateAsync(new NotificationCreateDto
                {
                    UserId = student.UserId.Value,
                    Title = "Chứng chỉ được cập nhật",
                    Message = $"Chứng chỉ {certificate.CertificateCode} của bạn đã được cập nhật.",
                    Type = "CERTIFICATE"
                });
            }

            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var certificate = await _certificateRepository.GetByIdAsync(id);
            if (certificate == null)
            {
                return false;
            }

            var code = certificate.CertificateCode;
            var studentId = certificate.StudentId;
            var studentUserId = certificate.Student?.UserId;

            await _certificateRepository.SoftDeleteAsync(id);

            await _auditLogService.CreateAsync(
                userid,
                "DELETE",
                "Certificate",
                id,
                $"Xóa chứng chỉ {code} của Student ID {studentId}",
                ipaddress);

            if (userid.HasValue)
            {
                await _notificationService.CreateAsync(new NotificationCreateDto
                {
                    UserId = userid.Value,
                    Title = "Xóa chứng chỉ",
                    Message = $"Bạn đã xóa chứng chỉ {code}.",
                    Type = "CERTIFICATE"
                });
            }

            if (studentUserId.HasValue && studentUserId.Value != userid)
            {
                await _notificationService.CreateAsync(new NotificationCreateDto
                {
                    UserId = studentUserId.Value,
                    Title = "Chứng chỉ đã bị xóa",
                    Message = $"Chứng chỉ {code} của bạn đã bị xóa khỏi hệ thống.",
                    Type = "CERTIFICATE"
                });
            }

            return true;
        }
    }
}