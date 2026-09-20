using ClosedXML.Excel;
using EnglishCenter.API.DTOs;
using EnglishCenter.API.Middleware;
using EnglishCenter.API.Models;
using EnglishCenter.Application.Abstractions.Persistence;
using System.Text.RegularExpressions;

namespace EnglishCenter.API.Services
{
    public class StudentService : IStudentService
    {
        private readonly IStudentRepository _studentRepository;
        private readonly IUserRepository _userRepository;
        private readonly IAuditLogService _auditLogService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly INotificationService _notificationService;

        public StudentService(
            IStudentRepository studentRepository,
            IUserRepository userRepository,
            IAuditLogService auditLogService,
            IHttpContextAccessor httpContextAccessor,
            INotificationService notificationService)
        {
            _studentRepository = studentRepository;
            _userRepository = userRepository;
            _auditLogService = auditLogService;
            _httpContextAccessor = httpContextAccessor;
            _notificationService = notificationService;
        }

        private int? userid => AuditContext.GetUserId(_httpContextAccessor.HttpContext!);
        private string? ipaddress => AuditContext.GetIPAddress(_httpContextAccessor.HttpContext!);

        public async Task<PagedResultDto<StudentDto>> GetAllAsync(
            string? search,
            DateTime? fromDateOfBirth,
            DateTime? toDateOfBirth,
            string? sortBy,
            bool sortDesc,
            int page,
            int pageSize)
        {
            var (students, totalItems) = await _studentRepository.GetAllAsync(
                search, fromDateOfBirth, toDateOfBirth, sortBy, sortDesc, page, pageSize);

            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 20 : pageSize;
            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            return new PagedResultDto<StudentDto>
            {
                Data = students.Select(s => new StudentDto
                {
                    Id = s.Id,
                    FullName = s.FullName,
                    DateOfBirth = s.DateOfBirth,
                    Email = s.Email,
                    Phone = s.Phone,
                    Address = s.Address,
                    UserId = s.UserId
                }).ToList(),
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages
            };
        }

        public async Task<StudentDto?> GetByIdAsync(int id)
        {
            var student = await _studentRepository.GetByIdAsync(id);
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
                UserName = student.User?.UserName
            };
        }

