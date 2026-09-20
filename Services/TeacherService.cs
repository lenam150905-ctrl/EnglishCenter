using EnglishCenter.API.DTOs;
using EnglishCenter.API.Middleware;
using EnglishCenter.API.Models;
using EnglishCenter.Application.Abstractions.Persistence;

namespace EnglishCenter.API.Services
{
    public class TeacherService : ITeacherService
    {
        private readonly ITeacherRepository _teacherRepository;
        private readonly IUserRepository _userRepository;
        private readonly IAuditLogService _auditLogService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly INotificationService _notificationService;

        public TeacherService(
            ITeacherRepository teacherRepository,
            IUserRepository userRepository,
            IAuditLogService auditLogService,
            IHttpContextAccessor httpContextAccessor,
            INotificationService notificationService)
        {
            _teacherRepository = teacherRepository;
            _userRepository = userRepository;
            _auditLogService = auditLogService;
            _httpContextAccessor = httpContextAccessor;
            _notificationService = notificationService;
        }

        private int? userid => AuditContext.GetUserId(_httpContextAccessor.HttpContext!);
        private string? ipaddress => AuditContext.GetIPAddress(_httpContextAccessor.HttpContext!);

        public async Task<PagedResultDto<TeacherDto>> GetAllAsync(
            string? search,
            string? specialization,
            string? sortBy,
            bool sortDesc,
            int page,
            int pageSize)
        {
            var (teachers, totalItems) = await _teacherRepository.GetAllAsync(
                search, specialization, sortBy, sortDesc, page, pageSize);

            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 20 : pageSize;
            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            return new PagedResultDto<TeacherDto>
            {
                Data = teachers.Select(t => new TeacherDto
                {
                    Id = t.Id,
                    FullName = t.FullName,
                    Email = t.Email,
                    Phone = t.Phone,
                    Specialization = t.Specialization,
                    UserId = t.UserId
                }).ToList(),
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages
            };
        }

        public async Task<TeacherDto?> GetByIdAsync(int id)
        {
            var teacher = await _teacherRepository.GetByIdAsync(id);
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
                UserId = teacher.UserId,
                UserName = teacher.User?.UserName
            };
        }

