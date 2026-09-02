using EnglishCenter.API.Data;
using EnglishCenter.API.DTOs;
using EnglishCenter.API.Models;
using Microsoft.EntityFrameworkCore;

namespace EnglishCenter.API.Services
{
    public class EnrollmentService : IEnrollmentService
    {
        private readonly ApplicationDbContext _context;

        public EnrollmentService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<EnrollmentDto>> GetAllAsync()
        {
            var enrollments = await _context.Enrollments
                .Include(e => e.Student)
                .Include(e => e.Course)
                .ToListAsync();

            return enrollments.Select(e => new EnrollmentDto
            {
                Id = e.Id,

                StudentId = e.StudentId,
                StudentName = e.Student.FullName,

                CourseId = e.CourseId,
                CourseName = e.Course.CourseName,

                EnrollmentDate = e.EnrollmentDate,
                Status = e.Status
            }).ToList();
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
            var student = await _context.Students
                .FindAsync(dto.StudentId);

            if (student == null)
            {
                throw new ArgumentException(
                    "Sinh viên không tồn tại.");
            }

            var course = await _context.Courses
                .FindAsync(dto.CourseId);

            if (course == null)
            {
                throw new ArgumentException(
                    "Khóa học không tồn tại.");
            }

            var existed = await _context.Enrollments
                .AnyAsync(e =>
                    e.StudentId == dto.StudentId &&
                    e.CourseId == dto.CourseId);

            if (existed)
            {
                throw new ArgumentException(
                    "Học viên đã đăng ký khóa học này.");
            }

            var enrollment = new Enrollment
            {
                StudentId = dto.StudentId,
                CourseId = dto.CourseId,
                EnrollmentDate = dto.EnrollmentDate,
                Status = dto.Status
            };

            _context.Enrollments.Add(enrollment);

            await _context.SaveChangesAsync();

            await _context.Entry(enrollment)
                .Reference(e => e.Student)
                .LoadAsync();

            await _context.Entry(enrollment)
                .Reference(e => e.Course)
                .LoadAsync();

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

        public async Task<bool> UpdateAsync(
            int id,
            EnrollmentUpdateDto dto)
        {
            var enrollment = await _context.Enrollments
                .FindAsync(id);

            if (enrollment == null)
            {
                return false;
            }

            var student = await _context.Students
                .FindAsync(dto.StudentId);

            if (student == null)
            {
                throw new ArgumentException(
                    "Sinh viên không tồn tại.");
            }

            var course = await _context.Courses
                .FindAsync(dto.CourseId);

            if (course == null)
            {
                throw new ArgumentException(
                    "Khóa học không tồn tại.");
            }

            var existed = await _context.Enrollments
                .AnyAsync(e =>
                    e.Id != id &&
                    e.StudentId == dto.StudentId &&
                    e.CourseId == dto.CourseId);

            if (existed)
            {
                throw new ArgumentException(
                    "Học viên đã đăng ký khóa học này.");
            }

            enrollment.StudentId = dto.StudentId;
            enrollment.CourseId = dto.CourseId;
            enrollment.EnrollmentDate = dto.EnrollmentDate;
            enrollment.Status = dto.Status;

            await _context.SaveChangesAsync();

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

            _context.Enrollments.Remove(enrollment);

            await _context.SaveChangesAsync();

            return true;
        }
    }
}