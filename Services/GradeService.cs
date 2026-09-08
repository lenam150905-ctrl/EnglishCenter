using EnglishCenter.API.Data;
using EnglishCenter.API.DTOs;
using EnglishCenter.API.Middleware;
using EnglishCenter.API.Models;
using Microsoft.EntityFrameworkCore;

namespace EnglishCenter.API.Services
{
    public class GradeService : IGradeService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditLogService _auditLogService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public GradeService(
            ApplicationDbContext context,
            IAuditLogService auditLogService,
            IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _auditLogService = auditLogService;
            _httpContextAccessor = httpContextAccessor;
        }
        private int? userid =>
AuditContext.GetUserId(
 _httpContextAccessor.HttpContext!);

        private string? ipaddress =>
            AuditContext.GetIPAddress(
                _httpContextAccessor.HttpContext!);
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
            var query = _context.Grades
                .Include(g => g.Exam)
                .Include(g => g.Student)
                .AsQueryable();

            // SEARCH
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(g =>
                    g.Student.FullName.Contains(search) ||
                    g.Exam.ExamName.Contains(search));
            }

            // FILTER
            if (examId.HasValue)
            {
                query = query.Where(g =>
                    g.ExamId == examId.Value);
            }

            if (studentId.HasValue)
            {
                query = query.Where(g =>
                    g.StudentId == studentId.Value);
            }

            if (minScore.HasValue)
            {
                query = query.Where(g =>
                    g.Score >= minScore.Value);
            }

            if (maxScore.HasValue)
            {
                query = query.Where(g =>
                    g.Score <= maxScore.Value);
            }

            // SORT
            if (!string.IsNullOrWhiteSpace(sortBy))
            {
                switch (sortBy.ToLower())
                {
                    case "id":
                        query = sortDesc
                            ? query.OrderByDescending(g => g.Id)
                            : query.OrderBy(g => g.Id);
                        break;

                    case "score":
                        query = sortDesc
                            ? query.OrderByDescending(g => g.Score)
                            : query.OrderBy(g => g.Score);
                        break;
                }
            }
            else
            {
                query = query.OrderBy(g => g.Id);
            }

            // PAGINATION
            if (page < 1)
            {
                page = 1;
            }

            if (pageSize < 1)
            {
                pageSize = 20;
            }

            var totalItems = await query.CountAsync();

            var totalPages = (int)Math.Ceiling(
                (double)totalItems / pageSize);

            var grades = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // DTO
            var data = grades.Select(g => new GradeDto
            {
                Id = g.Id,
                ExamId = g.ExamId,
                ExamName = g.Exam.ExamName,
                StudentId = g.StudentId,
                StudentName = g.Student.FullName,
                Score = g.Score,
                Comment = g.Comment
            }).ToList();

            return new PagedResultDto<GradeDto>
            {
                Data = data,
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages
            };
        }
        public async Task<GradeDto?> GetByIdAsync(int id)
        {
            var grade = await _context.Grades
                .Include(g => g.Exam)
                .Include(g => g.Student)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (grade == null)
            {
                return null;
            }

            return new GradeDto
            {
                Id = grade.Id,

                ExamId = grade.ExamId,
                ExamName = grade.Exam.ExamName,

                StudentId = grade.StudentId,
                StudentName = grade.Student.FullName,

                Score = grade.Score,
                Comment = grade.Comment
            };
        }

        public async Task<GradeDto> CreateAsync(GradeCreateDto dto)
        {
            // STUDENT
            var studentExists = await _context.Students
                .AnyAsync(s => s.Id == dto.StudentId);

            if (!studentExists)
            {
                throw new ArgumentException(
                    "Student không tồn tại.");
            }

            // EXAM
            var exam = await _context.Exams
                .FirstOrDefaultAsync(e => e.Id == dto.ExamId);

            if (exam == null)
            {
                throw new ArgumentException(
                    "Exam không tồn tại.");
            }

            if (!exam.CourseId.HasValue)
            {
                throw new ArgumentException(
                    "Exam chưa thuộc khóa học.");
            }

            // ENROLLMENT
            var enrollment = await _context.Enrollments
                .FirstOrDefaultAsync(e =>
                    e.StudentId == dto.StudentId &&
                    e.CourseId == exam.CourseId.Value);

            if (enrollment == null)
            {
                throw new ArgumentException(
                    "Student chưa đăng ký khóa học.");
            }

            // PHẢI ĐÃ THANH TOÁN
            if (enrollment.Status != "Active")
            {
                throw new ArgumentException(
                    "Student chưa thanh toán hoặc chưa được kích hoạt khóa học.");
            }

            // SCORE
            if (dto.Score < 0 || dto.Score > 10)
            {
                throw new ArgumentException(
                    "Điểm phải nằm trong khoảng từ 0 đến 10.");
            }

            // COMMENT
            if (dto.Comment != null &&
                dto.Comment.Length > 500)
            {
                throw new ArgumentException(
                    "Nhận xét không được vượt quá 500 ký tự.");
            }

            // CHECK GRADE TRÙNG
            var existed = await _context.Grades
                .AnyAsync(g =>
                    g.ExamId == dto.ExamId &&
                    g.StudentId == dto.StudentId);

            if (existed)
            {
                throw new ArgumentException(
                    "Student đã có điểm cho bài thi này.");
            }

            // CREATE GRADE
            var grade = new Grade
            {
                ExamId = dto.ExamId,
                StudentId = dto.StudentId,
                Score = dto.Score,
                Comment = dto.Comment ?? string.Empty
            };

            _context.Grades.Add(grade);

            await _context.SaveChangesAsync();
            await _auditLogService.CreateAsync(
    userid,
    "CREATE",
    "Grade",
    grade.Id,
    $"Chấm điểm {grade.Score:0.0} cho Student ID {grade.StudentId}, Exam ID {grade.ExamId}",
    ipaddress);

            return new GradeDto
            {
                Id = grade.Id,
                ExamId = grade.ExamId,
                StudentId = grade.StudentId,
                Score = grade.Score,
                Comment = grade.Comment
            };
        }
        public async Task<bool> UpdateAsync(
    int id,
    GradeUpdateDto dto)
        {
            // KIỂM TRA GRADE
            var grade = await _context.Grades
                .FindAsync(id);

            if (grade == null)
            {
                return false;
            }

            // EXAM
            var examExists = await _context.Exams
                .AnyAsync(e => e.Id == dto.ExamId);

            if (!examExists)
            {
                throw new ArgumentException(
                    "Exam không tồn tại.");
            }

            // STUDENT
            var studentExists = await _context.Students
                .AnyAsync(s => s.Id == dto.StudentId);

            if (!studentExists)
            {
                throw new ArgumentException(
                    "Student không tồn tại.");
            }

            // SCORE
            if (dto.Score < 0 || dto.Score > 10)
            {
                throw new ArgumentException(
                    "Điểm phải nằm trong khoảng từ 0 đến 10.");
            }

            // COMMENT
            if (dto.Comment != null &&
                dto.Comment.Length > 500)
            {
                throw new ArgumentException(
                    "Nhận xét không được vượt quá 500 ký tự.");
            }

            // CHECK TRÙNG
            var existed = await _context.Grades
                .AnyAsync(g =>
                    g.Id != id &&
                    g.ExamId == dto.ExamId &&
                    g.StudentId == dto.StudentId);

            if (existed)
            {
                throw new ArgumentException(
                    "Student đã có điểm cho bài thi này.");
            }
            var exam = await _context.Exams
               .FirstOrDefaultAsync(e => e.Id == dto.ExamId);
            var enrollment = await _context.Enrollments
              .FirstOrDefaultAsync(e =>
                  e.StudentId == dto.StudentId &&
                  e.CourseId == exam.CourseId.Value);

            if (enrollment == null)
            {
                throw new ArgumentException(
                    "Student chưa đăng ký khóa học.");
            }

            // PHẢI ĐÃ THANH TOÁN
            if (enrollment.Status != "Active")
            {
                throw new ArgumentException(
                    "Student chưa thanh toán hoặc chưa được kích hoạt khóa học.");
            }

            // UPDATE
            grade.ExamId = dto.ExamId;
            grade.StudentId = dto.StudentId;
            grade.Score = dto.Score;
            grade.Comment = dto.Comment ?? string.Empty;

            await _context.SaveChangesAsync();

            await _auditLogService.CreateAsync(
                userid,
                "UPDATE",
                "Grade",
                grade.Id,
                $"Cập nhật điểm {grade.Score:0.0} cho Student ID {grade.StudentId}, Exam ID {grade.ExamId}",
                ipaddress);

            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var grade = await _context.Grades
                .FindAsync(id);

            if (grade == null)
            {
                return false;
            }

            var studentId = grade.StudentId;
            var examId = grade.ExamId;
            var score = grade.Score;

            _context.Grades.Remove(grade);

            await _context.SaveChangesAsync();

            await _auditLogService.CreateAsync(
                userid,
                "DELETE",
                "Grade",
                id,
                $"Xóa điểm {score:0.0} của Student ID {studentId}, Exam ID {examId}",
                ipaddress);

            return true;
        }
    }
}