using ClosedXML.Excel;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using EnglishCenter.API.Data;
using EnglishCenter.API.DTOs;
using EnglishCenter.API.Middleware;
using EnglishCenter.API.Models;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace EnglishCenter.API.Services
{
    public class StudentService : IStudentService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditLogService _auditLogService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public StudentService(
            ApplicationDbContext context,
            IAuditLogService auditLogService, IHttpContextAccessor httpContextAccessor)
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

        public async Task<StudentDto> CreateAsync(StudentCreateDto dto)
        {
            // FULL NAME
            if (string.IsNullOrWhiteSpace(dto.FullName))
            {
                throw new ArgumentException(
                    "Họ tên không được để trống.");
            }

            if (dto.FullName.Length > 100)
            {
                throw new ArgumentException(
                    "Họ tên không được vượt quá 100 ký tự.");
            }

            // DATE OF BIRTH
            if (dto.DateOfBirth > DateTime.Now)
            {
                throw new ArgumentException(
                    "Ngày sinh không được lớn hơn ngày hiện tại.");
            }

            // EMAIL
            if (string.IsNullOrWhiteSpace(dto.Email))
            {
                throw new ArgumentException(
                    "Email không được để trống.");
            }

            if (!dto.Email.Contains("@"))
            {
                throw new ArgumentException(
                    "Email không hợp lệ.");
            }

            // PHONE
            if (string.IsNullOrWhiteSpace(dto.Phone))
            {
                throw new ArgumentException(
                    "Số điện thoại không được để trống.");
            }

            if (dto.Phone.Length < 9 || dto.Phone.Length > 15)
            {
                throw new ArgumentException(
                    "Số điện thoại không hợp lệ.");
            }

            // ADDRESS
            if (string.IsNullOrWhiteSpace(dto.Address))
            {
                throw new ArgumentException(
                    "Địa chỉ không được để trống.");
            }

            // CHECK EMAIL
            var existedEmail = await _context.Students
                .AnyAsync(s => s.Email == dto.Email);

            if (existedEmail)
            {
                throw new ArgumentException(
                    "Email đã tồn tại.");
            }

            // CHECK PHONE
            var existedPhone = await _context.Students
                .AnyAsync(s => s.Phone == dto.Phone);

            if (existedPhone)
            {
                throw new ArgumentException(
                    "Số điện thoại đã tồn tại.");
            }

            // CHECK USER
            if (dto.UserId.HasValue)
            {
                var userExists = await _context.Users
                    .AnyAsync(u => u.Id == dto.UserId.Value);

                if (!userExists)
                {
                    throw new ArgumentException(
                        "User không tồn tại.");
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

            await _auditLogService.CreateAsync(
                userid,
                "CREATE",
                "Student",
                student.Id,
                $"Tạo Student {student.FullName} - Email: {student.Email}",
                ipaddress);

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
            // KIỂM TRA STUDENT
            var student = await _context.Students
                .FindAsync(id);

            if (student == null)
            {
                return false;
            }

            // FULL NAME
            if (string.IsNullOrWhiteSpace(dto.FullName))
            {
                throw new ArgumentException(
                    "Họ tên không được để trống.");
            }

            if (dto.FullName.Length > 100)
            {
                throw new ArgumentException(
                    "Họ tên không được vượt quá 100 ký tự.");
            }

            // DATE OF BIRTH
            if (dto.DateOfBirth > DateTime.Now)
            {
                throw new ArgumentException(
                    "Ngày sinh không được lớn hơn ngày hiện tại.");
            }

            // EMAIL
            if (string.IsNullOrWhiteSpace(dto.Email))
            {
                throw new ArgumentException(
                    "Email không được để trống.");
            }

            if (!dto.Email.Contains("@"))
            {
                throw new ArgumentException(
                    "Email không hợp lệ.");
            }

            // PHONE
            if (string.IsNullOrWhiteSpace(dto.Phone))
            {
                throw new ArgumentException(
                    "Số điện thoại không được để trống.");
            }

            if (dto.Phone.Length < 9 || dto.Phone.Length > 10)
            {
                throw new ArgumentException(
                    "Số điện thoại không hợp lệ.");
            }

            // ADDRESS
            if (string.IsNullOrWhiteSpace(dto.Address))
            {
                throw new ArgumentException(
                    "Địa chỉ không được để trống.");
            }

            // CHECK EMAIL TRÙNG
            var existedEmail = await _context.Students
                .AnyAsync(s =>
                    s.Id != id &&
                    s.Email == dto.Email);

            if (existedEmail)
            {
                throw new ArgumentException(
                    "Email đã tồn tại.");
            }

            // CHECK PHONE TRÙNG
            var existedPhone = await _context.Students
                .AnyAsync(s =>
                    s.Id != id &&
                    s.Phone == dto.Phone);

            if (existedPhone)
            {
                throw new ArgumentException(
                    "Số điện thoại đã tồn tại.");
            }

            // CHECK USER
            if (dto.UserId.HasValue)
            {
                var userExists = await _context.Users
                    .AnyAsync(u => u.Id == dto.UserId.Value);

                if (!userExists)
                {
                    throw new ArgumentException(
                        "User không tồn tại.");
                }
            }

            // UPDATE
            student.FullName = dto.FullName;
            student.DateOfBirth = dto.DateOfBirth;
            student.Email = dto.Email;
            student.Phone = dto.Phone;
            student.Address = dto.Address;
            student.UserId = dto.UserId;

            await _context.SaveChangesAsync();

            await _auditLogService.CreateAsync(
                userid,
                "UPDATE",
                "Student",
                student.Id,
                $"Cập nhật thông tin Student {student.FullName}",
                ipaddress);

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

            var userId = student.UserId;
            var fullName = student.FullName;
            var email = student.Email;

            _context.Students.Remove(student);

            await _context.SaveChangesAsync();

            await _auditLogService.CreateAsync(
                userId,
                "DELETE",
                "Student",
                id,
                $"Xóa Student {fullName} - Email: {email}",
                ipaddress);

            return true;
        }
        public async Task<ExcelImportResultDto> ImportExcelAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("File Excel không được để trống.");
            }

            if (!Path.GetExtension(file.FileName)
                .Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                    "Chỉ hỗ trợ file Excel có định dạng .xlsx.");
            }

            var result = new ExcelImportResultDto();
            var studentsToAdd = new List<Student>();

            using var stream = new MemoryStream();

            await file.CopyToAsync(stream);
            stream.Position = 0;

            using var workbook = new XLWorkbook(stream);
            var worksheet = workbook.Worksheet(1);
            var headers = new[]
{
    "FullName",
    "DateOfBirth",
    "Email",
    "Phone",
    "Address",
    "UserId"
};

            for (int i = 0; i < headers.Length; i++)
            {
                var actualHeader = worksheet
                    .Cell(1, i + 1)
                    .GetString()
                    .Trim();

                if (!string.Equals(
                    actualHeader,
                    headers[i],
                    StringComparison.OrdinalIgnoreCase))
                {
                    throw new ArgumentException(
                        $"Cột {i + 1} phải là '{headers[i]}'.");
                }
            }

            var rows = worksheet.RowsUsed().Skip(1);

            foreach (var row in rows)
            {
                result.TotalRows++;

                try
                {
                    var fullName = row.Cell(1).GetString().Trim();
                    var dateOfBirthText = row.Cell(2).GetString().Trim();
                    var email = row.Cell(3).GetString().Trim();
                    var phone = row.Cell(4).GetString().Trim();
                    var address = row.Cell(5).GetString().Trim();
                    var userIdText = row.Cell(6).GetString().Trim();
                    if (string.IsNullOrWhiteSpace(fullName))
                    {
                        throw new ArgumentException(
                            "Họ tên không được để trống.");
                    }
                    // FULL NAME
                    if (fullName.Length < 2 || fullName.Length > 100)
                    {
                        throw new ArgumentException(
                            "Họ tên phải từ 2 đến 100 ký tự.");
                    }

                    // EMAIL
                    if (!System.Text.RegularExpressions.Regex.IsMatch(
                        email,
                        @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                    {
                        throw new ArgumentException(
                            "Email không đúng định dạng.");
                    }

                    // PHONE
                    if (!System.Text.RegularExpressions.Regex.IsMatch(
                        phone,
                        @"^0\d{9,10}$"))
                    {
                        throw new ArgumentException(
                            "Số điện thoại phải bắt đầu bằng 0 và có 10-11 số.");
                    }

                    // DATE OF BIRTH
                    if (!DateTime.TryParse(
                        dateOfBirthText,
                        out DateTime dateOfBirth))
                    {
                        throw new ArgumentException(
                            "Ngày sinh không hợp lệ.");
                    }

                    if (dateOfBirth > DateTime.Now)
                    {
                        throw new ArgumentException(
                            "Ngày sinh không được lớn hơn ngày hiện tại.");
                    }

                    var age = DateTime.Now.Year - dateOfBirth.Year;

                    if (dateOfBirth.Date > DateTime.Now.AddYears(-age).Date)
                    {
                        age--;
                    }

                    if (age < 6 || age > 100)
                    {
                        throw new ArgumentException(
                            "Tuổi phải nằm trong khoảng từ 6 đến 100.");
                    }

                    // ADDRESS
                    if (address.Length > 200)
                    {
                        throw new ArgumentException(
                            "Địa chỉ không được vượt quá 200 ký tự.");
                    }
                    // FULL NAME
                   

                    // EMAIL
                    if (string.IsNullOrWhiteSpace(email))
                    {
                        throw new ArgumentException(
                            "Email không được để trống.");
                    }

                    // PHONE
                    if (string.IsNullOrWhiteSpace(phone))
                    {
                        throw new ArgumentException(
                            "Số điện thoại không được để trống.");
                    }

                    // DATE OF BIRTH
                  

                    // USER ID
                    if (!int.TryParse(userIdText, out int userId))
                    {
                        throw new ArgumentException(
                            "UserId không hợp lệ.");
                    }

                    // CHECK EMAIL TRÙNG
                    var emailExists = await _context.Students
                        .AnyAsync(s => s.Email == email);

                    if (emailExists)
                    {
                        throw new ArgumentException(
                            "Email đã tồn tại.");
                    }

                    // CHECK PHONE TRÙNG
                    var phoneExists = await _context.Students
                        .AnyAsync(s => s.Phone == phone);

                    if (phoneExists)
                    {
                        throw new ArgumentException(
                            "Số điện thoại đã tồn tại.");
                    }

                    // CHECK USER
                    var userExists = await _context.Users
                        .AnyAsync(u => u.Id == userId);

                    if (!userExists)
                    {
                        throw new ArgumentException(
                            "UserId không tồn tại.");
                    }

                    // CHECK USER ĐÃ GÁN STUDENT
                    var userUsed = await _context.Students
                        .AnyAsync(s => s.UserId == userId);

                    if (userUsed)
                    {
                        throw new ArgumentException(
                            "User này đã được gán cho Student khác.");
                    }

                    var student = new Student
                    {
                        FullName = fullName,
                        DateOfBirth = dateOfBirth,
                        Email = email,
                        Phone = phone,
                        Address = address,
                        UserId = userId
                    };

                    studentsToAdd.Add(student);


                }
                catch (Exception ex)
                {
                    result.FailedRows++;

                    result.Errors.Add(
                        $"Dòng {row.RowNumber()}: {ex.Message}");
                }
            }
            if (studentsToAdd.Count > 0)
            {
                await _context.Students.AddRangeAsync(studentsToAdd);

                await _context.SaveChangesAsync();

                result.SuccessRows = studentsToAdd.Count;
            }
           
            await _auditLogService.CreateAsync(
    userid,
    "IMPORT",
    "Student",
   null,
    $"Import Excel Student: thành công {result.SuccessRows}/{result.TotalRows} dòng, lỗi {result.FailedRows} dòng.",
    ipaddress);

            return result;
        }
    }
}