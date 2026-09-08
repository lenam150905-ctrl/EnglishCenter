using DocumentFormat.OpenXml.Spreadsheet;
using EnglishCenter.API.Data;
using EnglishCenter.API.DTOs;
using EnglishCenter.API.Middleware;
using EnglishCenter.API.Models;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace EnglishCenter.API.Services
{
    public class CourseService : ICourseService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditLogService _auditLogService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CourseService(
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
        public async Task<PagedResultDto<CourseDto>> GetAllAsync(
    string? search,
    decimal? minTuitionFee,
    decimal? maxTuitionFee,
    string? sortBy,
    bool sortDesc,
    int page,
    int pageSize)
{
    var query = _context.Courses.AsQueryable();

    // SEARCH
    if (!string.IsNullOrWhiteSpace(search))
    {
        query = query.Where(c =>
            c.CourseName.Contains(search) ||
            c.Description.Contains(search));
            }

    // FILTER - HỌC PHÍ
    if (minTuitionFee.HasValue)
    {
        query = query.Where(c =>
            c.TuitionFee >= minTuitionFee.Value);
    }

    if (maxTuitionFee.HasValue)
    {
        query = query.Where(c =>
            c.TuitionFee <= maxTuitionFee.Value);
    }

    // SORT
    if (!string.IsNullOrWhiteSpace(sortBy))
    {
        switch (sortBy.ToLower())
        {
            case "id":
                query = sortDesc
                    ? query.OrderByDescending(c => c.Id)
                    : query.OrderBy(c => c.Id);
                break;

            case "coursename":
                query = sortDesc
                    ? query.OrderByDescending(c => c.CourseName)
                    : query.OrderBy(c => c.CourseName);
                break;

            case "tuitionfee":
                query = sortDesc
                    ? query.OrderByDescending(c => c.TuitionFee)
                    : query.OrderBy(c => c.TuitionFee);
                break;

            case "duration":
                query = sortDesc
                    ? query.OrderByDescending(c => c.Duration)
                    : query.OrderBy(c => c.Duration);
                break;
        }
    }
    else
    {
        query = query.OrderBy(c => c.Id);
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

    var courses = await query
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();

    // DTO
    var data = courses.Select(c => new CourseDto
    {
        Id = c.Id,
        CourseName = c.CourseName,
        TuitionFee = c.TuitionFee,
        Duration = c.Duration,
        Description = c.Description,
        Status= c.Status

    }).ToList();

    return new PagedResultDto<CourseDto>
    {
        Data = data,
        Page = page,
        PageSize = pageSize,
        TotalItems = totalItems,
        TotalPages = totalPages
    };
}

        public async Task<CourseDto?> GetByIdAsync(int id)
        {
            var course = await _context.Courses
                .FirstOrDefaultAsync(c => c.Id == id);

            if (course == null)
            {
                return null;
            }

            return new CourseDto
            {
                Id = course.Id,
                CourseCode = course.CourseCode,
                CourseName = course.CourseName,
                TuitionFee = course.TuitionFee,
                Description = course.Description,
                Duration = course.Duration,
                Status = course.Status
            };
        }

        public async Task<CourseDto> CreateAsync(CourseCreateDto dto)
        {
            // COURSE NAME
            if (string.IsNullOrWhiteSpace(dto.CourseName))
            {
                throw new ArgumentException(
                    "Tên khóa học không được để trống.");
            }

            if (dto.CourseName.Length > 100)
            {
                throw new ArgumentException(
                    "Tên khóa học không được vượt quá 100 ký tự.");
            }

            // DESCRIPTION
            if (string.IsNullOrWhiteSpace(dto.Description))
            {
                throw new ArgumentException(
                    "Mô tả không được để trống.");
            }

            // TUITION FEE
            if (dto.TuitionFee <= 0)
            {
                throw new ArgumentException(
                    "Học phí phải lớn hơn 0.");
            }

            // DURATION
            if (dto.Duration <= 0)
            {
                throw new ArgumentException(
                    "Thời lượng khóa học phải lớn hơn 0.");
            }

            // CHECK COURSE NAME
            var existed = await _context.Courses
                .AnyAsync(c =>
                    c.CourseName == dto.CourseName);
            var validStatuses = new[]
{
    "Pending",
    "Active",
    "Completed"
};

            if (!validStatuses.Contains(dto.Status))
            {
                throw new ArgumentException(
                    "Trạng thái khóa học không hợp lệ.");
            }

            if (existed)
            {
                throw new ArgumentException(
                    "Tên khóa học đã tồn tại.");
            }

            var course = new Course
            {
                CourseName = dto.CourseName,
                Description = dto.Description,
                TuitionFee = dto.TuitionFee,
                Duration = dto.Duration,
                Status= dto.Status
            };

            _context.Courses.Add(course);

            await _context.SaveChangesAsync();
           


            await _auditLogService.CreateAsync(
                userid,
                "CREATE",
                "Course",
                course.Id,
                $"Tạo khóa học {course.CourseName}",
                ipaddress);

            return new CourseDto
            {
                Id = course.Id,
                CourseName = course.CourseName,
                CourseCode= course.CourseCode,
                Duration = course.Duration,
                TuitionFee = course.TuitionFee,
                Description = course.Description,
                Status= course.Status

            };
        }

        public async Task<bool> UpdateAsync(int id, CourseUpdateDto dto)
        {
            // KIỂM TRA COURSE
            var course = await _context.Courses
                .FindAsync(id);

            if (course == null)
            {
                return false;
            }

            // COURSE NAME
            if (string.IsNullOrWhiteSpace(dto.CourseName))
            {
                throw new ArgumentException(
                    "Tên khóa học không được để trống.");
            }

            if (dto.CourseName.Length > 100)
            {
                throw new ArgumentException(
                    "Tên khóa học không được vượt quá 100 ký tự.");
            }

            // DESCRIPTION
            if (string.IsNullOrWhiteSpace(dto.Description))
            {
                throw new ArgumentException(
                    "Mô tả không được để trống.");
            }

            // TUITION FEE
            if (dto.TuitionFee <= 0)
            {
                throw new ArgumentException(
                    "Học phí phải lớn hơn 0.");
            }

            // DURATION
            if (dto.Duration <= 0)
            {
                throw new ArgumentException(
                    "Thời lượng khóa học phải lớn hơn 0.");
            }

            // CHECK COURSE NAME TRÙNG
            var existed = await _context.Courses
                .AnyAsync(c =>
                    c.Id != id &&
                    c.CourseName == dto.CourseName);

            if (existed)
            {
                throw new ArgumentException(
                    "Tên khóa học đã tồn tại.");
            }

            // UPDATE
            course.CourseName = dto.CourseName;
            course.Description = dto.Description;
            course.CourseCode = dto.CourseCode;
            course.TuitionFee = dto.TuitionFee;
            course.Duration = dto.Duration;

            await _context.SaveChangesAsync();
     
       
            await _auditLogService.CreateAsync(
                userid,
                "UPDATE",
                "Course",
                course.Id,
                $"Cập nhật khóa học {course.CourseName}",
                ipaddress);

            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var course = await _context.Courses
                .FirstOrDefaultAsync(c => c.Id == id);

            if (course == null)
            {
                return false;
            }

            var courseName = course.CourseName;

            _context.Courses.Remove(course);

            await _context.SaveChangesAsync();
          
      
            await _auditLogService.CreateAsync(
                userid,
                "DELETE",
                "Course",
                id,
                $"Xóa khóa học {courseName}",
                ipaddress);

            return true;
        }
    }
}