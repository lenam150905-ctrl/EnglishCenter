using EnglishCenter.API.Data;
using EnglishCenter.API.DTOs;
using EnglishCenter.API.Models;
using Microsoft.EntityFrameworkCore;

namespace EnglishCenter.API.Services
{
    public class InvoiceService : IInvoiceService
    {
        private readonly ApplicationDbContext _context;

        public InvoiceService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<InvoiceDto>> GetAllAsync()
        {
            var invoices = await _context.Invoices
                .Include(i => i.Student)
                .Include(i => i.Enrollment)
                    .ThenInclude(e => e.Course)
                .ToListAsync();

            return invoices.Select(i => new InvoiceDto
            {
                Id = i.Id,

                StudentId = i.StudentId,
                StudentName = i.Student?.FullName ?? string.Empty,

                EnrollmentId = i.EnrollmentId,
                CourseName = i.Enrollment?.Course?.CourseName,

                Amount = i.Amount,
                InvoiceDate = i.InvoiceDate,
                Status = i.Status
            }).ToList();
        }

        public async Task<InvoiceDto?> GetByIdAsync(int id)
        {
            var invoice = await _context.Invoices
                .Include(i => i.Student)
                .Include(i => i.Enrollment)
                    .ThenInclude(e => e.Course)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (invoice == null)
            {
                return null;
            }

            return new InvoiceDto
            {
                Id = invoice.Id,

                StudentId = invoice.StudentId,
                StudentName = invoice.Student?.FullName ?? string.Empty,

                EnrollmentId = invoice.EnrollmentId,
                CourseName = invoice.Enrollment?.Course?.CourseName,

                Amount = invoice.Amount,
                InvoiceDate = invoice.InvoiceDate,
                Status = invoice.Status
            };
        }

        public async Task<InvoiceDto> CreateAsync(
            InvoiceCreateDto dto)
        {
            var student = await _context.Students
                .FirstOrDefaultAsync(
                    s => s.Id == dto.StudentId);

            if (student == null)
            {
                throw new ArgumentException(
                    "Student không tồn tại.");
            }

            Enrollment? enrollment = null;

            if (dto.EnrollmentId.HasValue)
            {
                enrollment = await _context.Enrollments
                    .Include(e => e.Course)
                    .FirstOrDefaultAsync(
                        e => e.Id == dto.EnrollmentId.Value);

                if (enrollment == null)
                {
                    throw new ArgumentException(
                        "Enrollment không tồn tại.");
                }

                if (enrollment.StudentId != dto.StudentId)
                {
                    throw new ArgumentException(
                        "Enrollment không thuộc Student này.");
                }
            }

            var invoice = new Invoice
            {
                StudentId = dto.StudentId,
                EnrollmentId = dto.EnrollmentId,
                Amount = dto.Amount,
                InvoiceDate = dto.InvoiceDate,
                Status = dto.Status
            };

            _context.Invoices.Add(invoice);

            await _context.SaveChangesAsync();

            return new InvoiceDto
            {
                Id = invoice.Id,

                StudentId = invoice.StudentId,
                StudentName = student.FullName,

                EnrollmentId = invoice.EnrollmentId,
                CourseName = enrollment?.Course?.CourseName,

                Amount = invoice.Amount,
                InvoiceDate = invoice.InvoiceDate,
                Status = invoice.Status
            };
        }

        public async Task<bool> UpdateAsync(
            int id,
            InvoiceUpdateDto dto)
        {
            var invoice = await _context.Invoices
                .FindAsync(id);

            if (invoice == null)
            {
                return false;
            }

            var student = await _context.Students
                .FirstOrDefaultAsync(
                    s => s.Id == dto.StudentId);

            if (student == null)
            {
                throw new ArgumentException(
                    "Student không tồn tại.");
            }

            if (dto.EnrollmentId.HasValue)
            {
                var enrollment = await _context.Enrollments
                    .FirstOrDefaultAsync(
                        e => e.Id == dto.EnrollmentId.Value);

                if (enrollment == null)
                {
                    throw new ArgumentException(
                        "Enrollment không tồn tại.");
                }

                if (enrollment.StudentId != dto.StudentId)
                {
                    throw new ArgumentException(
                        "Enrollment không thuộc Student này.");
                }
            }

            invoice.StudentId = dto.StudentId;
            invoice.EnrollmentId = dto.EnrollmentId;
            invoice.Amount = dto.Amount;
            invoice.InvoiceDate = dto.InvoiceDate;
            invoice.Status = dto.Status;

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var invoice = await _context.Invoices
                .FindAsync(id);

            if (invoice == null)
            {
                return false;
            }

            _context.Invoices.Remove(invoice);

            await _context.SaveChangesAsync();

            return true;
        }
    }
}