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

        public async Task<PagedResultDto<EnrollmentDto>> GetAllAsync(
      string? search,
      int? studentId,
      int? courseId,
      string? status,
      string? sortBy,
      bool sortDesc,
      int page,
      int pageSize)
        {
            var query = _context.Enrollments
                .Include(e => e.Student)
                .Include(e => e.Course)
                .AsQueryable();

            // SEARCH
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(e =>
                    e.Student.FullName.Contains(search) ||
                    e.Student.Email.Contains(search) ||
                    e.Course.CourseName.Contains(search));
            }

            // FILTER
            if (studentId.HasValue)
            {
                query = query.Where(e =>
                    e.StudentId == studentId.Value);
            }

            if (courseId.HasValue)
            {
                query = query.Where(e =>
                    e.CourseId == courseId.Value);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(e =>
                    e.Status == status);
            }

            // SORT
            if (!string.IsNullOrWhiteSpace(sortBy))
            {
                switch (sortBy.ToLower())
                {
                    case "id":
                        query = sortDesc
                            ? query.OrderByDescending(e => e.Id)
                            : query.OrderBy(e => e.Id);
                        break;

                    case "enrollmentdate":
                        query = sortDesc
                            ? query.OrderByDescending(e => e.EnrollmentDate)
                            : query.OrderBy(e => e.EnrollmentDate);
                        break;

                    case "status":
                        query = sortDesc
                            ? query.OrderByDescending(e => e.Status)
                            : query.OrderBy(e => e.Status);
                        break;
                }
            }
            else
            {
                query = query.OrderBy(e => e.Id);
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

            var enrollments = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // DTO
            var data = enrollments.Select(e => new EnrollmentDto
            {
                Id = e.Id,
                StudentId = e.StudentId,
                StudentName = e.Student.FullName,
                CourseId = e.CourseId,
                CourseName = e.Course.CourseName,
                EnrollmentDate = e.EnrollmentDate,
                Status = e.Status
            }).ToList();

            return new PagedResultDto<EnrollmentDto>
            {
                Data = data,
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages
            };
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