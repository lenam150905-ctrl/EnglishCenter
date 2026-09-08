using EnglishCenter.API.Data;
using EnglishCenter.API.DTOs;
using EnglishCenter.API.Middleware;
using EnglishCenter.API.Models;
using Microsoft.EntityFrameworkCore;

namespace EnglishCenter.API.Services
{
    public class ExamService : IExamService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditLogService _auditLogService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ExamService(
            ApplicationDbContext context,
            IAuditLogService auditLogService, IHttpContextAccessor httpContextAccessor)
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

        public async Task<PagedResultDto<ExamDto>> GetAllAsync(
     string? search,
     int? courseId,
     string? examType,
     string? sortBy,
     bool sortDesc,
     int page,
     int pageSize)
        {
            var query = _context.Exams
                .Include(e => e.Course)
                .AsQueryable();

            // SEARCH
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(e =>
                    e.ExamName.Contains(search) ||
                    e.ExamType.Contains(search) ||
                    (e.Course != null &&
                     e.Course.CourseName.Contains(search)));
            }

            // FILTER
            if (courseId.HasValue)
            {
                query = query.Where(e =>
                    e.CourseId == courseId.Value);
            }

            if (!string.IsNullOrWhiteSpace(examType))
            {
                query = query.Where(e =>
                    e.ExamType == examType);
            }

            // SORT
            if (!string.IsNullOrWhiteSpace(sortBy))
            {
                switch (sortBy.ToLower())
                {
                    case "id":
                        query = sortDesc
                            ? query.OrderByDescending(e => e.Id)
                            : query.OrderBy(e => e.Id);
                        break;

                    case "examname":
                        query = sortDesc
                            ? query.OrderByDescending(e => e.ExamName)
                            : query.OrderBy(e => e.ExamName);
                        break;

                    case "examtype":
                        query = sortDesc
                            ? query.OrderByDescending(e => e.ExamType)
                            : query.OrderBy(e => e.ExamType);
                        break;

                    case "examdate":
                        query = sortDesc
                            ? query.OrderByDescending(e => e.ExamDate)
                            : query.OrderBy(e => e.ExamDate);
                        break;
                }
            }
            else
            {
                query = query.OrderBy(e => e.Id);
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

            var exams = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // DTO
            var data = exams.Select(e => new ExamDto
            {
                Id = e.Id,
                ExamName = e.ExamName,
                ExamType = e.ExamType,
                ExamDate = e.ExamDate,
                CourseId = e.CourseId,
                CourseName = e.Course?.CourseName
            }).ToList();

            return new PagedResultDto<ExamDto>
            {
                Data = data,
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages
            };
        }

        public async Task<ExamDto?> GetByIdAsync(int id)
        {
            var exam = await _context.Exams
                .Include(e => e.Course)
                .FirstOrDefaultAsync(e => e.Id == id);

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
            // EXAM NAME
            if (string.IsNullOrWhiteSpace(dto.ExamName))
            {
                throw new ArgumentException(
                    "Tên bài thi không được để trống.");
            }

            if (dto.ExamName.Length > 100)
            {
                throw new ArgumentException(
                    "Tên bài thi không được vượt quá 100 ký tự.");
            }

            // EXAM TYPE
            if (string.IsNullOrWhiteSpace(dto.ExamType))
            {
                throw new ArgumentException(
                    "Loại bài thi không được để trống.");
            }

            // COURSE
            if (dto.CourseId.HasValue)
            {
                var courseExists = await _context.Courses
                    .AnyAsync(c => c.Id == dto.CourseId.Value);

                if (!courseExists)
                {
                    throw new ArgumentException(
                        "Course không tồn tại.");
                }
            }

            // EXAM DATE
            if (dto.ExamDate < DateTime.Now)
            {
                throw new ArgumentException(
                    "Ngày thi không được ở trong quá khứ.");
            }

            // CHECK EXAM TRÙNG
            var existed = await _context.Exams
                .AnyAsync(e =>
                    e.ExamName == dto.ExamName &&
                    e.CourseId == dto.CourseId);

            if (existed)
            {
                throw new ArgumentException(
                    "Bài thi này đã tồn tại trong khóa học.");
            }

            var exam = new Exam
            {
                ExamName = dto.ExamName,
                ExamType = dto.ExamType,
                ExamDate = dto.ExamDate,
                CourseId = dto.CourseId
            };

            _context.Exams.Add(exam);

            await _context.SaveChangesAsync();

            await _auditLogService.CreateAsync(
                userid,
                "CREATE",
                "Exam",
                exam.Id,
                $"Tạo bài thi {exam.ExamName}",
                ipaddress);

            return new ExamDto
            {
                Id = exam.Id,
                ExamName = exam.ExamName,
                ExamType = exam.ExamType,
                ExamDate = exam.ExamDate,
                CourseId = exam.CourseId
            };
        }

