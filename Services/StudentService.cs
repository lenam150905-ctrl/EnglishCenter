using EnglishCenter.API.Data;
using EnglishCenter.API.DTOs;
using EnglishCenter.API.Models;
using Microsoft.EntityFrameworkCore;

namespace EnglishCenter.API.Services
{
    public class StudentService : IStudentService
    {
        private readonly ApplicationDbContext _context;

        public StudentService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PagedResultDto<StudentDto>> GetAllAsync(
     string? search,
     DateTime? fromDateOfBirth,
     DateTime? toDateOfBirth,
     string? sortBy,
     bool sortDesc,
     int page,
     int pageSize)
        {
            var query = _context.Students.AsQueryable();

            // SEARCH
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(s =>
                    s.FullName.Contains(search) ||
                    s.Email.Contains(search) ||
                    s.Phone.Contains(search) ||
                    s.Address.Contains(search));
            }

            // FILTER
            if (fromDateOfBirth.HasValue)
            {
                query = query.Where(s =>
                    s.DateOfBirth >= fromDateOfBirth.Value);
            }

            if (toDateOfBirth.HasValue)
            {
                query = query.Where(s =>
                    s.DateOfBirth <= toDateOfBirth.Value);
            }

            // SORT
            if (!string.IsNullOrWhiteSpace(sortBy))
            {
                switch (sortBy.ToLower())
                {
                    case "id":
                        query = sortDesc
                            ? query.OrderByDescending(s => s.Id)
                            : query.OrderBy(s => s.Id);
                        break;

                    case "fullname":
                        query = sortDesc
                            ? query.OrderByDescending(s => s.FullName)
                            : query.OrderBy(s => s.FullName);
                        break;

                    case "dateofbirth":
                        query = sortDesc
                            ? query.OrderByDescending(s => s.DateOfBirth)
                            : query.OrderBy(s => s.DateOfBirth);
                        break;

                    case "email":
                        query = sortDesc
                            ? query.OrderByDescending(s => s.Email)
                            : query.OrderBy(s => s.Email);
                        break;
                }
            }
            else
            {
                query = query.OrderBy(s => s.Id);
            }

            // TOTAL
            var totalItems = await query.CountAsync();

            // PAGINATION
            if (page < 1)
            {
                page = 1;
            }

            if (pageSize < 1)
            {
                pageSize = 20;
            }

            var totalPages = (int)Math.Ceiling(
                (double)totalItems / pageSize);

            var students = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // DTO
            var data = students.Select(s => new StudentDto
            {
                Id = s.Id,
                FullName = s.FullName,
                DateOfBirth = s.DateOfBirth,
                Email = s.Email,
                Phone = s.Phone,
                Address = s.Address,
                UserId = s.UserId
            }).ToList();

            return new PagedResultDto<StudentDto>
            {
                Data = data,
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages
            };
        }

        public async Task<StudentDto?> GetByIdAsync(int id)
        {
            var student = await _context.Students
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (student == null)
            {
                return null;
            }

            return new StudentDto
            {
                Id = student.Id,
                FullName = student.FullName,
                DateOfBirth = student.DateOfBirth,
                Email = student.Email,
                Phone = student.Phone,
                Address = student.Address,
                UserId = student.UserId,
                UserName = student.User != null
                    ? student.User.UserName
                    : null
            };
        }

        public async Task<StudentDto> CreateAsync(
            StudentCreateDto dto)
        {
            if (dto.DateOfBirth > DateTime.Now)
            {
                throw new ArgumentException(
                    "Ngày sinh không được lớn hơn ngày hiện tại.");
            }

            if (dto.UserId.HasValue)
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(
                        u => u.Id == dto.UserId.Value);

                if (user == null)
                {
                    throw new ArgumentException(
                        "User không tồn tại.");
                }

                var userAlreadyUsed =
                    await _context.Students
                        .AnyAsync(s =>
                            s.UserId == dto.UserId.Value);

                if (userAlreadyUsed)
                {
                    throw new ArgumentException(
                        "User này đã được liên kết với một Student.");
                }
            }

            var student = new Student
            {
                FullName = dto.FullName,
                DateOfBirth = dto.DateOfBirth,
                Email = dto.Email,
                Phone = dto.Phone,
                Address = dto.Address,
                UserId = dto.UserId
            };

            _context.Students.Add(student);

            await _context.SaveChangesAsync();

            await _context.Entry(student)
                .Reference(s => s.User)
                .LoadAsync();

            return new StudentDto
            {
                Id = student.Id,
                FullName = student.FullName,
                DateOfBirth = student.DateOfBirth,
                Email = student.Email,
                Phone = student.Phone,
                Address = student.Address,
                UserId = student.UserId,
                UserName = student.User != null
                    ? student.User.UserName
                    : null
            };
        }

        public async Task<bool> UpdateAsync(
            int id,
            StudentUpdateDto dto)
        {
            var student = await _context.Students
                .FindAsync(id);

            if (student == null)
            {
                return false;
            }

            if (dto.DateOfBirth > DateTime.Now)
            {
                throw new ArgumentException(
                    "Ngày sinh không được lớn hơn ngày hiện tại.");
            }

            if (dto.UserId.HasValue)
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(
                        u => u.Id == dto.UserId.Value);

                if (user == null)
                {
                    throw new ArgumentException(
                        "User không tồn tại.");
                }

                var userAlreadyUsed =
                    await _context.Students
                        .AnyAsync(s =>
                            s.Id != id &&
                            s.UserId == dto.UserId.Value);

                if (userAlreadyUsed)
                {
                    throw new ArgumentException(
                        "User này đã được liên kết với Student khác.");
                }
            }

            student.FullName = dto.FullName;
            student.DateOfBirth = dto.DateOfBirth;
            student.Email = dto.Email;
            student.Phone = dto.Phone;
            student.Address = dto.Address;
            student.UserId = dto.UserId;

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var student = await _context.Students
                .FindAsync(id);

            if (student == null)
            {
                return false;
            }

            _context.Students.Remove(student);

            await _context.SaveChangesAsync();

            return true;
        }
    }
}