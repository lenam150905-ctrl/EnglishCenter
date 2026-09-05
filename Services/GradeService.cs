using EnglishCenter.API.Data;
using EnglishCenter.API.DTOs;
using EnglishCenter.API.Models;
using Microsoft.EntityFrameworkCore;

namespace EnglishCenter.API.Services
{
    public class GradeService : IGradeService
    {
        private readonly ApplicationDbContext _context;

        public GradeService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PagedResultDto<GradeDto>> GetAllAsync(
     string? search,
     int? examId,
     int? studentId,
     decimal? minScore,
     decimal? maxScore,
     string? sortBy,
     bool sortDesc,
     int page,
     int pageSize)
        {
            var query = _context.Grades
                .Include(g => g.Exam)
                .Include(g => g.Student)
                .AsQueryable();

            // SEARCH
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(g =>
                    g.Student.FullName.Contains(search) ||
                    g.Exam.ExamName.Contains(search));
            }

            // FILTER
            if (examId.HasValue)
            {
                query = query.Where(g =>
                    g.ExamId == examId.Value);
            }

            if (studentId.HasValue)
            {
                query = query.Where(g =>
                    g.StudentId == studentId.Value);
            }

            if (minScore.HasValue)
            {
                query = query.Where(g =>
                    g.Score >= minScore.Value);
            }

            if (maxScore.HasValue)
            {
                query = query.Where(g =>
                    g.Score <= maxScore.Value);
            }

            // SORT
            if (!string.IsNullOrWhiteSpace(sortBy))
            {
                switch (sortBy.ToLower())
                {
                    case "id":
                        query = sortDesc
                            ? query.OrderByDescending(g => g.Id)
                            : query.OrderBy(g => g.Id);
                        break;

                    case "score":
                        query = sortDesc
                            ? query.OrderByDescending(g => g.Score)
                            : query.OrderBy(g => g.Score);
                        break;
                }
            }
            else
            {
                query = query.OrderBy(g => g.Id);
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

            var grades = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // DTO
            var data = grades.Select(g => new GradeDto
            {
                Id = g.Id,
                ExamId = g.ExamId,
                ExamName = g.Exam.ExamName,
                StudentId = g.StudentId,
                StudentName = g.Student.FullName,
                Score = g.Score,
                Comment = g.Comment
            }).ToList();

            return new PagedResultDto<GradeDto>
            {
                Data = data,
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages
            };
        }
        public async Task<GradeDto?> GetByIdAsync(int id)
        {
            var grade = await _context.Grades
                .Include(g => g.Exam)
                .Include(g => g.Student)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (grade == null)
            {
                return null;
            }

            return new GradeDto
            {
                Id = grade.Id,

                ExamId = grade.ExamId,
                ExamName = grade.Exam.ExamName,

                StudentId = grade.StudentId,
                StudentName = grade.Student.FullName,

                Score = grade.Score,
                Comment = grade.Comment
            };
        }

        public async Task<GradeDto> CreateAsync(
            GradeCreateDto dto)
        {
            var exam = await _context.Exams
                .FirstOrDefaultAsync(e => e.Id == dto.ExamId);

            if (exam == null)
            {
                throw new ArgumentException(
                    "Exam không tồn tại.");
            }

            var student = await _context.Students
                .FirstOrDefaultAsync(
                    s => s.Id == dto.StudentId);

            if (student == null)
            {
                throw new ArgumentException(
                    "Student không tồn tại.");
            }

            var existed = await _context.Grades
                .AnyAsync(g =>
                    g.ExamId == dto.ExamId &&
                    g.StudentId == dto.StudentId);

            if (existed)
            {
                throw new ArgumentException(
                    "Student này đã có điểm trong kỳ thi.");
            }

            var grade = new Grade
            {
                ExamId = dto.ExamId,
                StudentId = dto.StudentId,
                Score = dto.Score,
                Comment = dto.Comment
            };

            _context.Grades.Add(grade);

            await _context.SaveChangesAsync();

            return new GradeDto
            {
                Id = grade.Id,

                ExamId = grade.ExamId,
                ExamName = exam.ExamName,

                StudentId = grade.StudentId,
                StudentName = student.FullName,

                Score = grade.Score,
                Comment = grade.Comment
            };
        }

        public async Task<bool> UpdateAsync(
            int id,
            GradeUpdateDto dto)
        {
            var grade = await _context.Grades
                .FindAsync(id);

            if (grade == null)
            {
                return false;
            }

            var exam = await _context.Exams
                .FirstOrDefaultAsync(
                    e => e.Id == dto.ExamId);

            if (exam == null)
            {
                throw new ArgumentException(
                    "Exam không tồn tại.");
            }

            var student = await _context.Students
                .FirstOrDefaultAsync(
                    s => s.Id == dto.StudentId);

            if (student == null)
            {
                throw new ArgumentException(
                    "Student không tồn tại.");
            }

            var existed = await _context.Grades
                .AnyAsync(g =>
                    g.Id != id &&
                    g.ExamId == dto.ExamId &&
                    g.StudentId == dto.StudentId);

            if (existed)
            {
                throw new ArgumentException(
                    "Student này đã có điểm trong kỳ thi.");
            }

            grade.ExamId = dto.ExamId;
            grade.StudentId = dto.StudentId;
            grade.Score = dto.Score;
            grade.Comment = dto.Comment;

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var grade = await _context.Grades
                .FindAsync(id);

            if (grade == null)
            {
                return false;
            }

            _context.Grades.Remove(grade);

            await _context.SaveChangesAsync();

            return true;
        }
    }
}