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

        public async Task<List<CourseDto>> GetAllAsync()
        {
            var courses = await _context.Courses.ToListAsync();

            return courses.Select(c => new CourseDto
            {
                Id = c.Id,
                CourseCode = c.CourseCode,
                CourseName = c.CourseName,
                TuitionFee = c.TuitionFee
            }).ToList();
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