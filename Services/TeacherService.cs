using EnglishCenter.API.Data;
using EnglishCenter.API.DTOs;
using EnglishCenter.API.Middleware;
using EnglishCenter.API.Models;
using Microsoft.EntityFrameworkCore;

namespace EnglishCenter.API.Services
{
    public class TeacherService : ITeacherService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditLogService _auditLogService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly INotificationService _notificationService;
        public TeacherService(
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

        public async Task<PagedResultDto<TeacherDto>> GetAllAsync(
      string? search,
      string? specialization,
      string? sortBy,
      bool sortDesc,
      int page,
      int pageSize)
        {
            var query = _context.Teachers.AsQueryable();

            // SEARCH
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(t =>
                    t.FullName.Contains(search) ||
                    t.Email.Contains(search) ||
                    t.Phone.Contains(search) ||
                    t.Specialization.Contains(search));
            }

            // FILTER
            if (!string.IsNullOrWhiteSpace(specialization))
            {
                query = query.Where(t =>
                    t.Specialization == specialization);
            }

            // SORT
            if (!string.IsNullOrWhiteSpace(sortBy))
            {
                switch (sortBy.ToLower())
                {
                    case "id":
                        query = sortDesc
                            ? query.OrderByDescending(t => t.Id)
                            : query.OrderBy(t => t.Id);
                        break;

                    case "fullname":
                        query = sortDesc
                            ? query.OrderByDescending(t => t.FullName)
                            : query.OrderBy(t => t.FullName);
                        break;

                    case "email":
                        query = sortDesc
                            ? query.OrderByDescending(t => t.Email)
                            : query.OrderBy(t => t.Email);
                        break;

                    case "specialization":
                        query = sortDesc
                            ? query.OrderByDescending(t => t.Specialization)
                            : query.OrderBy(t => t.Specialization);
                        break;
                }
            }
            else
            {
                query = query.OrderBy(t => t.Id);
            }

            // VALIDATE PAGINATION
            if (page < 1)
            {
                page = 1;
            }

            if (pageSize < 1)
            {
                pageSize = 20;
            }

            // TOTAL
            var totalItems = await query.CountAsync();

            var totalPages = (int)Math.Ceiling(
                (double)totalItems / pageSize);

            // PAGINATION
            var teachers = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // DTO
            var data = teachers.Select(t => new TeacherDto
            {
                Id = t.Id,
                FullName = t.FullName,
                Email = t.Email,
                Phone = t.Phone,
                Specialization = t.Specialization,
                UserId = t.UserId
            }).ToList();

            return new PagedResultDto<TeacherDto>
            {
                Data = data,
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages
            };
        }

        public async Task<TeacherDto?> GetByIdAsync(int id)
        {
            var teacher = await _context.Teachers
                .FirstOrDefaultAsync(t => t.Id == id);

            if (teacher == null)
            {
                return null;
            }

            return new TeacherDto
            {
                Id = teacher.Id,
                FullName = teacher.FullName,
                Email = teacher.Email,
                Phone = teacher.Phone,
                Specialization = teacher.Specialization,
                UserId = teacher.UserId
            };
        }

