using EnglishCenter.API.Data;
using EnglishCenter.API.DTOs;
using EnglishCenter.API.Models;
using Microsoft.EntityFrameworkCore;

namespace EnglishCenter.API.Services
{
    public class TeacherService : ITeacherService
    {
        private readonly ApplicationDbContext _context;

        public TeacherService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<TeacherDto>> GetAllAsync()
        {
            var teachers = await _context.Teachers.ToListAsync();

            return teachers.Select(t => new TeacherDto
            {
                Id = t.Id,
                FullName = t.FullName,
                Email = t.Email,
                Phone = t.Phone,
                Specialization = t.Specialization,
                UserId = t.UserId
            }).ToList();
        }

        public async Task<TeacherDto?> GetByIdAsync(int id)
        {
            var teacher = await _context.Teachers
                .FirstOrDefaultAsync(t => t.Id == id);

            if (teacher == null)
            {
                return null;
            }

            return new TeacherDto
            {
                Id = teacher.Id,
                FullName = teacher.FullName,
                Email = teacher.Email,
                Phone = teacher.Phone,
                Specialization = teacher.Specialization,
                UserId = teacher.UserId
            };
        }

        public async Task<TeacherDto> CreateAsync(TeacherCreateDto dto)
        {
            var teacher = new Teacher
            {
                FullName = dto.FullName,
                Email = dto.Email,
                Phone = dto.Phone,
                Specialization = dto.Specialization,
                UserId = dto.UserId
            };

            _context.Teachers.Add(teacher);

            await _context.SaveChangesAsync();

            return new TeacherDto
            {
                Id = teacher.Id,
                FullName = teacher.FullName,
                Email = teacher.Email,
                Phone = teacher.Phone,
                Specialization = teacher.Specialization,
                UserId = teacher.UserId
            };
        }

        public async Task<bool> UpdateAsync(
            int id,
            TeacherUpdateDto dto)
        {
            var teacher = await _context.Teachers
                .FirstOrDefaultAsync(t => t.Id == id);

            if (teacher == null)
            {
                return false;
            }

            teacher.FullName = dto.FullName;
            teacher.Email = dto.Email;
            teacher.Phone = dto.Phone;
            teacher.Specialization = dto.Specialization;
            teacher.UserId = dto.UserId;

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var teacher = await _context.Teachers
                .FirstOrDefaultAsync(t => t.Id == id);

            if (teacher == null)
            {
                return false;
            }

            _context.Teachers.Remove(teacher);

            await _context.SaveChangesAsync();

            return true;
        }
    }
}