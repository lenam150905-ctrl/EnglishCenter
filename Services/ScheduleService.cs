using DocumentFormat.OpenXml.Office2010.Excel;
using EnglishCenter.API.Data;
using EnglishCenter.API.DTOs;
using EnglishCenter.API.Middleware;
using EnglishCenter.API.Models;
using Microsoft.EntityFrameworkCore;

namespace EnglishCenter.API.Services
{
    public class ScheduleService : IScheduleService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditLogService _auditLogService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly INotificationService _notificationService;

        public ScheduleService(
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
        public async Task<PagedResultDto<ScheduleDto>> GetAllAsync(
     string? search,
     int? courseId,
     int? teacherId,
     string? sortBy,
     bool sortDesc,
     int page,
     int pageSize)
        {
            var query = _context.Schedules
                .Include(s => s.Course)
                .Include(s => s.Teacher)
                .AsQueryable();

            // SEARCH
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(s =>
                    s.Room.Contains(search) ||
                    s.Course.CourseName.Contains(search) ||
                    s.Teacher.FullName.Contains(search));
            }

            // FILTER
            if (courseId.HasValue)
            {
                query = query.Where(s =>
                    s.CourseId == courseId.Value);
            }

            if (teacherId.HasValue)
            {
                query = query.Where(s =>
                    s.TeacherId == teacherId.Value);
            }

            // SORT
            if (!string.IsNullOrWhiteSpace(sortBy))
            {
                switch (sortBy.ToLower())
                {
                    case "id":
                        query = sortDesc
                            ? query.OrderByDescending(s => s.Id)
                            : query.OrderBy(s => s.Id);
                        break;

                    case "starttime":
                        query = sortDesc
                            ? query.OrderByDescending(s => s.StartTime)
                            : query.OrderBy(s => s.StartTime);
                        break;

                    case "endtime":
                        query = sortDesc
                            ? query.OrderByDescending(s => s.EndTime)
                            : query.OrderBy(s => s.EndTime);
                        break;

                    case "room":
                        query = sortDesc
                            ? query.OrderByDescending(s => s.Room)
                            : query.OrderBy(s => s.Room);
                        break;
                }
            }
            else
            {
                query = query.OrderBy(s => s.StartTime);
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

            var schedules = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // DTO
            var data = schedules.Select(s => new ScheduleDto
            {
                Id = s.Id,
                CourseId = s.CourseId,
                CourseName = s.Course.CourseName,
                TeacherId = s.TeacherId,
                TeacherName = s.Teacher.FullName,
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                Room = s.Room
            }).ToList();

            return new PagedResultDto<ScheduleDto>
            {
                Data = data,
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages
            };
        }
        public async Task<ScheduleDto?> GetByIdAsync(int id)
        {
            var schedule = await _context.Schedules
                .Include(s => s.Course)
                .Include(s => s.Teacher)
                .FirstOrDefaultAsync(s => s.Id == id);
            if (schedule == null)
            {
                return null;
            }
            return new ScheduleDto
            {
                Id = schedule.Id,
                CourseId = schedule.CourseId,
                CourseName = schedule.Course.CourseName,
                TeacherId = schedule.TeacherId,
                TeacherName = schedule.Teacher.FullName,
                StartTime = schedule.StartTime,
                EndTime = schedule.EndTime,
                Room = schedule.Room
            };
        }
        public async Task<ScheduleDto> CreateAsync(
     ScheduleCreateDto dto)
        {
            // COURSE
            var courseExists = await _context.Courses
                .AnyAsync(c => c.Id == dto.CourseId);

            if (!courseExists)
            {
                throw new ArgumentException(
                    "Course không tồn tại.");
            }
            // KIỂM TRA HỌC SINH ĐÃ ĐĂNG KÝ + THANH TOÁN
            var hasPaidStudent = await _context.Enrollments
                .AnyAsync(e =>
                    e.CourseId == dto.CourseId &&
                    e.Status == "Active");

            if (!hasPaidStudent)
            {
                throw new ArgumentException(
                    "Chưa có học sinh đăng ký và thanh toán khóa học.");
            }
            // TEACHER
            var teacherExists = await _context.Teachers
                .AnyAsync(t => t.Id == dto.TeacherId);

            if (!teacherExists)
            {
                throw new ArgumentException(
                    "Teacher không tồn tại.");
            }

            // START TIME
            if (dto.StartTime >= dto.EndTime)
            {
                throw new ArgumentException(
                    "Thời gian bắt đầu phải nhỏ hơn thời gian kết thúc.");
            }

            // ROOM
            if (string.IsNullOrWhiteSpace(dto.Room))
            {
                throw new ArgumentException(
                    "Phòng học không được để trống.");
            }

            if (dto.Room.Length > 50)
            {
                throw new ArgumentException(
                    "Tên phòng không được vượt quá 50 ký tự.");
            }

            // CHECK TEACHER TRÙNG LỊCH
            var teacherBusy = await _context.Schedules
                .AnyAsync(s =>
                    s.TeacherId == dto.TeacherId &&
                    dto.StartTime < s.EndTime &&
                    dto.EndTime > s.StartTime);

            if (teacherBusy)
            {
                throw new ArgumentException(
                    "Teacher đã có lịch trong khoảng thời gian này.");
            }

            // CHECK ROOM TRÙNG LỊCH
            var roomBusy = await _context.Schedules
                .AnyAsync(s =>
                    s.Room == dto.Room &&
                    dto.StartTime < s.EndTime &&
                    dto.EndTime > s.StartTime);

            if (roomBusy)
            {
                throw new ArgumentException(
                    "Phòng học đã được sử dụng trong khoảng thời gian này.");
            }
            var existed = await _context.Schedules
      .AnyAsync(s =>
          s.TeacherId == dto.TeacherId &&
          dto.StartTime < s.EndTime &&
          dto.EndTime > s.StartTime);

            if (existed)
            {
                throw new ArgumentException(
                    "Giáo viên đã có lịch trong khoảng thời gian này.");
            }

            var schedule = new Schedule
            {
                CourseId = dto.CourseId,
                TeacherId = dto.TeacherId,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                Room = dto.Room
            };

            _context.Schedules.Add(schedule);

            await _context.SaveChangesAsync();
            await _auditLogService.CreateAsync(
    userid,
    "CREATE",
    "Schedule",
    schedule.Id,
    $"Tạo lịch học Course ID {schedule.CourseId}, Teacher ID {schedule.TeacherId}, phòng {schedule.Room}",
    ipaddress);
            if (userid.HasValue)
            {
                await _notificationService.CreateAsync(
                    new NotificationCreateDto
                    {
                        UserId = userid.Value,
                        Title = "Tạo lịch học",
                        Message =
                            $"Bạn đã tạo lịch học cho khóa học " +
                            $"#{schedule.CourseId}, phòng {schedule.Room}.",
                        Type = "SCHEDULE"
                    });
            }

            // LẤY USER ID CỦA TEACHER
            var teacherUserId = await _context.Teachers
                .Where(t => t.Id == schedule.TeacherId)
                .Select(t => (int?)t.UserId)
                .FirstOrDefaultAsync();

            // THÔNG BÁO TEACHER
            if (teacherUserId.HasValue &&
                teacherUserId.Value != userid.Value)
            {
                await _notificationService.CreateAsync(
                    new NotificationCreateDto
                    {
                        UserId = teacherUserId.Value,
                        Title = "Lịch dạy mới",
                        Message =
                            $"Bạn có lịch dạy mới cho khóa học " +
                            $"#{schedule.CourseId}, " +
                            $"phòng {schedule.Room}. " +
                            $"Thời gian: {schedule.StartTime:dd/MM/yyyy HH:mm} - " +
                            $"{schedule.EndTime:HH:mm}.",
                        Type = "SCHEDULE"
                    });
            }
            var studentUserIds = await _context.Enrollments
    .Where(e =>
        e.CourseId == schedule.CourseId &&
        e.Status == "Active")
    .Select(e => e.Student.UserId)
    .Distinct()
    .ToListAsync();

            foreach (var studentUserId in studentUserIds)
            {
                if (studentUserId != userid.Value)
                {
                    await _notificationService.CreateAsync(
                        new NotificationCreateDto
                        {
                            UserId = studentUserId.Value,
                            Title = "Lịch học mới",
                            Message =
                                $"Khóa học #{schedule.CourseId} có lịch học mới. " +
                                $"Phòng: {schedule.Room}. " +
                                $"Thời gian: {schedule.StartTime:dd/MM/yyyy HH:mm} - " +
                                $"{schedule.EndTime:HH:mm}.",
                            Type = "SCHEDULE"
                        });
                }
            }
            return new ScheduleDto
            {
                Id = schedule.Id,
                CourseId = schedule.CourseId,
                TeacherId = schedule.TeacherId,
                StartTime = schedule.StartTime,
                EndTime = schedule.EndTime,
                Room = schedule.Room
            };
        }
        public async Task<bool> UpdateAsync(
     int id,
     ScheduleUpdateDto dto)
        {
            // KIỂM TRA SCHEDULE
            var schedule = await _context.Schedules
                .FindAsync(id);

            if (schedule == null)
            {
                return false;
            }

            // COURSE
            var courseExists = await _context.Courses
                .AnyAsync(c => c.Id == dto.CourseId);

            if (!courseExists)
            {
                throw new ArgumentException(
                    "Course không tồn tại.");
            }
            var hasPaidStudent = await _context.Enrollments
             .AnyAsync(e =>
                 e.CourseId == dto.CourseId &&
                 e.Status == "Active");

            if (!hasPaidStudent)
            {
                throw new ArgumentException(
                    "Chưa có học sinh đăng ký và thanh toán khóa học.");
            }
                // TEACHER
                var teacherExists = await _context.Teachers
                .AnyAsync(t => t.Id == dto.TeacherId);

            if (!teacherExists)
            {
                throw new ArgumentException(
                    "Teacher không tồn tại.");
            }

            // TIME
            if (dto.StartTime >= dto.EndTime)
            {
                throw new ArgumentException(
                    "Thời gian bắt đầu phải nhỏ hơn thời gian kết thúc.");
            }

            // ROOM
            if (string.IsNullOrWhiteSpace(dto.Room))
            {
                throw new ArgumentException(
                    "Phòng học không được để trống.");
            }

            if (dto.Room.Length > 50)
            {
                throw new ArgumentException(
                    "Tên phòng không được vượt quá 50 ký tự.");
            }


            // TEACHER TRÙNG LỊCH
            var teacherBusy = await _context.Schedules
                .AnyAsync(s =>
                    s.Id != id &&
                    s.TeacherId == dto.TeacherId &&
                    dto.StartTime < s.EndTime &&
                    dto.EndTime > s.StartTime);

            if (teacherBusy)
            {
                throw new ArgumentException(
                    "Teacher đã có lịch trong khoảng thời gian này.");
            }

            // ROOM TRÙNG LỊCH
            var roomBusy = await _context.Schedules
                .AnyAsync(s =>
                    s.Id != id &&
                    s.Room == dto.Room &&
                    dto.StartTime < s.EndTime &&
                    dto.EndTime > s.StartTime);

            if (roomBusy)
            {
                throw new ArgumentException(
                    "Phòng học đã được sử dụng trong khoảng thời gian này.");
            }

            // UPDATE
            schedule.CourseId = dto.CourseId;
            schedule.TeacherId = dto.TeacherId;
            schedule.StartTime = dto.StartTime;
            schedule.EndTime = dto.EndTime;
            schedule.Room = dto.Room;
            await _context.SaveChangesAsync();

            await _auditLogService.CreateAsync(
                userid,
                "UPDATE",
                "Schedule",
                schedule.Id,
                $"Cập nhật lịch học Course ID {schedule.CourseId}, Teacher ID {schedule.TeacherId}, phòng {schedule.Room}",
                ipaddress);
            if (userid.HasValue)
            {
                await _notificationService.CreateAsync(
                    new NotificationCreateDto
                    {
                        UserId = userid.Value,
                        Title = "Cập nhật lịch học",
                        Message =
                            $"Bạn đã cập nhật lịch học cho khóa học " +
                            $"#{schedule.CourseId}, phòng {schedule.Room}.",
                        Type = "SCHEDULE"
                    });
            }
            // LẤY USER ID TEACHER
            var teacherUserId = await _context.Teachers
                .Where(t => t.Id == schedule.TeacherId)
                .Select(t => (int?)t.UserId)
                .FirstOrDefaultAsync();

            // THÔNG BÁO TEACHER
            if (teacherUserId.HasValue &&
                teacherUserId.Value != userid.Value)
            {
                await _notificationService.CreateAsync(
                    new NotificationCreateDto
                    {
                        UserId = teacherUserId.Value,
                        Title = "Lịch dạy được cập nhật",
                        Message =
                            $"Lịch dạy của bạn đã được cập nhật. " +
                            $"Khóa học: #{schedule.CourseId}, " +
                            $"phòng: {schedule.Room}, " +
                            $"thời gian: {schedule.StartTime:dd/MM/yyyy HH:mm} - " +
                            $"{schedule.EndTime:HH:mm}.",
                        Type = "SCHEDULE"
                    });
            }
            var studentUserIds = await _context.Enrollments
    .Where(e =>
        e.CourseId == schedule.CourseId &&
        e.Status == "Active")
    .Select(e => e.Student.UserId)
    .Distinct()
    .ToListAsync();

            foreach (var studentUserId in studentUserIds)
            {
                if (studentUserId != userid.Value)
                {
                    await _notificationService.CreateAsync(
                        new NotificationCreateDto
                        {
                            UserId = studentUserId.Value,
                            Title = "Lịch học được cập nhật",
                            Message =
                                $"Lịch học khóa học #{schedule.CourseId} đã được cập nhật. " +
                                $"Phòng: {schedule.Room}. " +
                                $"Thời gian: {schedule.StartTime:dd/MM/yyyy HH:mm} - " +
                                $"{schedule.EndTime:HH:mm}.",
                            Type = "SCHEDULE"
                        });
                }
            }
            return true;

        }
        public async Task<bool> DeleteAsync(int id)
        {
            var schedule = await _context.Schedules.FindAsync(id);
            if (schedule == null)
            {
                return false;
            }
            var courseId = schedule.CourseId;
            var teacherId = schedule.TeacherId;
            var room = schedule.Room;

            schedule.IsDeleted = true;

            await _context.SaveChangesAsync();

            await _auditLogService.CreateAsync(
                userid,
                "DELETE",
                "Schedule",
                id,
                $"Xóa lịch học Course ID {courseId}, Teacher ID {teacherId}, phòng {room}",
                ipaddress);
            if (userid.HasValue)
            {
                await _notificationService.CreateAsync(
                    new NotificationCreateDto
                    {
                        UserId = userid.Value,
                        Title = "Xóa lịch học",
                        Message =
                            $"Bạn đã xóa lịch học cho khóa học " +
                            $"#{schedule.CourseId}, phòng {schedule.Room}.",
                        Type = "SCHEDULE"
                    });
            }
            var teacherUserId = await _context.Teachers
    .Where(t => t.Id == teacherId)
    .Select(t => (int?)t.UserId)
    .FirstOrDefaultAsync();

            var studentUserIds = await _context.Enrollments
                .Where(e =>
                    e.CourseId == courseId &&
                    e.Status == "Active")
                .Select(e => e.Student.UserId)
                .Distinct()
                .ToListAsync();
            // THÔNG BÁO TEACHER
            if (teacherUserId.HasValue &&
                teacherUserId.Value != userid.Value)
            {
                await _notificationService.CreateAsync(
                    new NotificationCreateDto
                    {
                        UserId = teacherUserId.Value,
                        Title = "Lịch dạy đã bị xóa",
                        Message =
                            $"Lịch dạy của bạn cho khóa học #{courseId} " +
                            $"tại phòng {room} đã bị xóa.",
                        Type = "SCHEDULE"
                    });
            }

            // THÔNG BÁO STUDENT
            foreach (var studentUserId in studentUserIds)
            {
                if (studentUserId != userid.Value)
                {
                    await _notificationService.CreateAsync(
                        new NotificationCreateDto
                        {
                            UserId = studentUserId.Value,
                            Title = "Lịch học đã bị xóa",
                            Message =
                                $"Lịch học của khóa học #{courseId} " +
                                $"tại phòng {room} đã bị xóa.",
                            Type = "SCHEDULE"
                        });
                }
            }
            return true;
        }
    }
}