using EnglishCenter.API.DTOs;
using EnglishCenter.API.Middleware;
using EnglishCenter.API.Models;
using EnglishCenter.Application.Abstractions.Persistence;

namespace EnglishCenter.API.Services
{
    public class CourseService : ICourseService
    {
        private readonly ICourseRepository _courseRepository;
        private readonly IAuditLogService _auditLogService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly INotificationService _notificationService;

        public CourseService(
            ICourseRepository courseRepository,
            IAuditLogService auditLogService,
            IHttpContextAccessor httpContextAccessor,
            INotificationService notificationService)
        {
            _courseRepository = courseRepository;
            _auditLogService = auditLogService;
            _httpContextAccessor = httpContextAccessor;
            _notificationService = notificationService;
        }

        private int? userid => AuditContext.GetUserId(_httpContextAccessor.HttpContext!);
        private string? ipaddress => AuditContext.GetIPAddress(_httpContextAccessor.HttpContext!);

        public async Task<PagedResultDto<CourseDto>> GetAllAsync(
            string? search,
            decimal? minTuitionFee,
            decimal? maxTuitionFee,
            string? sortBy,
            bool sortDesc,
            int page,
            int pageSize)
        {
            var (courses, totalItems) = await _courseRepository.GetAllAsync(
                search, minTuitionFee, maxTuitionFee, sortBy, sortDesc, page, pageSize);

            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 20 : pageSize;
            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            return new PagedResultDto<CourseDto>
            {
                Data = courses.Select(c => new CourseDto
                {
                    Id = c.Id,
                    CourseCode = c.CourseCode,
                    CourseName = c.CourseName,
                    Description = c.Description,
                    Duration = c.Duration,
                    TuitionFee = c.TuitionFee,
                    Status = c.Status
                }).ToList(),
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages
            };
        }

        public async Task<CourseDto?> GetByIdAsync(int id)
        {
            var course = await _courseRepository.GetByIdAsync(id);
            if (course == null)
            {
                return null;
            }

            return new CourseDto
            {
                Id = course.Id,
                CourseCode = course.CourseCode,
                CourseName = course.CourseName,
                Description = course.Description,
                Duration = course.Duration,
                TuitionFee = course.TuitionFee,
                Status = course.Status
            };
        }

        public async Task<CourseDto> CreateAsync(CourseCreateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.CourseName))
            {
                throw new ArgumentException("Tên khóa học không được để trống.");
            }

            if (dto.Duration <= 0)
            {
                throw new ArgumentException("Thời lượng khóa học phải lớn hơn 0.");
            }

            if (dto.TuitionFee < 0)
            {
                throw new ArgumentException("Học phí không được âm.");
            }

            var course = new Course
            {
                CourseCode = dto.CourseCode,
                CourseName = dto.CourseName,
                Description = dto.Description,
                Duration = dto.Duration,
                TuitionFee = dto.TuitionFee,
                Status = dto.Status
            };

            await _courseRepository.CreateAsync(course);

            await _auditLogService.CreateAsync(
                userid,
                "CREATE",
                "Course",
                course.Id,
                $"Tạo khóa học {course.CourseName} - Mã: {course.CourseCode}",
                ipaddress);

            if (userid.HasValue)
            {
                await _notificationService.CreateAsync(new NotificationCreateDto
                {
                    UserId = userid.Value,
                    Title = "Tạo khóa học",
                    Message = $"Bạn đã tạo khóa học {course.CourseName}.",
                    Type = "COURSE"
                });
            }

            return new CourseDto
            {
                Id = course.Id,
                CourseCode = course.CourseCode,
                CourseName = course.CourseName,
                Description = course.Description,
                Duration = course.Duration,
                TuitionFee = course.TuitionFee,
                Status = course.Status
            };
        }

        public async Task<bool> UpdateAsync(int id, CourseUpdateDto dto)
        {
            var course = await _courseRepository.GetByIdAsync(id);
            if (course == null)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(dto.CourseName))
            {
                throw new ArgumentException("Tên khóa học không được để trống.");
            }

            if (dto.Duration <= 0)
            {
                throw new ArgumentException("Thời lượng khóa học phải lớn hơn 0.");
            }

            if (dto.TuitionFee < 0)
            {
                throw new ArgumentException("Học phí không được âm.");
            }

            course.CourseCode = dto.CourseCode;
            course.CourseName = dto.CourseName;
            course.Description = dto.Description;
            course.Duration = dto.Duration;
            course.TuitionFee = dto.TuitionFee;
            course.Status = dto.Status;

            await _courseRepository.UpdateAsync(course);

            await _auditLogService.CreateAsync(
                userid,
                "UPDATE",
                "Course",
                course.Id,
                $"Cập nhật khóa học {course.CourseName} - Mã: {course.CourseCode}",
                ipaddress);

            if (userid.HasValue)
            {
                await _notificationService.CreateAsync(new NotificationCreateDto
                {
                    UserId = userid.Value,
                    Title = "Cập nhật khóa học",
                    Message = $"Bạn đã cập nhật khóa học {course.CourseName}.",
                    Type = "COURSE"
                });
            }

            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var course = await _courseRepository.GetByIdAsync(id);
            if (course == null)
            {
                return false;
            }

            var courseName = course.CourseName;
            var courseCode = course.CourseCode;

            await _courseRepository.SoftDeleteAsync(id);

            await _auditLogService.CreateAsync(
                userid,
                "DELETE",
                "Course",
                id,
                $"Xóa khóa học {courseName} - Mã: {courseCode}",
                ipaddress);

            if (userid.HasValue)
            {
                await _notificationService.CreateAsync(new NotificationCreateDto
                {
                    UserId = userid.Value,
                    Title = "Xóa khóa học",
                    Message = $"Bạn đã xóa khóa học {courseName}.",
                    Type = "COURSE"
                });
            }

            return true;
        }
    }
}