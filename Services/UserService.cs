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

        public async Task<List<UserDto>> GetAllAsync()
        {
            var users = await _context.Users
                .ToListAsync();

            return users.Select(u => new UserDto
            {
                Id = u.Id,
                UserName = u.UserName,
                Role = u.Role
            }).ToList();
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

        public async Task<bool> UpdateAsync(
            int id,
            UserUpdateDto dto)
        {
            var user = await _context.Users
                .FindAsync(id);

            if (user == null)
            {
                return false;
            }

            var existed = await _context.Users
                .AnyAsync(u =>
                    u.Id != id &&
                    u.UserName == dto.UserName);

            if (existed)
            {
                throw new ArgumentException(
                    "Tên đăng nhập đã tồn tại.");
            }

            user.UserName = dto.UserName;
            user.Role = dto.Role;

            if (!string.IsNullOrWhiteSpace(dto.Password))
            {
                user.PasswordHash = dto.Password;
            }

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