        public async Task<StudentDto> CreateAsync(StudentCreateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.FullName))
            {
                throw new ArgumentException("Họ tên không được để trống.");
            }

            if (dto.FullName.Length > 100)
            {
                throw new ArgumentException("Họ tên không được vượt quá 100 ký tự.");
            }

            if (dto.DateOfBirth > DateTime.Now)
            {
                throw new ArgumentException("Ngày sinh không được lớn hơn ngày hiện tại.");
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

            if (string.IsNullOrWhiteSpace(dto.Address))
            {
                throw new ArgumentException("Địa chỉ không được để trống.");
            }

            var existedEmail = await _studentRepository.ExistsByEmailAsync(dto.Email);
            if (existedEmail)
            {
                throw new ArgumentException("Email đã tồn tại.");
            }

            var existedPhone = await _studentRepository.ExistsByPhoneAsync(dto.Phone);
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

                var userAssigned = await _studentRepository.ExistsByUserIdAsync(dto.UserId.Value);
                if (userAssigned)
                {
                    throw new ArgumentException("User này đã được gán cho học sinh khác.");
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

            await _studentRepository.CreateAsync(student);

            await _auditLogService.CreateAsync(
                userid,
                "CREATE",
                "Student",
                student.Id,
                $"Tạo Student {student.FullName} - Email: {student.Email}",
                ipaddress);

            if (userid.HasValue)
            {
                await _notificationService.CreateAsync(new NotificationCreateDto
                {
                    UserId = userid.Value,
                    Title = "Tạo học sinh",
                    Message = $"Bạn đã tạo thông tin học sinh {student.FullName}.",
                    Type = "STUDENT"
                });
            }

            if (student.UserId.HasValue && student.UserId != userid)
            {
                await _notificationService.CreateAsync(new NotificationCreateDto
                {
                    UserId = student.UserId.Value,
                    Title = "Thông tin học sinh",
                    Message = $"Tài khoản của bạn đã được liên kết với hồ sơ học sinh {student.FullName}.",
                    Type = "STUDENT"
                });
            }

            return new StudentDto
            {
                Id = student.Id,
                FullName = student.FullName,
                DateOfBirth = student.DateOfBirth,
                Email = student.Email,
                Phone = student.Phone,
                Address = student.Address,
                UserId = student.UserId
            };
        }

        public async Task<bool> UpdateAsync(int id, StudentUpdateDto dto)
        {
            var student = await _studentRepository.GetByIdAsync(id);
            if (student == null)
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

            if (dto.DateOfBirth > DateTime.Now)
            {
                throw new ArgumentException("Ngày sinh không được lớn hơn ngày hiện tại.");
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

            if (string.IsNullOrWhiteSpace(dto.Address))
            {
                throw new ArgumentException("Địa chỉ không được để trống.");
            }

            var existedEmail = await _studentRepository.ExistsByEmailAsync(dto.Email, id);
            if (existedEmail)
            {
                throw new ArgumentException("Email đã tồn tại.");
            }

            var existedPhone = await _studentRepository.ExistsByPhoneAsync(dto.Phone, id);
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

                var userAssigned = await _studentRepository.ExistsByUserIdAsync(dto.UserId.Value, id);
                if (userAssigned)
                {
                    throw new ArgumentException("User này đã được gán cho học sinh khác.");
                }
            }

            student.FullName = dto.FullName;
            student.DateOfBirth = dto.DateOfBirth;
            student.Email = dto.Email;
            student.Phone = dto.Phone;
            student.Address = dto.Address;
            student.UserId = dto.UserId;

            await _studentRepository.UpdateAsync(student);

            await _auditLogService.CreateAsync(
                userid,
                "UPDATE",
                "Student",
                student.Id,
                $"Cập nhật Student {student.FullName} - Email: {student.Email}",
                ipaddress);

            if (userid.HasValue)
            {
                await _notificationService.CreateAsync(new NotificationCreateDto
                {
                    UserId = userid.Value,
                    Title = "Cập nhật học sinh",
                    Message = $"Bạn đã cập nhật thông tin học sinh {student.FullName}.",
                    Type = "STUDENT"
                });
            }

            if (student.UserId.HasValue && student.UserId != userid)
            {
                await _notificationService.CreateAsync(new NotificationCreateDto
                {
                    UserId = student.UserId.Value,
                    Title = "Thông tin học sinh cập nhật",
                    Message = $"Hồ sơ học sinh {student.FullName} của bạn đã được cập nhật.",
                    Type = "STUDENT"
                });
            }

            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var student = await _studentRepository.GetByIdAsync(id);
            if (student == null)
            {
                return false;
            }

            var fullName = student.FullName;
            var email = student.Email;
            var userId = student.UserId;

            await _studentRepository.SoftDeleteAsync(id);

            await _auditLogService.CreateAsync(
                userid,
                "DELETE",
                "Student",
                id,
                $"Xóa Student {fullName} - Email: {email}",
                ipaddress);

            if (userid.HasValue)
            {
                await _notificationService.CreateAsync(new NotificationCreateDto
                {
                    UserId = userid.Value,
                    Title = "Xóa học sinh",
                    Message = $"Bạn đã xóa học sinh {fullName}.",
                    Type = "STUDENT"
                });
            }

            if (userId.HasValue && userId.Value != userid)
            {
                await _notificationService.CreateAsync(new NotificationCreateDto
                {
                    UserId = userId.Value,
                    Title = "Tài khoản học sinh đã bị xóa",
                    Message = $"Thông tin học sinh {fullName} của bạn đã bị xóa.",
                    Type = "STUDENT"
                });
            }

            return true;
        }

        public async Task<ExcelImportResultDto> ValidateImportExcelAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("File Excel không được để trống.");
            }

            if (Path.GetExtension(file.FileName).ToLower() != ".xlsx")
            {
                throw new ArgumentException("Chỉ hỗ trợ file Excel có định dạng .xlsx.");
            }

            var result = new ExcelImportResultDto();
            var studentsToAdd = new List<Student>();

            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            stream.Position = 0;

            using var workbook = new XLWorkbook(stream);
            var worksheet = workbook.Worksheet(1);

            var expectedHeaders = new[]
            {
                "FullName", "DateOfBirth", "Email", "Phone", "Address", "UserId"
            };

            for (int i = 0; i < expectedHeaders.Length; i++)
            {
                var header = worksheet.Cell(1, i + 1).GetString().Trim();
                if (!string.Equals(header, expectedHeaders[i], StringComparison.OrdinalIgnoreCase))
                {
                    throw new ArgumentException($"Header cột {i + 1} không đúng. Yêu cầu: {expectedHeaders[i]}.");
                }
            }

            int lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;

            for (int row = 2; row <= lastRow; row++)
            {
                var fullName = worksheet.Cell(row, 1).GetString().Trim();
                var dateOfBirthText = worksheet.Cell(row, 2).GetString().Trim();
                var email = worksheet.Cell(row, 3).GetString().Trim();
                var phone = worksheet.Cell(row, 4).GetString().Trim();
                var address = worksheet.Cell(row, 5).GetString().Trim();
                var userIdText = worksheet.Cell(row, 6).GetString().Trim();

                if (string.IsNullOrWhiteSpace(fullName) || fullName.Length < 2 || fullName.Length > 100)
                {
                    result.Errors.Add($"Dòng {row}: Họ tên phải từ 2 đến 100 ký tự.");
                    continue;
                }

                if (!DateTime.TryParse(dateOfBirthText, out DateTime dateOfBirth) || dateOfBirth > DateTime.Now)
                {
                    result.Errors.Add($"Dòng {row}: Ngày sinh không hợp lệ.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
                {
                    result.Errors.Add($"Dòng {row}: Email không hợp lệ.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(phone) || phone.Length < 9 || phone.Length > 15)
                {
                    result.Errors.Add($"Dòng {row}: Số điện thoại không hợp lệ.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(address))
                {
                    result.Errors.Add($"Dòng {row}: Địa chỉ không được để trống.");
                    continue;
                }

                if (!int.TryParse(userIdText, out int studentUserId) || studentUserId <= 0)
                {
                    result.Errors.Add($"Dòng {row}: UserId không hợp lệ.");
                    continue;
                }

                bool emailExists = await _studentRepository.ExistsByEmailAsync(email);
                bool emailInCurrentImport = studentsToAdd.Any(s => s.Email == email);
                if (emailExists || emailInCurrentImport)
                {
                    result.Errors.Add($"Dòng {row}: Email {email} đã tồn tại.");
                    continue;
                }

                bool phoneExists = await _studentRepository.ExistsByPhoneAsync(phone);
                bool phoneInCurrentImport = studentsToAdd.Any(s => s.Phone == phone);
                if (phoneExists || phoneInCurrentImport)
                {
                    result.Errors.Add($"Dòng {row}: Số điện thoại {phone} đã tồn tại.");
                    continue;
                }

                var user = await _userRepository.GetByIdAsync(studentUserId);
                if (user == null)
                {
                    result.Errors.Add($"Dòng {row}: UserId {studentUserId} không tồn tại.");
                    continue;
                }

                bool userAlreadyAssigned = await _studentRepository.ExistsByUserIdAsync(studentUserId);
                bool userInCurrentImport = studentsToAdd.Any(s => s.UserId == studentUserId);
                if (userAlreadyAssigned || userInCurrentImport)
                {
                    result.Errors.Add($"Dòng {row}: UserId {studentUserId} đã được gán cho học sinh khác.");
                    continue;
                }

                studentsToAdd.Add(new Student
                {
                    FullName = fullName,
                    DateOfBirth = dateOfBirth,
                    Email = email,
                    Phone = phone,
                    Address = address,
                    UserId = studentUserId
                });

                result.SuccessRows++;
            }

            return result;
        }

        public async Task<ExcelImportResultDto> ImportExcelAsync(
            IFormFile file,
            int userId,
            string? ipAddress)
        {
            var result = await ValidateImportExcelAsync(file);
            if (result.Errors.Count > 0)
            {
                throw new ArgumentException("File Excel không hợp lệ.");
            }

            var studentsToAdd = new List<Student>();
            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            stream.Position = 0;

            using var workbook = new XLWorkbook(stream);
            var worksheet = workbook.Worksheet(1);
            int lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;

            for (int row = 2; row <= lastRow; row++)
            {
                var fullName = worksheet.Cell(row, 1).GetString().Trim();
                var dateOfBirthText = worksheet.Cell(row, 2).GetString().Trim();
                var email = worksheet.Cell(row, 3).GetString().Trim();
                var phone = worksheet.Cell(row, 4).GetString().Trim();
                var address = worksheet.Cell(row, 5).GetString().Trim();
                var userIdText = worksheet.Cell(row, 6).GetString().Trim();

                DateTime.TryParse(dateOfBirthText, out DateTime dateOfBirth);
                int.TryParse(userIdText, out int studentUserId);

                studentsToAdd.Add(new Student
                {
                    FullName = fullName,
                    DateOfBirth = dateOfBirth,
                    Email = email,
                    Phone = phone,
                    Address = address,
                    UserId = studentUserId
                });
            }

            if (studentsToAdd.Count > 0)
            {
                await _studentRepository.CreateRangeAsync(studentsToAdd);
            }

            await _auditLogService.CreateAsync(
                userId,
                "IMPORT",
                "Student",
                null,
                $"Import Excel học sinh. Thành công: {result.SuccessRows}, Lỗi: {result.Errors.Count}.",
                ipAddress);

            await _notificationService.CreateForUserAsync(new NotificationCreateDto
            {
                UserId = userId,
                Title = "Import học sinh",
                Message = $"Import hoàn tất. Thành công: {result.SuccessRows}, Lỗi: {result.Errors.Count}.",
                Type = "IMPORT"
            });

            return result;
        }
    }
}