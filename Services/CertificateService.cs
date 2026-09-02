using EnglishCenter.API.Data;
using EnglishCenter.API.DTOs;
using EnglishCenter.API.Models;
using Microsoft.EntityFrameworkCore;

namespace EnglishCenter.API.Services
{
    public class CertificateService : ICertificateService
    {
        private readonly ApplicationDbContext _context;

        public CertificateService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<CertificateDto>> GetAllAsync()
        {
            var certificates = await _context.Certificates
                .Include(c => c.Student)
                .Include(c => c.Course)
                .ToListAsync();

            return certificates.Select(c => new CertificateDto
            {
                Id = c.Id,

                StudentId = c.StudentId,
                StudentName = c.Student?.FullName ?? string.Empty,

                CourseId = c.CourseId,
                CourseName = c.Course?.CourseName ?? string.Empty,

                CertificateCode = c.CertificateCode,
                IssueDate = c.IssueDate,
                PdfFilePath = c.PdfFilePath
            }).ToList();
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
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.Id == dto.StudentId);

            if (student == null)
            {
                throw new ArgumentException(
                    "Student không tồn tại.");
            }

            var course = await _context.Courses
                .FirstOrDefaultAsync(c => c.Id == dto.CourseId);

            if (course == null)
            {
                throw new ArgumentException(
                    "Course không tồn tại.");
            }

            var codeExists = await _context.Certificates
                .AnyAsync(c =>
                    c.CertificateCode == dto.CertificateCode);

            if (codeExists)
            {
                throw new ArgumentException(
                    "Mã chứng chỉ đã tồn tại.");
            }

            var certificate = new Certificate
            {
                StudentId = dto.StudentId,
                CourseId = dto.CourseId,
                CertificateCode = dto.CertificateCode,
                IssueDate = dto.IssueDate,
                PdfFilePath = dto.PdfFilePath
            };

            _context.Certificates.Add(certificate);

            await _context.SaveChangesAsync();

            return new CertificateDto
            {
                Id = certificate.Id,

                StudentId = certificate.StudentId,
                StudentName = student.FullName,

                CourseId = certificate.CourseId,
                CourseName = course.CourseName,

                CertificateCode =
                    certificate.CertificateCode,

                IssueDate = certificate.IssueDate,

                PdfFilePath =
                    certificate.PdfFilePath
            };
        }

        public async Task<bool> UpdateAsync(
            int id,
            CertificateUpdateDto dto)
        {
            var certificate = await _context.Certificates
                .FindAsync(id);

            if (certificate == null)
            {
                return false;
            }

            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.Id == dto.StudentId);

            if (student == null)
            {
                throw new ArgumentException(
                    "Student không tồn tại.");
            }

            var course = await _context.Courses
                .FirstOrDefaultAsync(c => c.Id == dto.CourseId);

            if (course == null)
            {
                throw new ArgumentException(
                    "Course không tồn tại.");
            }

            var codeExists = await _context.Certificates
                .AnyAsync(c =>
                    c.CertificateCode == dto.CertificateCode
                    && c.Id != id);

            if (codeExists)
            {
                throw new ArgumentException(
                    "Mã chứng chỉ đã tồn tại.");
            }

            certificate.StudentId = dto.StudentId;
            certificate.CourseId = dto.CourseId;
            certificate.CertificateCode =
                dto.CertificateCode;
            certificate.IssueDate = dto.IssueDate;
            certificate.PdfFilePath =
                dto.PdfFilePath;

            await _context.SaveChangesAsync();

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

            _context.Certificates.Remove(certificate);

            await _context.SaveChangesAsync();

            return true;
        }
    }
}