        public async Task<TeacherDto> CreateAsync(TeacherCreateDto dto)
        {
            // FULL NAME
            if (string.IsNullOrWhiteSpace(dto.FullName))
            {
                throw new ArgumentException(
                    "Họ tên không được để trống.");
            }

            if (dto.FullName.Length > 100)
            {
                throw new ArgumentException(
                    "Họ tên không được vượt quá 100 ký tự.");
            }

            // EMAIL
            if (string.IsNullOrWhiteSpace(dto.Email))
            {
                throw new ArgumentException(
                    "Email không được để trống.");
            }

            if (!dto.Email.Contains("@"))
            {
                throw new ArgumentException(
                    "Email không hợp lệ.");
            }

            // PHONE
            if (string.IsNullOrWhiteSpace(dto.Phone))
            {
                throw new ArgumentException(
                    "Số điện thoại không được để trống.");
            }

            if (dto.Phone.Length < 9 || dto.Phone.Length > 15)
            {
                throw new ArgumentException(
                    "Số điện thoại không hợp lệ.");
            }

            // SPECIALIZATION
            if (string.IsNullOrWhiteSpace(dto.Specialization))
            {
                throw new ArgumentException(
                    "Chuyên môn không được để trống.");
            }

            // CHECK EMAIL TRÙNG
            var existedEmail = await _context.Teachers
                .AnyAsync(t => t.Email == dto.Email);

            if (existedEmail)
            {
                throw new ArgumentException(
                    "Email đã tồn tại.");
            }

            // CHECK PHONE TRÙNG
            var existedPhone = await _context.Teachers
                .AnyAsync(t => t.Phone == dto.Phone);

            if (existedPhone)
            {
                throw new ArgumentException(
                    "Số điện thoại đã tồn tại.");
            }

            var teacher = new Teacher
            {
                FullName = dto.FullName,
                Email = dto.Email,
                Phone = dto.Phone,
                Specialization = dto.Specialization,
                UserId = dto.UserId
            };

            _context.Teachers.Add(teacher);

            await _context.SaveChangesAsync();

            await _auditLogService.CreateAsync(
                userid,
                "CREATE",
                "Teacher",
                teacher.Id,
                $"Tạo Teacher {teacher.FullName} - Email: {teacher.Email}",
                ipaddress);
            // =========================
            // NOTIFICATION
            // =========================

            // Người thực hiện
            if (userid.HasValue)
            {
                await _notificationService.CreateAsync(
                    new NotificationCreateDto
                    {
                        UserId = userid.Value,
                        Title = "Tạo giáo viên",
                        Message =
                            $"Bạn đã tạo giáo viên {teacher.FullName}.",
                        Type = "TEACHER"
                    });
            }

            // Teacher được tạo
            if (teacher.UserId.HasValue &&
                teacher.UserId.Value != userid.Value)
            {
                await _notificationService.CreateAsync(
                    new NotificationCreateDto
                    {
                        UserId = teacher.UserId.Value,
                        Title = "Tài khoản giáo viên",
                        Message =
                            $"Bạn đã được thêm vào hệ thống với vai trò giáo viên. " +
                            $"Chuyên môn: {teacher.Specialization}.",
                        Type = "TEACHER"
                    });
            }
            return new TeacherDto
            {
                Id = teacher.Id,
                FullName = teacher.FullName,
                Email = teacher.Email,
                Phone = teacher.Phone,
                Specialization = teacher.Specialization,
                UserId = teacher.UserId
            };
        }

