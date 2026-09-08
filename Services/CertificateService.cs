using EnglishCenter.API.Data;
using EnglishCenter.API.DTOs;
using EnglishCenter.API.Middleware;
using EnglishCenter.API.Models;
using Microsoft.EntityFrameworkCore;

namespace EnglishCenter.API.Services
{
    public class CertificateService : ICertificateService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditLogService _auditLogService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CertificateService(
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
        public async Task<PagedResultDto<CertificateDto>> GetAllAsync(
    string? search,
    int? studentId,
    int? courseId,
    string? sortBy,
    bool sortDesc,
    int page,
    int pageSize)
        {
            var query = _context.Certificates
                .Include(c => c.Student)
                .Include(c => c.Course)
                .AsQueryable();

            // SEARCH
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(c =>
                    c.CertificateCode.Contains(search) ||
                    c.Student.FullName.Contains(search) ||
                    c.Course.CourseName.Contains(search));
            }

            // FILTER
            if (studentId.HasValue)
            {
                query = query.Where(c =>
                    c.StudentId == studentId.Value);
            }

            if (courseId.HasValue)
            {
                query = query.Where(c =>
                    c.CourseId == courseId.Value);
            }

            // SORT
            if (!string.IsNullOrWhiteSpace(sortBy))
            {
                switch (sortBy.ToLower())
                {
                    case "id":
                        query = sortDesc
                            ? query.OrderByDescending(c => c.Id)
                            : query.OrderBy(c => c.Id);
                        break;

                    case "certificatecode":
                        query = sortDesc
                            ? query.OrderByDescending(c => c.CertificateCode)
                            : query.OrderBy(c => c.CertificateCode);
                        break;

                    case "issuedate":
                        query = sortDesc
                            ? query.OrderByDescending(c => c.IssueDate)
                            : query.OrderBy(c => c.IssueDate);
                        break;
                }
            }
            else
            {
                query = query.OrderBy(c => c.Id);
            }

            // PAGINATION
            if (page < 1)
            {
                page = 1;
            }

            if (pageSize < 1)
            {
                pageSize = 20;
            }

            var totalItems = await query.CountAsync();

            var totalPages = (int)Math.Ceiling(
                (double)totalItems / pageSize);

            var certificates = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // DTO
            var data = certificates.Select(c => new CertificateDto
            {
                Id = c.Id,
                StudentId = c.StudentId,
                StudentName = c.Student.FullName,
                CourseId = c.CourseId,
                CourseName = c.Course.CourseName,
                CertificateCode = c.CertificateCode,
                IssueDate = c.IssueDate,
                PdfFilePath = c.PdfFilePath
            }).ToList();

            return new PagedResultDto<CertificateDto>
            {
                Data = data,
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages
            };
        }

        public async Task<CertificateDto?> GetByIdAsync(int id)
        {
            var certificate = await _context.Certificates
                .Include(c => c.Student)
                .Include(c => c.Course)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (certificate == null)
            {
                return null;
            }

            return new CertificateDto
            {
                Id = certificate.Id,

                StudentId = certificate.StudentId,
                StudentName =
                    certificate.Student?.FullName ?? string.Empty,

                CourseId = certificate.CourseId,
                CourseName =
                    certificate.Course?.CourseName ?? string.Empty,

                CertificateCode = certificate.CertificateCode,
                IssueDate = certificate.IssueDate,
                PdfFilePath = certificate.PdfFilePath
            };
        }

        public async Task<CertificateDto> CreateAsync(
    CertificateCreateDto dto)
        {
            // STUDENT
            var studentExists = await _context.Students
                .AnyAsync(s => s.Id == dto.StudentId);

            if (!studentExists)
            {
                throw new ArgumentException(
                    "Student không tồn tại.");
            }

            // COURSE
            var courseExists = await _context.Courses
                .AnyAsync(c => c.Id == dto.CourseId);

            if (!courseExists)
            {
                throw new ArgumentException(
                    "Course không tồn tại.");
            }

            // CERTIFICATE CODE
            if (string.IsNullOrWhiteSpace(dto.CertificateCode))
            {
                throw new ArgumentException(
                    "Mã chứng chỉ không được để trống.");
            }

            if (dto.CertificateCode.Length > 50)
            {
                throw new ArgumentException(
                    "Mã chứng chỉ không được vượt quá 50 ký tự.");
            }

            // CHECK CODE TRÙNG
            var existedCode = await _context.Certificates
                .AnyAsync(c =>
                    c.CertificateCode == dto.CertificateCode);

            if (existedCode)
            {
                throw new ArgumentException(
                    "Mã chứng chỉ đã tồn tại.");
            }

            // ISSUE DATE
            if (dto.IssueDate > DateTime.Now)
            {
                throw new ArgumentException(
                    "Ngày cấp không được lớn hơn ngày hiện tại.");
            }

          
            // CHECK STUDENT ĐÃ HỌC COURSE
            var enrollmentExists = await _context.Enrollments
                .AnyAsync(e =>
                    e.StudentId == dto.StudentId &&
                    e.CourseId == dto.CourseId);

            if (!enrollmentExists)
            {
                throw new ArgumentException(
                    "Student chưa đăng ký khóa học này.");
            }
            // CHECK GRADE
            var grade = await _context.Grades
                .Include(g => g.Exam)
                .FirstOrDefaultAsync(g =>
                    g.StudentId == dto.StudentId &&
                    g.Exam.CourseId == dto.CourseId);

            if (grade == null)
            {
                throw new ArgumentException(
                    "Student chưa có điểm thi.");
            }

            // CHECK ĐẠT
            if (grade.Score < 5)
            {
                throw new ArgumentException(
                    "Student chưa đạt điểm để được cấp chứng chỉ.");
            }
            // CHECK ĐÃ CÓ CERTIFICATE
            var existedCertificate = await _context.Certificates
                .AnyAsync(c =>
                    c.StudentId == dto.StudentId &&
                    c.CourseId == dto.CourseId);

            if (existedCertificate)
            {
                throw new ArgumentException(
                    "Student đã có chứng chỉ cho khóa học này.");
            }

            var certificate = new Certificate
            {
                StudentId = dto.StudentId,
                CourseId = dto.CourseId,
                CertificateCode = dto.CertificateCode,
                IssueDate = dto.IssueDate,
                PdfFilePath = dto.PdfFilePath ?? string.Empty
            };

            _context.Certificates.Add(certificate);

            await _context.SaveChangesAsync();

            await _auditLogService.CreateAsync(
                userid,
                "CREATE",
                "Certificate",
                certificate.Id,
                $"Cấp chứng chỉ {certificate.CertificateCode} cho Student ID {certificate.StudentId}",
                ipaddress);

            return new CertificateDto
            {
                Id = certificate.Id,
                StudentId = certificate.StudentId,
                CourseId = certificate.CourseId,
                CertificateCode = certificate.CertificateCode,
                IssueDate = certificate.IssueDate,
                PdfFilePath = certificate.PdfFilePath
            };
        }

