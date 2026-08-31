using EnglishCenter.API.Data;
using EnglishCenter.API.Models;
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

        public async Task<List<Course>> GetAllAsync()
        {
            return await _context.Courses.ToListAsync();
        }

        public async Task<Course?> GetByIdAsync(int id)
        {
            return await _context.Courses
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<Course> CreateAsync(Course course)
        {
            _context.Courses.Add(course);

            await _context.SaveChangesAsync();

            return course;
        }

        public async Task<bool> UpdateAsync(int id, Course course)
        {
            var existingCourse = await _context.Courses
                .FirstOrDefaultAsync(c => c.Id == id);

            if (existingCourse == null)
            {
                return false;
            }

            existingCourse.CourseCode = course.CourseCode;
            existingCourse.CourseName = course.CourseName;
            existingCourse.TuitionFee = course.TuitionFee;

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