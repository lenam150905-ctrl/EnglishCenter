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

        public async Task<List<ExamDto>> GetAllAsync()
        {
            var exams = await _context.Exams
                .Include(e => e.Course)
                .ToListAsync();

            return exams.Select(e => new ExamDto
            {
                Id = e.Id,
                ExamName = e.ExamName,
                ExamType = e.ExamType,
                ExamDate = e.ExamDate,
                CourseId = e.CourseId,
                CourseName = e.Course?.CourseName
            }).ToList();
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

        public async Task<ExamDto> CreateAsync(
            ExamCreateDto dto)
        {
            if (dto.ExamDate < DateTime.Now)
            {
                throw new ArgumentException(
                    "Ngày thi không được nhỏ hơn ngày hiện tại.");
            }

            Course? course = null;

            if (dto.CourseId.HasValue)
            {
                course = await _context.Courses
                    .FirstOrDefaultAsync(
                        c => c.Id == dto.CourseId.Value);

                if (course == null)
                {
                    throw new ArgumentException(
                        "Course không tồn tại.");
                }
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
                CourseId = exam.CourseId,
                CourseName = course?.CourseName
            };
        }

        public async Task<bool> UpdateAsync(
            int id,
            ExamUpdateDto dto)
        {
            var exam = await _context.Exams
                .FindAsync(id);

            if (exam == null)
            {
                return false;
            }

            if (dto.ExamDate < DateTime.Now)
            {
                throw new ArgumentException(
                    "Ngày thi không được nhỏ hơn ngày hiện tại.");
            }

            if (dto.CourseId.HasValue)
            {
                var courseExists = await _context.Courses
                    .AnyAsync(
                        c => c.Id == dto.CourseId.Value);

                if (!courseExists)
                {
                    throw new ArgumentException(
                        "Course không tồn tại.");
                }
            }

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