        public async Task<bool> UpdateAsync(int id, TeacherUpdateDto dto)
        {
            // KIỂM TRA TEACHER
            var teacher = await _context.Teachers
                .FindAsync(id);

            if (teacher == null)
            {
                return false;
            }

            // FULL NAME
            if (string.IsNullOrWhiteSpace(dto.FullName))
            {
                throw new ArgumentException(
                    "Họ tên không được để trống.");
            }

            if (dto.FullName.Length > 100)
            {
                throw new ArgumentException(
                    "Họ tên không được vượt quá 100 ký tự.");
            }

            // EMAIL
            if (string.IsNullOrWhiteSpace(dto.Email))
            {
                throw new ArgumentException(
                    "Email không được để trống.");
            }

            if (!dto.Email.Contains("@"))
            {
                throw new ArgumentException(
                    "Email không hợp lệ.");
            }

            // PHONE
            if (string.IsNullOrWhiteSpace(dto.Phone))
            {
                throw new ArgumentException(
                    "Số điện thoại không được để trống.");
            }

            if (dto.Phone.Length < 9 || dto.Phone.Length > 15)
            {
                throw new ArgumentException(
                    "Số điện thoại không hợp lệ.");
            }

            // SPECIALIZATION
            if (string.IsNullOrWhiteSpace(dto.Specialization))
            {
                throw new ArgumentException(
                    "Chuyên môn không được để trống.");
            }

            // EMAIL TRÙNG
            var existedEmail = await _context.Teachers
                .AnyAsync(t =>
                    t.Id != id &&
                    t.Email == dto.Email);

            if (existedEmail)
            {
                throw new ArgumentException(
                    "Email đã tồn tại.");
            }

            // PHONE TRÙNG
            var existedPhone = await _context.Teachers
                .AnyAsync(t =>
                    t.Id != id &&
                    t.Phone == dto.Phone);

            if (existedPhone)
            {
                throw new ArgumentException(
                    "Số điện thoại đã tồn tại.");
            }

            // UPDATE
            teacher.FullName = dto.FullName;
            teacher.Email = dto.Email;
            teacher.Phone = dto.Phone;
            teacher.Specialization = dto.Specialization;
            teacher.UserId = dto.UserId;
            var oldUserId = teacher.UserId;
            await _context.SaveChangesAsync();

            await _auditLogService.CreateAsync(
                userid,
                "UPDATE",
                "Teacher",
                teacher.Id,
                $"Cập nhật thông tin Teacher {teacher.FullName}",
                ipaddress);
            // =========================
            // NOTIFICATION
            // =========================

            // Người thực hiện
            if (userid.HasValue)
            {
                await _notificationService.CreateAsync(
                    new NotificationCreateDto
                    {
                        UserId = userid.Value,
                        Title = "Cập nhật giáo viên",
                        Message =
                            $"Bạn đã cập nhật thông tin giáo viên {teacher.FullName}.",
                        Type = "TEACHER"
                    });
            }

            // UserId mới của Teacher
            if (teacher.UserId.HasValue &&
                teacher.UserId.Value != userid.Value)
            {
                await _notificationService.CreateAsync(
                    new NotificationCreateDto
                    {
                        UserId = teacher.UserId.Value,
                        Title = "Thông tin giáo viên được cập nhật",
                        Message =
                            $"Thông tin giáo viên {teacher.FullName} của bạn đã được cập nhật.",
                        Type = "TEACHER"
                    });
            }

            // Nếu UserId bị thay đổi thì thông báo User cũ
            if (oldUserId.HasValue &&
                oldUserId.Value != teacher.UserId &&
                oldUserId.Value != userid.Value)
            {
                await _notificationService.CreateAsync(
                    new NotificationCreateDto
                    {
                        UserId = oldUserId.Value,
                        Title = "Quyền giáo viên đã thay đổi",
                        Message =
                            $"Tài khoản của bạn không còn được gán với giáo viên {teacher.FullName}.",
                        Type = "TEACHER"
                    });
            }
            return true;
        }
        public async Task<bool> DeleteAsync(int id)
        {
            var teacher = await _context.Teachers
                .FirstOrDefaultAsync(t => t.Id == id);

            if (teacher == null)
            {
                return false;
            }
            var fullName = teacher.FullName;
            var email = teacher.Email;
            var teacherUserId = teacher.UserId;
            teacher.IsDeleted = true;

            await _context.SaveChangesAsync();

            await _auditLogService.CreateAsync(
                userid,
                "DELETE",
                "Teacher",
                id,
                $"Xóa Teacher {fullName} - Email: {email}",
                ipaddress);
            // =========================
            // NOTIFICATION
            // =========================

            // Người thực hiện
            if (userid.HasValue)
            {
                await _notificationService.CreateAsync(
                    new NotificationCreateDto
                    {
                        UserId = userid.Value,
                        Title = "Xóa giáo viên",
                        Message =
                            $"Bạn đã xóa giáo viên {fullName}.",
                        Type = "TEACHER"
                    });
            }

            // User của Teacher
            if (teacherUserId.HasValue &&
                teacherUserId.Value != userid.Value)
            {
                await _notificationService.CreateAsync(
                    new NotificationCreateDto
                    {
                        UserId = teacherUserId.Value,
                        Title = "Tài khoản giáo viên đã bị xóa",
                        Message =
                            $"Thông tin giáo viên {fullName} của bạn đã bị xóa khỏi hệ thống.",
                        Type = "TEACHER"
                    });
            }
            return true;
        }
    }
}