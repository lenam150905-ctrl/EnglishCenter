using EnglishCenter.API.DTOs;
using EnglishCenter.API.Middleware;
using EnglishCenter.API.Models;
using EnglishCenter.Application.Abstractions.Persistence;

namespace EnglishCenter.API.Services
{
    public class ScheduleService : IScheduleService
    {
        private readonly IScheduleRepository _scheduleRepository;
        private readonly ICourseRepository _courseRepository;
        private readonly ITeacherRepository _teacherRepository;
        private readonly IEnrollmentRepository _enrollmentRepository;
        private readonly IAuditLogService _auditLogService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly INotificationService _notificationService;

        public ScheduleService(
            IScheduleRepository scheduleRepository,
            ICourseRepository courseRepository,
            ITeacherRepository teacherRepository,
            IEnrollmentRepository enrollmentRepository,
            IAuditLogService auditLogService,
            IHttpContextAccessor httpContextAccessor,
            INotificationService notificationService)
        {
            _scheduleRepository = scheduleRepository;
            _courseRepository = courseRepository;
            _teacherRepository = teacherRepository;
            _enrollmentRepository = enrollmentRepository;
            _auditLogService = auditLogService;
            _httpContextAccessor = httpContextAccessor;
            _notificationService = notificationService;
        }

        private int? userid => AuditContext.GetUserId(_httpContextAccessor.HttpContext!);
        private string? ipaddress => AuditContext.GetIPAddress(_httpContextAccessor.HttpContext!);

        public async Task<PagedResultDto<ScheduleDto>> GetAllAsync(
            string? search,
            int? courseId,
            int? teacherId,
            string? sortBy,
            bool sortDesc,
            int page,
            int pageSize)
        {
            var (schedules, totalItems) = await _scheduleRepository.GetAllAsync(
                search, courseId, teacherId, sortBy, sortDesc, page, pageSize);

            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 20 : pageSize;
            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            return new PagedResultDto<ScheduleDto>
            {
                Data = schedules.Select(s => new ScheduleDto
                {
                    Id = s.Id,
                    CourseId = s.CourseId,
                    CourseName = s.Course?.CourseName ?? string.Empty,
                    TeacherId = s.TeacherId,
                    TeacherName = s.Teacher?.FullName ?? string.Empty,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                    Room = s.Room
                }).ToList(),
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages
            };
        }

        public async Task<ScheduleDto?> GetByIdAsync(int id)
        {
            var schedule = await _scheduleRepository.GetByIdAsync(id);
            if (schedule == null)
            {
                return null;
            }

            return new ScheduleDto
            {
                Id = schedule.Id,
                CourseId = schedule.CourseId,
                CourseName = schedule.Course?.CourseName ?? string.Empty,
                TeacherId = schedule.TeacherId,
                TeacherName = schedule.Teacher?.FullName ?? string.Empty,
                StartTime = schedule.StartTime,
                EndTime = schedule.EndTime,
                Room = schedule.Room
            };
        }

        public async Task<ScheduleDto> CreateAsync(ScheduleCreateDto dto)
        {
            var courseExists = await _courseRepository.ExistsAsync(dto.CourseId);
            if (!courseExists)
            {
                throw new ArgumentException("Course không tồn tại.");
            }

            var hasPaidStudent = await _enrollmentRepository.HasActiveEnrollmentForCourseAsync(dto.CourseId);
            if (!hasPaidStudent)
            {
                throw new ArgumentException("Chưa có học sinh đăng ký và thanh toán khóa học.");
            }

            var teacherExists = await _teacherRepository.ExistsAsync(dto.TeacherId);
            if (!teacherExists)
            {
                throw new ArgumentException("Teacher không tồn tại.");
            }

            if (dto.StartTime >= dto.EndTime)
            {
                throw new ArgumentException("Thời gian bắt đầu phải nhỏ hơn thời gian kết thúc.");
            }

            if (string.IsNullOrWhiteSpace(dto.Room))
            {
                throw new ArgumentException("Phòng học không được để trống.");
            }

            if (dto.Room.Length > 50)
            {
                throw new ArgumentException("Tên phòng không được vượt quá 50 ký tự.");
            }

            var teacherBusy = await _scheduleRepository.HasTeacherConflictAsync(dto.TeacherId, dto.StartTime, dto.EndTime);
            if (teacherBusy)
            {
                throw new ArgumentException("Teacher đã có lịch trong khoảng thời gian này.");
            }

            var roomBusy = await _scheduleRepository.HasRoomConflictAsync(dto.Room, dto.StartTime, dto.EndTime);
            if (roomBusy)
            {
                throw new ArgumentException("Phòng học đã được sử dụng trong khoảng thời gian này.");
            }

            var schedule = new Schedule
            {
                CourseId = dto.CourseId,
                TeacherId = dto.TeacherId,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                Room = dto.Room
            };

            await _scheduleRepository.CreateAsync(schedule);

            var created = await _scheduleRepository.GetByIdAsync(schedule.Id);

            await _auditLogService.CreateAsync(
                userid,
                "CREATE",
                "Schedule",
                schedule.Id,
                $"Tạo lịch học môn {created?.Course?.CourseName} - Giáo viên: {created?.Teacher?.FullName}, Phòng: {schedule.Room}",
                ipaddress);

            if (userid.HasValue)
            {
                await _notificationService.CreateAsync(new NotificationCreateDto
                {
                    UserId = userid.Value,
                    Title = "Tạo lịch học",
                    Message = $"Bạn đã tạo lịch học cho khóa học {created?.Course?.CourseName}.",
                    Type = "SCHEDULE"
                });
            }

            if (created?.Teacher?.UserId.HasValue == true && created.Teacher.UserId != userid)
            {
                await _notificationService.CreateAsync(new NotificationCreateDto
                {
                    UserId = created.Teacher.UserId.Value,
                    Title = "Lịch dạy mới",
                    Message = $"Bạn có lịch dạy mới cho khóa học {created?.Course?.CourseName} tại phòng {schedule.Room}.",
                    Type = "SCHEDULE"
                });
            }

            return new ScheduleDto
            {
                Id = schedule.Id,
                CourseId = schedule.CourseId,
                CourseName = created?.Course?.CourseName ?? string.Empty,
                TeacherId = schedule.TeacherId,
                TeacherName = created?.Teacher?.FullName ?? string.Empty,
                StartTime = schedule.StartTime,
                EndTime = schedule.EndTime,
                Room = schedule.Room
            };
        }

