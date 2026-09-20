using EnglishCenter.API.DTOs;
using EnglishCenter.API.Middleware;
using EnglishCenter.API.Models;
using EnglishCenter.Application.Abstractions.Persistence;

namespace EnglishCenter.API.Services
{
    public class GradeService : IGradeService
    {
        private readonly IGradeRepository _gradeRepository;
        private readonly IExamRepository _examRepository;
        private readonly IStudentRepository _studentRepository;
        private readonly IEnrollmentRepository _enrollmentRepository;
        private readonly IAuditLogService _auditLogService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly INotificationService _notificationService;

        public GradeService(
            IGradeRepository gradeRepository,
            IExamRepository examRepository,
            IStudentRepository studentRepository,
            IEnrollmentRepository enrollmentRepository,
            IAuditLogService auditLogService,
            IHttpContextAccessor httpContextAccessor,
            INotificationService notificationService)
        {
            _gradeRepository = gradeRepository;
            _examRepository = examRepository;
            _studentRepository = studentRepository;
            _enrollmentRepository = enrollmentRepository;
            _auditLogService = auditLogService;
            _httpContextAccessor = httpContextAccessor;
            _notificationService = notificationService;
        }

        private int? userid => AuditContext.GetUserId(_httpContextAccessor.HttpContext!);
        private string? ipaddress => AuditContext.GetIPAddress(_httpContextAccessor.HttpContext!);

        public async Task<PagedResultDto<GradeDto>> GetAllAsync(
            string? search,
            int? examId,
            int? studentId,
            decimal? minScore,
            decimal? maxScore,
            string? sortBy,
            bool sortDesc,
            int page,
            int pageSize)
        {
            var (grades, totalItems) = await _gradeRepository.GetAllAsync(
                search, examId, studentId, minScore, maxScore, sortBy, sortDesc, page, pageSize);

            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 20 : pageSize;
            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            return new PagedResultDto<GradeDto>
            {
                Data = grades.Select(g => new GradeDto
                {
                    Id = g.Id,
                    ExamId = g.ExamId,
                    ExamName = g.Exam?.ExamName ?? string.Empty,
                    StudentId = g.StudentId,
                    StudentName = g.Student?.FullName ?? string.Empty,
                    Score = g.Score,
                    Comment = g.Comment
                }).ToList(),
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages
            };
        }

        public async Task<GradeDto?> GetByIdAsync(int id)
        {
            var grade = await _gradeRepository.GetByIdAsync(id);
            if (grade == null)
            {
                return null;
            }

            return new GradeDto
            {
                Id = grade.Id,
                ExamId = grade.ExamId,
                ExamName = grade.Exam?.ExamName ?? string.Empty,
                StudentId = grade.StudentId,
                StudentName = grade.Student?.FullName ?? string.Empty,
                Score = grade.Score,
                Comment = grade.Comment
            };
        }

        public async Task<GradeDto> CreateAsync(GradeCreateDto dto)
        {
            var exam = await _examRepository.GetByIdAsync(dto.ExamId);
            if (exam == null)
            {
                throw new ArgumentException("Kỳ thi không tồn tại.");
            }

            var student = await _studentRepository.GetByIdAsync(dto.StudentId);
            if (student == null)
            {
                throw new ArgumentException("Học sinh không tồn tại.");
            }

            if (exam.CourseId.HasValue)
            {
                var enrolled = await _enrollmentRepository.ExistsAsync(dto.StudentId, exam.CourseId.Value);
                if (!enrolled)
                {
                    throw new ArgumentException("Học sinh chưa đăng ký khóa học của kỳ thi này.");
                }
            }

            if (dto.Score < 0 || dto.Score > 10)
            {
                throw new ArgumentException("Điểm số phải từ 0 đến 10.");
            }

            var existed = await _gradeRepository.ExistsAsync(dto.ExamId, dto.StudentId);
            if (existed)
            {
                throw new ArgumentException("Học sinh này đã có điểm trong kỳ thi.");
            }

            var grade = new Grade
            {
                ExamId = dto.ExamId,
                StudentId = dto.StudentId,
                Score = dto.Score,
                Comment = dto.Comment
            };

            await _gradeRepository.CreateAsync(grade);

            await _auditLogService.CreateAsync(
                userid,
                "CREATE",
                "Grade",
                grade.Id,
                $"Nhập điểm: Học sinh {student.FullName}, Kỳ thi {exam.ExamName}, Điểm: {grade.Score}",
                ipaddress);

            if (userid.HasValue)
            {
                await _notificationService.CreateAsync(new NotificationCreateDto
                {
                    UserId = userid.Value,
                    Title = "Nhập điểm",
                    Message = $"Bạn đã nhập điểm cho học sinh {student.FullName}.",
                    Type = "GRADE"
                });
            }

            if (student.UserId.HasValue && student.UserId != userid)
            {
                await _notificationService.CreateAsync(new NotificationCreateDto
                {
                    UserId = student.UserId.Value,
                    Title = "Có điểm thi mới",
                    Message = $"Điểm thi kỳ thi {exam.ExamName} của bạn là: {grade.Score}.",
                    Type = "GRADE"
                });
            }

            return new GradeDto
            {
                Id = grade.Id,
                ExamId = grade.ExamId,
                ExamName = exam.ExamName,
                StudentId = grade.StudentId,
                StudentName = student.FullName,
                Score = grade.Score,
                Comment = grade.Comment
            };
        }

