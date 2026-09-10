using EnglishCenter.API.Data;
using EnglishCenter.API.DTOs;
using EnglishCenter.API.Middleware;
using EnglishCenter.API.Models;
using Microsoft.EntityFrameworkCore;

namespace EnglishCenter.API.Services
{
    public class EnrollmentService : IEnrollmentService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditLogService _auditLogService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly INotificationService _notificationService;
        public EnrollmentService(
        ApplicationDbContext context,
        IAuditLogService auditLogService,
        IHttpContextAccessor httpContextAccessor,
        INotificationService notificationService)
        {
            _context = context;
            _auditLogService = auditLogService;
            _httpContextAccessor = httpContextAccessor;
            _notificationService = notificationService;
        }
        private int? userid =>
AuditContext.GetUserId(
 _httpContextAccessor.HttpContext!);

        private string? ipaddress =>
            AuditContext.GetIPAddress(
                _httpContextAccessor.HttpContext!);
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
            var query = _context.Enrollments
                .Include(e => e.Student)
                .Include(e => e.Course)
                .AsQueryable();

            // SEARCH
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(e =>
                    e.Student.FullName.Contains(search) ||
                    e.Student.Email.Contains(search) ||
                    e.Course.CourseName.Contains(search));
            }

            // FILTER
            if (studentId.HasValue)
            {
                query = query.Where(e =>
                    e.StudentId == studentId.Value);
            }

            if (courseId.HasValue)
            {
                query = query.Where(e =>
                    e.CourseId == courseId.Value);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(e =>
                    e.Status == status);
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

                    case "enrollmentdate":
                        query = sortDesc
                            ? query.OrderByDescending(e => e.EnrollmentDate)
                            : query.OrderBy(e => e.EnrollmentDate);
                        break;

                    case "status":
                        query = sortDesc
                            ? query.OrderByDescending(e => e.Status)
                            : query.OrderBy(e => e.Status);
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

            var enrollments = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // DTO
            var data = enrollments.Select(e => new EnrollmentDto
            {
                Id = e.Id,
                StudentId = e.StudentId,
                StudentName = e.Student.FullName,
                CourseId = e.CourseId,
                CourseName = e.Course.CourseName,
                EnrollmentDate = e.EnrollmentDate,
                Status = e.Status
            }).ToList();

            return new PagedResultDto<EnrollmentDto>
            {
                Data = data,
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages
            };
        }

        public async Task<EnrollmentDto?> GetByIdAsync(int id)
        {
            var enrollment = await _context.Enrollments
                .Include(e => e.Student)
                .Include(e => e.Course)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (enrollment == null)
            {
                return null;
            }

            return new EnrollmentDto
            {
                Id = enrollment.Id,

                StudentId = enrollment.StudentId,
                StudentName = enrollment.Student.FullName,

                CourseId = enrollment.CourseId,
                CourseName = enrollment.Course.CourseName,

                EnrollmentDate = enrollment.EnrollmentDate,
                Status = enrollment.Status
            };
        }

        public async Task<EnrollmentDto> CreateAsync(
     EnrollmentCreateDto dto)
        {
            // STUDENT
            var studentExists = await _context.Students
                .AnyAsync(s => s.Id == dto.StudentId);

            if (!studentExists)
            {
                throw new ArgumentException(
                    "Student không tồn tại.");
            }

            // COURSE
            var courseExists = await _context.Courses
                .AnyAsync(c => c.Id == dto.CourseId);

            if (!courseExists)
            {
                throw new ArgumentException(
                    "Course không tồn tại.");
            }

            // ENROLLMENT DATE
            if (dto.EnrollmentDate > DateTime.Now)
            {
                throw new ArgumentException(
                    "Ngày đăng ký không được lớn hơn ngày hiện tại.");
            }

            // CHECK ĐĂNG KÝ TRÙNG
            var existed = await _context.Enrollments
                .AnyAsync(e =>
                    e.StudentId == dto.StudentId &&
                    e.CourseId == dto.CourseId);

            if (existed)
            {
                throw new ArgumentException(
                    "Student đã đăng ký khóa học này.");
            }

            var enrollment = new Enrollment
            {
                StudentId = dto.StudentId,
                CourseId = dto.CourseId,
                EnrollmentDate = dto.EnrollmentDate,
                Status = "Pending"
            };

            _context.Enrollments.Add(enrollment);

            await _context.SaveChangesAsync();
            await _auditLogService.CreateAsync(
    userid,
    "CREATE",
    "Enrollment",
    enrollment.Id,
    $"Student đăng ký khóa học ID {dto.CourseId}",
    ipaddress);

            // Tạo Invoice sau khi đã có EnrollmentId
            var course = await _context.Courses
                .FirstOrDefaultAsync(c => c.Id == dto.CourseId);

            if (course == null)
            {
                throw new ArgumentException(
                    "Course không tồn tại.");
            }

            var invoice = new Invoice
            {
                EnrollmentId = enrollment.Id,
                Amount = course.TuitionFee,
                Status = "Unpaid",
                StudentId= dto.StudentId,
                InvoiceDate = DateTime.Now
            };

            _context.Invoices.Add(invoice);

            await _context.SaveChangesAsync();
            var student = await _context.Students
    .FirstOrDefaultAsync(s => s.Id == dto.StudentId);
            // NOTIFICATION CHO NGƯỜI THỰC HIỆN
            if (userid.HasValue)
            {
                await _notificationService.CreateAsync(
                    new NotificationCreateDto
                    {
                        UserId = userid.Value,
                        Title = "Đăng ký khóa học",
                        Message =
                            $"Bạn đã tạo đăng ký khóa học ID {dto.CourseId}.",
                        Type = "ENROLLMENT"
                    });
            }

            // NOTIFICATION CHO STUDENT
            if (student.UserId != userid)
            {
                await _notificationService.CreateAsync(
                    new NotificationCreateDto
                    {
                        UserId = student.UserId.Value,
                        Title = "Đăng ký khóa học",
                        Message =
                            $"Bạn đã đăng ký khóa học ID {dto.CourseId}. Hóa đơn đang chờ thanh toán.",
                        Type = "ENROLLMENT"
                    });
            }

            return new EnrollmentDto
            {
                Id = enrollment.Id,
                StudentId = enrollment.StudentId,
                CourseId = enrollment.CourseId,
                EnrollmentDate = enrollment.EnrollmentDate,
                Status = enrollment.Status
            };
        }

        public async Task<bool> UpdateAsync(
     int id,
     EnrollmentUpdateDto dto)
        {
            // KIỂM TRA ENROLLMENT
            var enrollment = await _context.Enrollments
                .FindAsync(id);

            if (enrollment == null)
            {
                return false;
            }

            // STUDENT
            var studentExists = await _context.Students
                .AnyAsync(s => s.Id == dto.StudentId);

            if (!studentExists)
            {
                throw new ArgumentException(
                    "Student không tồn tại.");
            }

            // COURSE
            var courseExists = await _context.Courses
                .AnyAsync(c => c.Id == dto.CourseId);

            if (!courseExists)
            {
                throw new ArgumentException(
                    "Course không tồn tại.");
            }

            // ENROLLMENT DATE
            if (dto.EnrollmentDate > DateTime.Now)
            {
                throw new ArgumentException(
                    "Ngày đăng ký không được lớn hơn ngày hiện tại.");
            }

            // CHECK ĐĂNG KÝ TRÙNG
            var existed = await _context.Enrollments
                .AnyAsync(e =>
                    e.Id != id &&
                    e.StudentId == dto.StudentId &&
                    e.CourseId == dto.CourseId);

            if (existed)
            {
                throw new ArgumentException(
                    "Student đã đăng ký khóa học này.");
            }

            // UPDATE
            enrollment.StudentId = dto.StudentId;
            enrollment.CourseId = dto.CourseId;
            enrollment.EnrollmentDate = dto.EnrollmentDate;

            await _context.SaveChangesAsync();
            await _auditLogService.CreateAsync(
    userid,
    "UPDATE",
    "Enrollment",
    enrollment.Id,
    $"Cập nhật đăng ký khóa học ID {enrollment.CourseId}",
    ipaddress);
            // NGƯỜI THỰC HIỆN
            if (userid.HasValue)
            {
                await _notificationService.CreateAsync(
                    new NotificationCreateDto
                    {
                        UserId = userid.Value,
                        Title = "Cập nhật đăng ký",
                        Message =
                            $"Bạn đã cập nhật đăng ký khóa học ID {enrollment.CourseId}.",
                        Type = "ENROLLMENT"
                    });
            }
            var student = await _context.Students
    .FirstOrDefaultAsync(s => s.Id == dto.StudentId);
            // STUDENT LIÊN QUAN
            if (student.UserId != userid)
            {
                await _notificationService.CreateAsync(
                    new NotificationCreateDto
                    {
                        UserId = student.UserId.Value,
                        Title = "Đăng ký khóa học được cập nhật",
                        Message =
                            $"Thông tin đăng ký khóa học ID {enrollment.CourseId} của bạn đã được cập nhật.",
                        Type = "ENROLLMENT"
                    });
            }
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var enrollment = await _context.Enrollments
                .FindAsync(id);

            if (enrollment == null)
            {
                return false;
            }

  
            var courseId = enrollment.CourseId;

            enrollment.IsDeleted = true;

            await _context.SaveChangesAsync();

            await _auditLogService.CreateAsync(
                userid,
                "DELETE",
                "Enrollment",
                id,
                $"Xóa đăng ký khóa học ID {courseId}",
                ipaddress);
            // NOTIFICATION NGƯỜI THỰC HIỆN
            if (userid.HasValue)
            {
                await _notificationService.CreateAsync(
                    new NotificationCreateDto
                    {
                        UserId = userid.Value,
                        Title = "Xóa đăng ký khóa học",
                        Message =
                            $"Bạn đã xóa đăng ký khóa học ID {courseId}.",
                        Type = "ENROLLMENT"
                    });
            }

            var student = await _context.Students
        .FirstOrDefaultAsync(s =>
            s.Id == enrollment.StudentId);

            // NOTIFICATION STUDENT
            if (student != null &&
                student.UserId != userid)
            {
                await _notificationService.CreateAsync(
                    new NotificationCreateDto
                    {
                        UserId = student.UserId.Value,
                        Title = "Đăng ký khóa học đã bị xóa",
                        Message =
                            $"Đăng ký khóa học ID {courseId} của bạn đã bị xóa.",
                        Type = "ENROLLMENT"
                    });
            }

            return true;
        }
        public async Task<bool> CancelAsync(int id)
        {
            var enrollment = await _context.Enrollments
                .FirstOrDefaultAsync(e => e.Id == id);

            if (enrollment == null)
            {
                return false;
            }

            if (enrollment.Status == "Cancelled")
            {
                throw new ArgumentException(
                    "Đăng ký đã được hủy.");
            }

            if (enrollment.Status == "Completed")
            {
                throw new ArgumentException(
                    "Khóa học đã hoàn thành, không thể hủy.");
            }

            var studentUserId = await _context.Students
                .Where(s => s.Id == enrollment.StudentId)
                .Select(s => (int?)s.UserId)
                .FirstOrDefaultAsync();

            enrollment.Status = "Cancelled";

            await _context.SaveChangesAsync();

            await _auditLogService.CreateAsync(
                userid,
                "CANCEL",
                "Enrollment",
                id,
                $"Hủy đăng ký khóa học ID {enrollment.CourseId}",
                ipaddress);

            if (userid.HasValue)
            {
                await _notificationService.CreateAsync(
                    new NotificationCreateDto
                    {
                        UserId = userid.Value,
                        Title = "Hủy đăng ký khóa học",
                        Message =
                            $"Bạn đã hủy đăng ký khóa học ID {enrollment.CourseId}.",
                        Type = "ENROLLMENT"
                    });
            }

            if (studentUserId.HasValue &&
                studentUserId.Value != userid.Value)
            {
                await _notificationService.CreateAsync(
                    new NotificationCreateDto
                    {
                        UserId = studentUserId.Value,
                        Title = "Đăng ký khóa học đã bị hủy",
                        Message =
                            $"Đăng ký khóa học ID {enrollment.CourseId} của bạn đã bị hủy.",
                        Type = "ENROLLMENT"
                    });
            }

            return true;
        }
    }
}