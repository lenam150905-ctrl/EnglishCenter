using EnglishCenter.API.DTOs;
using EnglishCenter.API.Middleware;
using EnglishCenter.API.Models;
using EnglishCenter.Application.Abstractions.Persistence;

namespace EnglishCenter.API.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IAuditLogService _auditLogService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly INotificationService _notificationService;

        public UserService(
            IUserRepository userRepository,
            IAuditLogService auditLogService,
            IHttpContextAccessor httpContextAccessor,
            INotificationService notificationService)
        {
            _userRepository = userRepository;
            _auditLogService = auditLogService;
            _httpContextAccessor = httpContextAccessor;
            _notificationService = notificationService;
        }

        private int? userid => AuditContext.GetUserId(_httpContextAccessor.HttpContext!);
        private string? ipaddress => AuditContext.GetIPAddress(_httpContextAccessor.HttpContext!);

        public async Task<PagedResultDto<UserDto>> GetAllAsync(
            string? search,
            string? role,
            string? sortBy,
            bool sortDesc,
            int page,
            int pageSize)
        {
            var (users, totalItems) = await _userRepository.GetAllAsync(search, role, sortBy, sortDesc, page, pageSize);

            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 20 : pageSize;
            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

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
            var user = await _userRepository.GetByIdAsync(id);
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
            var existed = await _userRepository.ExistsByUserNameAsync(dto.UserName);
            if (existed)
            {
                throw new ArgumentException("Tên đăng nhập đã tồn tại.");
            }

            var emailExisted = await _userRepository.ExistsByEmailAsync(dto.Email);
            if (emailExisted)
            {
                throw new ArgumentException("Email đã tồn tại.");
            }

            var user = new User
            {
                UserName = dto.UserName,
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = dto.Role
            };

            await _userRepository.CreateAsync(user);

            await _auditLogService.CreateAsync(
                userid,
                "CREATE",
                "User",
                user.Id,
                $"Tạo tài khoản {user.UserName} - Email: {user.Email}, Role: {user.Role}",
                ipaddress);

            if (userid.HasValue)
            {
                await _notificationService.CreateAsync(new NotificationCreateDto
                {
                    UserId = userid.Value,
                    Title = "Tạo tài khoản",
                    Message = $"Bạn đã tạo tài khoản {user.UserName} với quyền {user.Role}.",
                    Type = "USER"
                });
            }

            if (user.Id != userid)
            {
                await _notificationService.CreateAsync(new NotificationCreateDto
                {
                    UserId = user.Id,
                    Title = "Tài khoản mới",
                    Message = $"Tài khoản {user.UserName} đã được tạo cho bạn với quyền {user.Role}.",
                    Type = "USER"
                });
            }

            return new UserDto
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                Role = user.Role
            };
        }

        public async Task<bool> UpdateAsync(int id, UserUpdateDto dto)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
            {
                return false;
            }

            var existed = await _userRepository.ExistsByUserNameAsync(dto.UserName, id);
            if (existed)
            {
                throw new ArgumentException("Tên đăng nhập đã tồn tại.");
            }

            var emailExisted = await _userRepository.ExistsByEmailAsync(dto.Email, id);
            if (emailExisted)
            {
                throw new ArgumentException("Email đã tồn tại.");
            }

            user.UserName = dto.UserName;
            user.Email = dto.Email;
            user.Role = dto.Role;

            if (!string.IsNullOrWhiteSpace(dto.Password))
            {
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
            }

            await _userRepository.UpdateAsync(user);

            await _auditLogService.CreateAsync(
                userid,
                "UPDATE",
                "User",
                user.Id,
                $"Cập nhật tài khoản {user.UserName} - Email: {user.Email}, Role: {user.Role}",
                ipaddress);

            if (userid.HasValue)
            {
                await _notificationService.CreateAsync(new NotificationCreateDto
                {
                    UserId = userid.Value,
                    Title = "Cập nhật tài khoản",
                    Message = $"Bạn đã cập nhật tài khoản {user.UserName}.",
                    Type = "USER"
                });
            }

            if (user.Id != userid)
            {
                await _notificationService.CreateAsync(new NotificationCreateDto
                {
                    UserId = user.Id,
                    Title = "Tài khoản được cập nhật",
                    Message = $"Thông tin tài khoản {user.UserName} của bạn đã được cập nhật. Quyền hiện tại: {user.Role}.",
                    Type = "USER"
                });
            }

            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
            {
                return false;
            }

            var userName = user.UserName;
            var email = user.Email;
            var role = user.Role;

            await _userRepository.SoftDeleteAsync(id);

            await _auditLogService.CreateAsync(
                userid,
                "DELETE",
                "User",
                id,
                $"Xóa tài khoản {userName} - Email: {email}, Role: {role}",
                ipaddress);

            if (userid.HasValue)
            {
                await _notificationService.CreateAsync(new NotificationCreateDto
                {
                    UserId = userid.Value,
                    Title = "Xóa tài khoản",
                    Message = $"Bạn đã xóa tài khoản {userName}.",
                    Type = "USER"
                });
            }

            if (user.Id != userid)
            {
                await _notificationService.CreateAsync(new NotificationCreateDto
                {
                    UserId = user.Id,
                    Title = "Tài khoản đã bị xóa",
                    Message = $"Tài khoản {userName} của bạn đã bị xóa khỏi hệ thống.",
                    Type = "USER"
                });
            }

            return true;
        }
    }
}