        public async Task<bool> UpdateAsync(int id, GradeUpdateDto dto)
        {
            var grade = await _gradeRepository.GetByIdAsync(id);
            if (grade == null)
            {
                return false;
            }

            if (dto.Score < 0 || dto.Score > 10)
            {
                throw new ArgumentException("Điểm số phải từ 0 đến 10.");
            }

            grade.Score = dto.Score;
            grade.Comment = dto.Comment;

            await _gradeRepository.UpdateAsync(grade);

            await _auditLogService.CreateAsync(
                userid,
                "UPDATE",
                "Grade",
                grade.Id,
                $"Cập nhật điểm: Học sinh {grade.Student?.FullName}, Kỳ thi {grade.Exam?.ExamName}, Điểm mới: {grade.Score}",
                ipaddress);

            if (userid.HasValue)
            {
                await _notificationService.CreateAsync(new NotificationCreateDto
                {
                    UserId = userid.Value,
                    Title = "Cập nhật điểm",
                    Message = $"Bạn đã cập nhật điểm cho học sinh {grade.Student?.FullName}.",
                    Type = "GRADE"
                });
            }

            if (grade.Student?.UserId.HasValue == true && grade.Student.UserId != userid)
            {
                await _notificationService.CreateAsync(new NotificationCreateDto
                {
                    UserId = grade.Student.UserId.Value,
                    Title = "Điểm thi được cập nhật",
                    Message = $"Điểm thi kỳ thi {grade.Exam?.ExamName} của bạn đã được cập nhật thành: {grade.Score}.",
                    Type = "GRADE"
                });
            }

            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var grade = await _gradeRepository.GetByIdAsync(id);
            if (grade == null)
            {
                return false;
            }

            var studentName = grade.Student?.FullName;
            var examName = grade.Exam?.ExamName;
            var studentUserId = grade.Student?.UserId;

            await _gradeRepository.SoftDeleteAsync(id);

            await _auditLogService.CreateAsync(
                userid,
                "DELETE",
                "Grade",
                id,
                $"Xóa điểm: Học sinh {studentName}, Kỳ thi {examName}",
                ipaddress);

            if (userid.HasValue)
            {
                await _notificationService.CreateAsync(new NotificationCreateDto
                {
                    UserId = userid.Value,
                    Title = "Xóa điểm thi",
                    Message = $"Bạn đã xóa điểm thi của học sinh {studentName}.",
                    Type = "GRADE"
                });
            }

            if (studentUserId.HasValue && studentUserId.Value != userid)
            {
                await _notificationService.CreateAsync(new NotificationCreateDto
                {
                    UserId = studentUserId.Value,
                    Title = "Điểm thi đã bị xóa",
                    Message = $"Điểm thi kỳ thi {examName} của bạn đã bị xóa khỏi hệ thống.",
                    Type = "GRADE"
                });
            }

            return true;
        }
    }
}