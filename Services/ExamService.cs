using EnglishCenter.API.Data;
using EnglishCenter.API.DTOs;
using EnglishCenter.API.Models;
using Microsoft.EntityFrameworkCore;

namespace EnglishCenter.API.Services
{
    public class ExamService : IExamService
    {
        private readonly ApplicationDbContext _context;

        public ExamService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PagedResultDto<ExamDto>> GetAllAsync(
     string? search,
     int? courseId,
     string? examType,
     string? sortBy,
     bool sortDesc,
     int page,
     int pageSize)
        {
            var query = _context.Exams
                .Include(e => e.Course)
                .AsQueryable();

            // SEARCH
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(e =>
                    e.ExamName.Contains(search) ||
                    e.ExamType.Contains(search) ||
                    (e.Course != null &&
                     e.Course.CourseName.Contains(search)));
            }

            // FILTER
            if (courseId.HasValue)
            {
                query = query.Where(e =>
                    e.CourseId == courseId.Value);
            }

            if (!string.IsNullOrWhiteSpace(examType))
            {
                query = query.Where(e =>
                    e.ExamType == examType);
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

                    case "examname":
                        query = sortDesc
                            ? query.OrderByDescending(e => e.ExamName)
                            : query.OrderBy(e => e.ExamName);
                        break;

                    case "examtype":
                        query = sortDesc
                            ? query.OrderByDescending(e => e.ExamType)
                            : query.OrderBy(e => e.ExamType);
                        break;

                    case "examdate":
                        query = sortDesc
                            ? query.OrderByDescending(e => e.ExamDate)
                            : query.OrderBy(e => e.ExamDate);
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

            var exams = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // DTO
            var data = exams.Select(e => new ExamDto
            {
                Id = e.Id,
                ExamName = e.ExamName,
                ExamType = e.ExamType,
                ExamDate = e.ExamDate,
                CourseId = e.CourseId,
                CourseName = e.Course?.CourseName
            }).ToList();

            return new PagedResultDto<ExamDto>
            {
                Data = data,
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages
            };
        }

        public async Task<ExamDto?> GetByIdAsync(int id)
        {
            var exam = await _context.Exams
                .Include(e => e.Course)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (exam == null)
            {
                return null;
            }

            return new ExamDto
            {
                Id = exam.Id,
                ExamName = exam.ExamName,
                ExamType = exam.ExamType,
                ExamDate = exam.ExamDate,
                CourseId = exam.CourseId,
                CourseName = exam.Course?.CourseName
            };
        }

        public async Task<ExamDto> CreateAsync(ExamCreateDto dto)
        {
            // EXAM NAME
            if (string.IsNullOrWhiteSpace(dto.ExamName))
            {
                throw new ArgumentException(
                    "Tên bài thi không được để trống.");
            }

            if (dto.ExamName.Length > 100)
            {
                throw new ArgumentException(
                    "Tên bài thi không được vượt quá 100 ký tự.");
            }

            // EXAM TYPE
            if (string.IsNullOrWhiteSpace(dto.ExamType))
            {
                throw new ArgumentException(
                    "Loại bài thi không được để trống.");
            }

            // COURSE
            if (dto.CourseId.HasValue)
            {
                var courseExists = await _context.Courses
                    .AnyAsync(c => c.Id == dto.CourseId.Value);

                if (!courseExists)
                {
                    throw new ArgumentException(
                        "Course không tồn tại.");
                }
            }

            // EXAM DATE
            if (dto.ExamDate < DateTime.Now)
            {
                throw new ArgumentException(
                    "Ngày thi không được ở trong quá khứ.");
            }

            // CHECK EXAM TRÙNG
            var existed = await _context.Exams
                .AnyAsync(e =>
                    e.ExamName == dto.ExamName &&
                    e.CourseId == dto.CourseId);

            if (existed)
            {
                throw new ArgumentException(
                    "Bài thi này đã tồn tại trong khóa học.");
            }

            var exam = new Exam
            {
                ExamName = dto.ExamName,
                ExamType = dto.ExamType,
                ExamDate = dto.ExamDate,
                CourseId = dto.CourseId
            };

            _context.Exams.Add(exam);

            await _context.SaveChangesAsync();

            return new ExamDto
            {
                Id = exam.Id,
                ExamName = exam.ExamName,
                ExamType = exam.ExamType,
                ExamDate = exam.ExamDate,
                CourseId = exam.CourseId
            };
        }

        public async Task<bool> UpdateAsync(
     int id,
     ExamUpdateDto dto)
        {
            // KIỂM TRA EXAM
            var exam = await _context.Exams
                .FindAsync(id);

            if (exam == null)
            {
                return false;
            }

            // EXAM NAME
            if (string.IsNullOrWhiteSpace(dto.ExamName))
            {
                throw new ArgumentException(
                    "Tên bài thi không được để trống.");
            }

            if (dto.ExamName.Length > 100)
            {
                throw new ArgumentException(
                    "Tên bài thi không được vượt quá 100 ký tự.");
            }

            // EXAM TYPE
            if (string.IsNullOrWhiteSpace(dto.ExamType))
            {
                throw new ArgumentException(
                    "Loại bài thi không được để trống.");
            }

            // COURSE
            if (dto.CourseId.HasValue)
            {
                var courseExists = await _context.Courses
                    .AnyAsync(c => c.Id == dto.CourseId.Value);

                if (!courseExists)
                {
                    throw new ArgumentException(
                        "Course không tồn tại.");
                }
            }

            // EXAM DATE
            if (dto.ExamDate < DateTime.Now)
            {
                throw new ArgumentException(
                    "Ngày thi không được ở trong quá khứ.");
            }

            // CHECK TRÙNG
            var existed = await _context.Exams
                .AnyAsync(e =>
                    e.Id != id &&
                    e.ExamName == dto.ExamName &&
                    e.CourseId == dto.CourseId);

            if (existed)
            {
                throw new ArgumentException(
                    "Bài thi này đã tồn tại trong khóa học.");
            }

            // UPDATE
            exam.ExamName = dto.ExamName;
            exam.ExamType = dto.ExamType;
            exam.ExamDate = dto.ExamDate;
            exam.CourseId = dto.CourseId;

            await _context.SaveChangesAsync();

            return true;
        }
        public async Task<bool> DeleteAsync(int id)
        {
            var exam = await _context.Exams
                .FindAsync(id);

            if (exam == null)
            {
                return false;
            }

            _context.Exams.Remove(exam);

            await _context.SaveChangesAsync();

            return true;
        }
    }
}