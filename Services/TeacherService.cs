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

        public async Task<PagedResultDto<TeacherDto>> GetAllAsync(
      string? search,
      string? specialization,
      string? sortBy,
      bool sortDesc,
      int page,
      int pageSize)
        {
            var query = _context.Teachers.AsQueryable();

            // SEARCH
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(t =>
                    t.FullName.Contains(search) ||
                    t.Email.Contains(search) ||
                    t.Phone.Contains(search) ||
                    t.Specialization.Contains(search));
            }

            // FILTER
            if (!string.IsNullOrWhiteSpace(specialization))
            {
                query = query.Where(t =>
                    t.Specialization == specialization);
            }

            // SORT
            if (!string.IsNullOrWhiteSpace(sortBy))
            {
                switch (sortBy.ToLower())
                {
                    case "id":
                        query = sortDesc
                            ? query.OrderByDescending(t => t.Id)
                            : query.OrderBy(t => t.Id);
                        break;

                    case "fullname":
                        query = sortDesc
                            ? query.OrderByDescending(t => t.FullName)
                            : query.OrderBy(t => t.FullName);
                        break;

                    case "email":
                        query = sortDesc
                            ? query.OrderByDescending(t => t.Email)
                            : query.OrderBy(t => t.Email);
                        break;

                    case "specialization":
                        query = sortDesc
                            ? query.OrderByDescending(t => t.Specialization)
                            : query.OrderBy(t => t.Specialization);
                        break;
                }
            }
            else
            {
                query = query.OrderBy(t => t.Id);
            }

            // VALIDATE PAGINATION
            if (page < 1)
            {
                page = 1;
            }

            if (pageSize < 1)
            {
                pageSize = 20;
            }

            // TOTAL
            var totalItems = await query.CountAsync();

            var totalPages = (int)Math.Ceiling(
                (double)totalItems / pageSize);

            // PAGINATION
            var teachers = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // DTO
            var data = teachers.Select(t => new TeacherDto
            {
                Id = t.Id,
                FullName = t.FullName,
                Email = t.Email,
                Phone = t.Phone,
                Specialization = t.Specialization,
                UserId = t.UserId
            }).ToList();

            return new PagedResultDto<TeacherDto>
            {
                Data = data,
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages
            };
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