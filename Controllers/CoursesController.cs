using EnglishCenter.API.Data;
using EnglishCenter.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnglishCenter.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CoursesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CoursesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/courses
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Course>>> GetCourses()
        {
            var courses = await _context.Courses.ToListAsync();

            return Ok(courses);
        }

        // GET: api/courses/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Course>> GetCourse(int id)
        {
            var course = await _context.Courses
                .FirstOrDefaultAsync(c => c.Id == id);

            if (course == null)
            {
                return NotFound();
            }

            return Ok(course);
        }

        // POST: api/courses
        [HttpPost]
        public async Task<ActionResult<Course>> CreateCourse(
            Course course)
        {
            _context.Courses.Add(course);

            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetCourse),
                new { id = course.Id },
                course);
        }

        // PUT: api/courses/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCourse(
            int id,
            Course course)
        {
            if (id != course.Id)
            {
                return BadRequest();
            }

            var existingCourse = await _context.Courses
                .FirstOrDefaultAsync(c => c.Id == id);

            if (existingCourse == null)
            {
                return NotFound();
            }

            existingCourse.CourseCode = course.CourseCode;
            existingCourse.CourseName = course.CourseName;
            existingCourse.Description = course.Description;
            existingCourse.Duration = course.Duration;
            existingCourse.TuitionFee = course.TuitionFee;
            existingCourse.IsActive = course.IsActive;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/courses/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCourse(int id)
        {
            var course = await _context.Courses
                .FirstOrDefaultAsync(c => c.Id == id);

            if (course == null)
            {
                return NotFound();
            }

            _context.Courses.Remove(course);

            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}