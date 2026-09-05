using EnglishCenter.API.Data;
using EnglishCenter.API.DTOs;
using Microsoft.EntityFrameworkCore;

namespace EnglishCenter.API.Services
{
    public class CourseService : ICourseService
    {
        private readonly ApplicationDbContext _context;

        public CourseService(ApplicationDbContext context)
        {
            _context = context;
        }

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
        TuitionFee = c.TuitionFee
      
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
                TuitionFee = course.TuitionFee
            };
        }

        public async Task<CourseDto> CreateAsync(CourseCreateDto dto)
        {
            var course = new Models.Course
            {
                CourseCode = dto.CourseCode,
                CourseName = dto.CourseName,
                TuitionFee = dto.TuitionFee
            };

            _context.Courses.Add(course);

            await _context.SaveChangesAsync();

            return new CourseDto
            {
                Id = course.Id,
                CourseCode = course.CourseCode,
                CourseName = course.CourseName,
                TuitionFee = course.TuitionFee
            };
        }

        public async Task<bool> UpdateAsync(
            int id,
            CourseUpdateDto dto)
        {
            var course = await _context.Courses
                .FirstOrDefaultAsync(c => c.Id == id);

            if (course == null)
            {
                return false;
            }

            course.CourseCode = dto.CourseCode;
            course.CourseName = dto.CourseName;
            course.TuitionFee = dto.TuitionFee;

            await _context.SaveChangesAsync();

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

            _context.Courses.Remove(course);

            await _context.SaveChangesAsync();

            return true;
        }
    }
}