        public async Task<TeacherDto> CreateAsync(TeacherCreateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.FullName))
            {
                throw new ArgumentException("Họ tên không được để trống.");
            }

            if (dto.FullName.Length > 100)
            {
                throw new ArgumentException("Họ tên không được vượt quá 100 ký tự.");
            }

            if (string.IsNullOrWhiteSpace(dto.Email))
            {
                throw new ArgumentException("Email không được để trống.");
            }

            if (!dto.Email.Contains("@"))
            {
                throw new ArgumentException("Email không hợp lệ.");
            }

            if (string.IsNullOrWhiteSpace(dto.Phone))
            {
                throw new ArgumentException("Số điện thoại không được để trống.");
            }

            if (dto.Phone.Length < 9 || dto.Phone.Length > 15)
            {
                throw new ArgumentException("Số điện thoại không hợp lệ.");
            }

            if (string.IsNullOrWhiteSpace(dto.Specialization))
            {
                throw new ArgumentException("Chuyên môn không được để trống.");
            }

            var existedEmail = await _teacherRepository.ExistsByEmailAsync(dto.Email);
            if (existedEmail)
            {
                throw new ArgumentException("Email đã tồn tại.");
            }

            var existedPhone = await _teacherRepository.ExistsByPhoneAsync(dto.Phone);
            if (existedPhone)
            {
                throw new ArgumentException("Số điện thoại đã tồn tại.");
            }

            if (dto.UserId.HasValue)
            {
                var userExists = await _userRepository.GetByIdAsync(dto.UserId.Value);
                if (userExists == null)
                {
                    throw new ArgumentException("Tài khoản User không tồn tại.");
                }

                var userAssigned = await _teacherRepository.ExistsByUserIdAsync(dto.UserId.Value);
                if (userAssigned)
                {
                    throw new ArgumentException("User này đã được gán cho giáo viên khác.");
                }
            }

            var teacher = new Teacher
            {
                FullName = dto.FullName,
                Email = dto.Email,
                Phone = dto.Phone,
                Specialization = dto.Specialization,
                UserId = dto.UserId
            };

            await _teacherRepository.CreateAsync(teacher);

            await _auditLogService.CreateAsync(
                userid,
                "CREATE",
                "Teacher",
                teacher.Id,
                $"Tạo giáo viên {teacher.FullName} - Email: {teacher.Email}, Chuyên môn: {teacher.Specialization}",
                ipaddress);

            if (userid.HasValue)
            {
                await _notificationService.CreateAsync(new NotificationCreateDto
                {
                    UserId = userid.Value,
                    Title = "Tạo giáo viên",
                    Message = $"Bạn đã tạo giáo viên {teacher.FullName}.",
                    Type = "TEACHER"
                });
            }

            if (teacher.UserId.HasValue && teacher.UserId != userid)
            {
                await _notificationService.CreateAsync(new NotificationCreateDto
                {
                    UserId = teacher.UserId.Value,
                    Title = "Tài khoản giáo viên",
                    Message = $"Tài khoản của bạn đã được liên kết với hồ sơ giáo viên {teacher.FullName}.",
                    Type = "TEACHER"
                });
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

        public async Task<bool> UpdateAsync(int id, TeacherUpdateDto dto)
        {
            var teacher = await _teacherRepository.GetByIdAsync(id);
            if (teacher == null)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(dto.FullName))
            {
                throw new ArgumentException("Họ tên không được để trống.");
            }

            if (dto.FullName.Length > 100)
            {
                throw new ArgumentException("Họ tên không được vượt quá 100 ký tự.");
            }

            if (string.IsNullOrWhiteSpace(dto.Email))
            {
                throw new ArgumentException("Email không được để trống.");
            }

            if (!dto.Email.Contains("@"))
            {
                throw new ArgumentException("Email không hợp lệ.");
            }

            if (string.IsNullOrWhiteSpace(dto.Phone))
            {
                throw new ArgumentException("Số điện thoại không được để trống.");
            }

            if (dto.Phone.Length < 9 || dto.Phone.Length > 15)
            {
                throw new ArgumentException("Số điện thoại không hợp lệ.");
            }

            if (string.IsNullOrWhiteSpace(dto.Specialization))
            {
                throw new ArgumentException("Chuyên môn không được để trống.");
            }

            var existedEmail = await _teacherRepository.ExistsByEmailAsync(dto.Email, id);
            if (existedEmail)
            {
                throw new ArgumentException("Email đã tồn tại.");
            }

            var existedPhone = await _teacherRepository.ExistsByPhoneAsync(dto.Phone, id);
            if (existedPhone)
            {
                throw new ArgumentException("Số điện thoại đã tồn tại.");
            }

            if (dto.UserId.HasValue)
            {
                var userExists = await _userRepository.GetByIdAsync(dto.UserId.Value);
                if (userExists == null)
                {
                    throw new ArgumentException("Tài khoản User không tồn tại.");
                }

                var userAssigned = await _teacherRepository.ExistsByUserIdAsync(dto.UserId.Value, id);
                if (userAssigned)
                {
                    throw new ArgumentException("User này đã được gán cho giáo viên khác.");
                }
            }

            teacher.FullName = dto.FullName;
            teacher.Email = dto.Email;
            teacher.Phone = dto.Phone;
            teacher.Specialization = dto.Specialization;
            teacher.UserId = dto.UserId;

            await _teacherRepository.UpdateAsync(teacher);

            await _auditLogService.CreateAsync(
                userid,
                "UPDATE",
                "Teacher",
                teacher.Id,
                $"Cập nhật giáo viên {teacher.FullName} - Email: {teacher.Email}, Chuyên môn: {teacher.Specialization}",
                ipaddress);

            if (userid.HasValue)
            {
                await _notificationService.CreateAsync(new NotificationCreateDto
                {
                    UserId = userid.Value,
                    Title = "Cập nhật giáo viên",
                    Message = $"Bạn đã cập nhật thông tin giáo viên {teacher.FullName}.",
                    Type = "TEACHER"
                });
            }

            if (teacher.UserId.HasValue && teacher.UserId != userid)
            {
                await _notificationService.CreateAsync(new NotificationCreateDto
                {
                    UserId = teacher.UserId.Value,
                    Title = "Thông tin giáo viên cập nhật",
                    Message = $"Hồ sơ giáo viên {teacher.FullName} của bạn đã được cập nhật.",
                    Type = "TEACHER"
                });
            }

            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var teacher = await _teacherRepository.GetByIdAsync(id);
            if (teacher == null)
            {
                return false;
            }

            var fullName = teacher.FullName;
            var email = teacher.Email;
            var userId = teacher.UserId;

            await _teacherRepository.SoftDeleteAsync(id);

            await _auditLogService.CreateAsync(
                userid,
                "DELETE",
                "Teacher",
                id,
                $"Xóa giáo viên {fullName} - Email: {email}",
                ipaddress);

            if (userid.HasValue)
            {
                await _notificationService.CreateAsync(new NotificationCreateDto
                {
                    UserId = userid.Value,
                    Title = "Xóa giáo viên",
                    Message = $"Bạn đã xóa giáo viên {fullName}.",
                    Type = "TEACHER"
                });
            }

            if (userId.HasValue && userId.Value != userid)
            {
                await _notificationService.CreateAsync(new NotificationCreateDto
                {
                    UserId = userId.Value,
                    Title = "Tài khoản giáo viên đã bị xóa",
                    Message = $"Thông tin giáo viên {fullName} của bạn đã bị xóa.",
                    Type = "TEACHER"
                });
            }

            return true;
        }
    }
}