        public async Task<bool> UpdateAsync(int id, ScheduleUpdateDto dto)
        {
            var schedule = await _scheduleRepository.GetByIdAsync(id);
            if (schedule == null)
            {
                return false;
            }

            var courseExists = await _courseRepository.ExistsAsync(dto.CourseId);
            if (!courseExists)
            {
                throw new ArgumentException("Course không tồn tại.");
            }

            var teacherExists = await _teacherRepository.ExistsAsync(dto.TeacherId);
            if (!teacherExists)
            {
                throw new ArgumentException("Teacher không tồn tại.");
            }

            if (dto.StartTime >= dto.EndTime)
            {
                throw new ArgumentException("Thời gian bắt đầu phải nhỏ hơn thời gian kết thúc.");
            }

            if (string.IsNullOrWhiteSpace(dto.Room))
            {
                throw new ArgumentException("Phòng học không được để trống.");
            }

            if (dto.Room.Length > 50)
            {
                throw new ArgumentException("Tên phòng không được vượt quá 50 ký tự.");
            }

            var teacherBusy = await _scheduleRepository.HasTeacherConflictAsync(dto.TeacherId, dto.StartTime, dto.EndTime, id);
            if (teacherBusy)
            {
                throw new ArgumentException("Teacher đã có lịch trong khoảng thời gian này.");
            }

            var roomBusy = await _scheduleRepository.HasRoomConflictAsync(dto.Room, dto.StartTime, dto.EndTime, id);
            if (roomBusy)
            {
                throw new ArgumentException("Phòng học đã được sử dụng trong khoảng thời gian này.");
            }

            schedule.CourseId = dto.CourseId;
            schedule.TeacherId = dto.TeacherId;
            schedule.StartTime = dto.StartTime;
            schedule.EndTime = dto.EndTime;
            schedule.Room = dto.Room;

            await _scheduleRepository.UpdateAsync(schedule);

            var updated = await _scheduleRepository.GetByIdAsync(id);

            await _auditLogService.CreateAsync(
                userid,
                "UPDATE",
                "Schedule",
                schedule.Id,
                $"Cập nhật lịch học môn {updated?.Course?.CourseName} - Giáo viên: {updated?.Teacher?.FullName}, Phòng: {schedule.Room}",
                ipaddress);

            if (userid.HasValue)
            {
                await _notificationService.CreateAsync(new NotificationCreateDto
                {
                    UserId = userid.Value,
                    Title = "Cập nhật lịch học",
                    Message = $"Bạn đã cập nhật lịch học cho khóa học {updated?.Course?.CourseName}.",
                    Type = "SCHEDULE"
                });
            }

            if (updated?.Teacher?.UserId.HasValue == true && updated.Teacher.UserId != userid)
            {
                await _notificationService.CreateAsync(new NotificationCreateDto
                {
                    UserId = updated.Teacher.UserId.Value,
                    Title = "Lịch dạy cập nhật",
                    Message = $"Lịch dạy khóa học {updated?.Course?.CourseName} của bạn đã được cập nhật.",
                    Type = "SCHEDULE"
                });
            }

            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var schedule = await _scheduleRepository.GetByIdAsync(id);
            if (schedule == null)
            {
                return false;
            }

            var courseName = schedule.Course?.CourseName;
            var teacherName = schedule.Teacher?.FullName;
            var room = schedule.Room;
            var teacherUserId = schedule.Teacher?.UserId;

            await _scheduleRepository.SoftDeleteAsync(id);

            await _auditLogService.CreateAsync(
                userid,
                "DELETE",
                "Schedule",
                id,
                $"Xóa lịch học môn {courseName} - Giáo viên: {teacherName}, Phòng: {room}",
                ipaddress);

            if (userid.HasValue)
            {
                await _notificationService.CreateAsync(new NotificationCreateDto
                {
                    UserId = userid.Value,
                    Title = "Xóa lịch học",
                    Message = $"Bạn đã xóa lịch học của khóa học {courseName}.",
                    Type = "SCHEDULE"
                });
            }

            if (teacherUserId.HasValue && teacherUserId.Value != userid)
            {
                await _notificationService.CreateAsync(new NotificationCreateDto
                {
                    UserId = teacherUserId.Value,
                    Title = "Lịch dạy đã bị xóa",
                    Message = $"Lịch dạy khóa học {courseName} tại phòng {room} đã bị hủy.",
                    Type = "SCHEDULE"
                });
            }

            return true;
        }
    }
}