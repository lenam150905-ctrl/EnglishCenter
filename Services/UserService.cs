using EnglishCenter.API.Data;
using EnglishCenter.API.DTOs;
using EnglishCenter.API.Middleware;
using EnglishCenter.API.Models;
using Microsoft.EntityFrameworkCore;

namespace EnglishCenter.API.Services
{
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditLogService _auditLogService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserService(
            ApplicationDbContext context,
            IAuditLogService auditLogService,
            IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _auditLogService = auditLogService;
            _httpContextAccessor = httpContextAccessor;
        }
        private int? userid =>
AuditContext.GetUserId(
 _httpContextAccessor.HttpContext!);

        private string? ipaddress =>
            AuditContext.GetIPAddress(
                _httpContextAccessor.HttpContext!);
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
      u.Email.Contains(search) ||
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
                    Email = u.Email,
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
                Email = user.Email,
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

            var emailExisted = await _context.Users
                .AnyAsync(u => u.Email == dto.Email);

            if (emailExisted)
            {
                throw new ArgumentException(
                    "Email đã tồn tại.");
            }

            var user = new User
            {
                UserName = dto.UserName,
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = dto.Role
            };

            _context.Users.Add(user);

            await _context.SaveChangesAsync();
            await _auditLogService.CreateAsync(
    userid,
    "CREATE",
    "User",
    user.Id,
    $"Tạo tài khoản {user.UserName} - Email: {user.Email}, Role: {user.Role}",
    ipaddress);

            return new UserDto
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
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

            var emailExisted = await _context.Users
                .AnyAsync(u =>
                    u.Id != id &&
                    u.Email == dto.Email);

            if (emailExisted)
            {
                throw new ArgumentException(
                    "Email đã tồn tại.");
            }

            user.UserName = dto.UserName;
            user.Email = dto.Email;
            user.Role = dto.Role;

            if (!string.IsNullOrWhiteSpace(dto.Password))
            {
                user.PasswordHash =
                    BCrypt.Net.BCrypt.HashPassword(dto.Password);
            }

            await _context.SaveChangesAsync();

            await _auditLogService.CreateAsync(
                userid,
                "UPDATE",
                "User",
                user.Id,
                $"Cập nhật tài khoản {user.UserName} - Email: {user.Email}, Role: {user.Role}",
                ipaddress);

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

            var userName = user.UserName;
            var email = user.Email;
            var role = user.Role;

            _context.Users.Remove(user);

            await _context.SaveChangesAsync();

            await _auditLogService.CreateAsync(
                userid,
                "DELETE",
                "User",
                id,
                $"Xóa tài khoản {userName} - Email: {email}, Role: {role}",
                ipaddress);

            return true;
        }
    }
}