        public async Task<bool> UpdateAsync(
     int id,
     ExamUpdateDto dto)
        {
            // KIỂM TRA EXAM
            var exam = await _context.Exams
                .FindAsync(id);

            if (exam == null)
            {
                return false;
            }

            // EXAM NAME
            if (string.IsNullOrWhiteSpace(dto.ExamName))
            {
                throw new ArgumentException(
                    "Tên bài thi không được để trống.");
            }

            if (dto.ExamName.Length > 100)
            {
                throw new ArgumentException(
                    "Tên bài thi không được vượt quá 100 ký tự.");
            }

            // EXAM TYPE
            if (string.IsNullOrWhiteSpace(dto.ExamType))
            {
                throw new ArgumentException(
                    "Loại bài thi không được để trống.");
            }

            // COURSE
            if (dto.CourseId.HasValue)
            {
                var courseExists = await _context.Courses
                    .AnyAsync(c => c.Id == dto.CourseId.Value);

                if (!courseExists)
                {
                    throw new ArgumentException(
                        "Course không tồn tại.");
                }
            }

            // EXAM DATE
            if (dto.ExamDate < DateTime.Now)
            {
                throw new ArgumentException(
                    "Ngày thi không được ở trong quá khứ.");
            }

            // CHECK TRÙNG
            var existed = await _context.Exams
                .AnyAsync(e =>
                    e.Id != id &&
                    e.ExamName == dto.ExamName &&
                    e.CourseId == dto.CourseId);

            if (existed)
            {
                throw new ArgumentException(
                    "Bài thi này đã tồn tại trong khóa học.");
            }

            // UPDATE
            exam.ExamName = dto.ExamName;
            exam.ExamType = dto.ExamType;
            exam.ExamDate = dto.ExamDate;
            exam.CourseId = dto.CourseId;

            await _context.SaveChangesAsync();

            await _auditLogService.CreateAsync(
                userid,
                "UPDATE",
                "Exam",
                exam.Id,
                $"Cập nhật bài thi {exam.ExamName}",
                ipaddress);

            return true;
        }
        public async Task<bool> DeleteAsync(int id)
        {
            var exam = await _context.Exams
                .FindAsync(id);

            if (exam == null)
            {
                return false;
            }

            var examName = exam.ExamName;

            _context.Exams.Remove(exam);

            await _context.SaveChangesAsync();

            await _auditLogService.CreateAsync(
                userid,
                "DELETE",
                "Exam",
                id,
                $"Xóa bài thi {examName}",
                ipaddress);

            return true;
        }
        public async Task<bool> CanStartExamAsync(
    int studentId,
    int examId)
        {
            // KIỂM TRA EXAM
            var exam = await _context.Exams
                .FirstOrDefaultAsync(e => e.Id == examId);

            if (exam == null)
            {
                throw new ArgumentException(
                    "Bài thi không tồn tại.");
            }

            // KIỂM TRA COURSE
            if (!exam.CourseId.HasValue)
            {
                throw new ArgumentException(
                    "Bài thi chưa thuộc khóa học.");
            }


            // KIỂM TRA ENROLLMENT
            var enrollment = await _context.Enrollments
                .FirstOrDefaultAsync(e =>
                    e.StudentId == studentId &&
                    e.CourseId == exam.CourseId.Value);

            if (enrollment == null)
            {
                throw new ArgumentException(
                    "Student chưa đăng ký khóa học này.");
            }

            // KIỂM TRA HÓA ĐƠN
            var invoicePaid = await _context.Invoices
                .AnyAsync(i =>
                    i.EnrollmentId == enrollment.Id &&
                    i.Status == "Paid");

            if (!invoicePaid)
            {
                throw new ArgumentException(
                    "Student chưa thanh toán khóa học.");
            }
            // KIỂM TRA COURSE ĐÃ HOÀN THÀNH
            var course = await _context.Courses
                .FirstOrDefaultAsync(c =>
                    c.Id == exam.CourseId.Value);

            if (course == null)
            {
                throw new ArgumentException(
                    "Khóa học không tồn tại.");
            }

            if (course.Status != "Completed")
            {
                throw new ArgumentException(
                    "Khóa học chưa hoàn thành.");
            }

            return true;
        }
    }
}