        public async Task<bool> UpdateAsync(
     int id,
     CertificateUpdateDto dto)
        {
            // KIỂM TRA CERTIFICATE
            var certificate = await _context.Certificates
                .FindAsync(id);

            if (certificate == null)
            {
                return false;
            }

            // STUDENT
            var studentExists = await _context.Students
                .AnyAsync(s => s.Id == dto.StudentId);

            if (!studentExists)
            {
                throw new ArgumentException(
                    "Student không tồn tại.");
            }

            // COURSE
            var courseExists = await _context.Courses
                .AnyAsync(c => c.Id == dto.CourseId);

            if (!courseExists)
            {
                throw new ArgumentException(
                    "Course không tồn tại.");
            }

            // CERTIFICATE CODE
            if (string.IsNullOrWhiteSpace(dto.CertificateCode))
            {
                throw new ArgumentException(
                    "Mã chứng chỉ không được để trống.");
            }

            if (dto.CertificateCode.Length > 50)
            {
                throw new ArgumentException(
                    "Mã chứng chỉ không được vượt quá 50 ký tự.");
            }

            // CHECK CODE TRÙNG
            var existedCode = await _context.Certificates
                .AnyAsync(c =>
                    c.Id != id &&
                    c.CertificateCode == dto.CertificateCode);

            if (existedCode)
            {
                throw new ArgumentException(
                    "Mã chứng chỉ đã tồn tại.");
            }

            // ISSUE DATE
            if (dto.IssueDate > DateTime.Now)
            {
                throw new ArgumentException(
                    "Ngày cấp không được lớn hơn ngày hiện tại.");
            }

            // PDF
            if (string.IsNullOrWhiteSpace(dto.PdfFilePath))
            {
                throw new ArgumentException(
                    "Đường dẫn file PDF không được để trống.");
            }

            // CHECK STUDENT ĐÃ ĐĂNG KÝ COURSE
            var enrollmentExists = await _context.Enrollments
                .AnyAsync(e =>
                    e.StudentId == dto.StudentId &&
                    e.CourseId == dto.CourseId);

            if (!enrollmentExists)
            {
                throw new ArgumentException(
                    "Student chưa đăng ký khóa học này.");
            }
            var grade = await _context.Grades
    .Include(g => g.Exam)
    .FirstOrDefaultAsync(g =>
        g.StudentId == dto.StudentId &&
        g.Exam.CourseId == dto.CourseId);

            if (grade == null)
            {
                throw new ArgumentException(
                    "Student chưa có điểm thi.");
            }

            if (grade.Score < 5)
            {
                throw new ArgumentException(
                    "Student chưa đạt điểm để được cấp chứng chỉ.");
            }
            // CHECK CERTIFICATE TRÙNG STUDENT + COURSE
            var existedCertificate = await _context.Certificates
                .AnyAsync(c =>
                    c.Id != id &&
                    c.StudentId == dto.StudentId &&
                    c.CourseId == dto.CourseId);

            if (existedCertificate)
            {
                throw new ArgumentException(
                    "Student đã có chứng chỉ cho khóa học này.");
            }


            // UPDATE
            certificate.StudentId = dto.StudentId;
            certificate.CourseId = dto.CourseId;
            certificate.CertificateCode = dto.CertificateCode;
            certificate.IssueDate = dto.IssueDate;
            certificate.PdfFilePath = dto.PdfFilePath;
            await _context.SaveChangesAsync();

            await _auditLogService.CreateAsync(
                userid,
                "UPDATE",
                "Certificate",
                certificate.Id,
                $"Cập nhật chứng chỉ {certificate.CertificateCode}",
                ipaddress);

            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var certificate = await _context.Certificates
                .FindAsync(id);

            if (certificate == null)
            {
                return false;
            }

            var certificateCode = certificate.CertificateCode;

            certificate.IsDeleted = true;

            await _context.SaveChangesAsync();

            await _auditLogService.CreateAsync(
               userid,
                "DELETE",
                "Certificate",
                id,
                $"Xóa chứng chỉ {certificateCode}",
                ipaddress);

            return true;
        }
    }
}