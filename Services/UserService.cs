using EnglishCenter.API.Data;
using EnglishCenter.API.DTOs;
using EnglishCenter.API.Models;
using Microsoft.EntityFrameworkCore;

namespace EnglishCenter.API.Services
{
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;

        public UserService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PagedResultDto<UserDto>> GetAllAsync(
      string? search,
      string? role,
      string? sortBy,
      bool sortDesc,
      int page,
      int pageSize)
        {
            var query = _context.Users.AsQueryable();

            // SEARCH
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(u =>
                    u.UserName.Contains(search) ||
                    u.Role.Contains(search));
            }

            // FILTER
            if (!string.IsNullOrWhiteSpace(role))
            {
                query = query.Where(u =>
                    u.Role == role);
            }

            // SORT
            if (!string.IsNullOrWhiteSpace(sortBy))
            {
                switch (sortBy.ToLower())
                {
                    case "username":
                        query = sortDesc
                            ? query.OrderByDescending(u => u.UserName)
                            : query.OrderBy(u => u.UserName);
                        break;

                    case "role":
                        query = sortDesc
                            ? query.OrderByDescending(u => u.Role)
                            : query.OrderBy(u => u.Role);
                        break;

                    case "id":
                        query = sortDesc
                            ? query.OrderByDescending(u => u.Id)
                            : query.OrderBy(u => u.Id);
                        break;
                }

            }
            else
            {
                query = query.OrderBy(u => u.Id);
            }
            if (page < 1)
            {
                page = 1;
            }

            if (pageSize < 1)
            {
                pageSize = 20;
            }
            var totalItems = await query.CountAsync();
            // PAGINATION
            var users = await query
    .Skip((page - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync();
            var totalPages = (int)Math.Ceiling(
    (double)totalItems / pageSize);

            return new PagedResultDto<UserDto>
            {
                Data = users.Select(u => new UserDto
                {
                    Id = u.Id,
                    UserName = u.UserName,
                    Role = u.Role
                }).ToList(),

                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages
            };
        }

        public async Task<UserDto?> GetByIdAsync(int id)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return null;
            }

            return new UserDto
            {
                Id = user.Id,
                UserName = user.UserName,
                Role = user.Role
            };
        }

        public async Task<UserDto> CreateAsync(UserCreateDto dto)
        {
            // USERNAME
            if (string.IsNullOrWhiteSpace(dto.UserName))
            {
                throw new ArgumentException(
                    "Tên đăng nhập không được để trống.");
            }

            if (dto.UserName.Length < 3 || dto.UserName.Length > 50)
            {
                throw new ArgumentException(
                    "Tên đăng nhập phải từ 3 đến 50 ký tự.");
            }

            // PASSWORD
            if (string.IsNullOrWhiteSpace(dto.Password))
            {
                throw new ArgumentException(
                    "Mật khẩu không được để trống.");
            }

            if (dto.Password.Length < 6)
            {
                throw new ArgumentException(
                    "Mật khẩu phải có ít nhất 6 ký tự.");
            }

            // ROLE
            if (dto.Role != "Student" &&
                dto.Role != "Teacher" &&
                dto.Role != "Admin")
            {
                throw new ArgumentException(
                    "Role không hợp lệ.");
            }

            // CHECK USERNAME
            var existed = await _context.Users
                .AnyAsync(u => u.UserName == dto.UserName);

            if (existed)
            {
                throw new ArgumentException(
                    "Tên đăng nhập đã tồn tại.");
            }

            var user = new User
            {
                UserName = dto.UserName,
                PasswordHash = dto.Password,
                Role = dto.Role
            };

            _context.Users.Add(user);

            await _context.SaveChangesAsync();

            return new UserDto
            {
                Id = user.Id,
                UserName = user.UserName,
                Role = user.Role
            };
        }

        public async Task<bool> UpdateAsync(int id, UserUpdateDto dto)
        {
            // KIỂM TRA USER
            var user = await _context.Users
                .FindAsync(id);

            if (user == null)
            {
                return false;
            }

            // USERNAME
            if (string.IsNullOrWhiteSpace(dto.UserName))
            {
                throw new ArgumentException(
                    "Tên đăng nhập không được để trống.");
            }

            if (dto.UserName.Length < 3 || dto.UserName.Length > 50)
            {
                throw new ArgumentException(
                    "Tên đăng nhập phải từ 3 đến 50 ký tự.");
            }

            // ROLE
            if (dto.Role != "Student" &&
                dto.Role != "Teacher" &&
                dto.Role != "Admin")
            {
                throw new ArgumentException(
                    "Role không hợp lệ.");
            }

            // PASSWORD
            if (!string.IsNullOrWhiteSpace(dto.Password))
            {
                if (dto.Password.Length < 6)
                {
                    throw new ArgumentException(
                        "Mật khẩu phải có ít nhất 6 ký tự.");
                }

                user.PasswordHash = dto.Password;
            }

            // CHECK USERNAME TRÙNG
            var existed = await _context.Users
                .AnyAsync(u =>
                    u.Id != id &&
                    u.UserName == dto.UserName);

            if (existed)
            {
                throw new ArgumentException(
                    "Tên đăng nhập đã tồn tại.");
            }

            // UPDATE
            user.UserName = dto.UserName;
            user.Role = dto.Role;

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var user = await _context.Users
                .FindAsync(id);

            if (user == null)
            {
                return false;
            }

            _context.Users.Remove(user);

            await _context.SaveChangesAsync();

            return true;
        }
    }
}