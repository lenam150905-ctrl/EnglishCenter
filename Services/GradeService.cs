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

        public async Task<List<GradeDto>> GetAllAsync()
        {
            var grades = await _context.Grades
                .Include(g => g.Exam)
                .Include(g => g.Student)
                .ToListAsync();

            return grades.Select(g => new GradeDto
            {
                Id = g.Id,

                ExamId = g.ExamId,
                ExamName = g.Exam.ExamName,

                StudentId = g.StudentId,
                StudentName = g.Student.FullName,

                Score = g.Score,
                Comment = g.Comment
            }).ToList();
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