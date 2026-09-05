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

        public async Task<PagedResultDto<InvoiceDto>> GetAllAsync(
     string? search,
     int? studentId,
     int? enrollmentId,
     string? status,
     decimal? minAmount,
     decimal? maxAmount,
     string? sortBy,
     bool sortDesc,
     int page,
     int pageSize)
        {
            var query = _context.Invoices
     .Include(i => i.Student)
     .Include(i => i.Enrollment)
         .ThenInclude(e => e.Course)
     .AsQueryable();

            // SEARCH
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(i =>
                    i.Student.FullName.Contains(search) ||
                    i.Student.Email.Contains(search) ||
                    i.Status.Contains(search) ||
                   ( i.Enrollment != null &&
                     i.Enrollment.Course.CourseName.Contains(search)));
            }

            // FILTER
            if (studentId.HasValue)
            {
                query = query.Where(i =>
                    i.StudentId == studentId.Value);
            }

            if (enrollmentId.HasValue)
            {
                query = query.Where(i =>
                    i.EnrollmentId == enrollmentId.Value);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(i =>
                    i.Status == status);
            }

            if (minAmount.HasValue)
            {
                query = query.Where(i =>
                    i.Amount >= minAmount.Value);
            }

            if (maxAmount.HasValue)
            {
                query = query.Where(i =>
                    i.Amount <= maxAmount.Value);
            }

            // SORT
            if (!string.IsNullOrWhiteSpace(sortBy))
            {
                switch (sortBy.ToLower())
                {
                    case "id":
                        query = sortDesc
                            ? query.OrderByDescending(i => i.Id)
                            : query.OrderBy(i => i.Id);
                        break;

                    case "amount":
                        query = sortDesc
                            ? query.OrderByDescending(i => i.Amount)
                            : query.OrderBy(i => i.Amount);
                        break;

                    case "invoicedate":
                        query = sortDesc
                            ? query.OrderByDescending(i => i.InvoiceDate)
                            : query.OrderBy(i => i.InvoiceDate);
                        break;

                    case "status":
                        query = sortDesc
                            ? query.OrderByDescending(i => i.Status)
                            : query.OrderBy(i => i.Status);
                        break;
                }
            }
            else
            {
                query = query.OrderBy(i => i.Id);
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

            var invoices = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // DTO
            var data = invoices.Select(i => new InvoiceDto
            {
                Id = i.Id,
                StudentId = i.StudentId,
                StudentName = i.Student.FullName,
                EnrollmentId = i.EnrollmentId,
                Amount = i.Amount,
                CourseName = i.Enrollment?.Course?.CourseName,
                InvoiceDate = i.InvoiceDate,
                Status = i.Status
            }).ToList();

            return new PagedResultDto<InvoiceDto>
            {
                Data = data,
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages
            };
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
            // STUDENT
            var studentExists = await _context.Students
                .AnyAsync(s => s.Id == dto.StudentId);

            if (!studentExists)
            {
                throw new ArgumentException(
                    "Student không tồn tại.");
            }

            // ENROLLMENT
            if (dto.EnrollmentId.HasValue)
            {
                var enrollment = await _context.Enrollments
                    .FirstOrDefaultAsync(e =>
                        e.Id == dto.EnrollmentId.Value);

                if (enrollment == null)
                {
                    throw new ArgumentException(
                        "Enrollment không tồn tại.");
                }

                // KIỂM TRA ENROLLMENT THUỘC STUDENT
                if (enrollment.StudentId != dto.StudentId)
                {
                    throw new ArgumentException(
                        "Enrollment không thuộc Student này.");
                }
            }

            // AMOUNT
            if (dto.Amount <= 0)
            {
                throw new ArgumentException(
                    "Số tiền phải lớn hơn 0.");
            }

            // INVOICE DATE
            if (dto.InvoiceDate > DateTime.Now)
            {
                throw new ArgumentException(
                    "Ngày lập hóa đơn không được lớn hơn ngày hiện tại.");
            }

            // STATUS
            if (dto.Status != "Unpaid" &&
                dto.Status != "Paid" &&
                dto.Status != "Cancelled")
            {
                throw new ArgumentException(
                    "Status không hợp lệ.");
            }

            // CHECK INVOICE TRÙNG ENROLLMENT
            if (dto.EnrollmentId.HasValue)
            {
                var existed = await _context.Invoices
                    .AnyAsync(i =>
                        i.EnrollmentId == dto.EnrollmentId.Value &&
                        i.Status != "Cancelled");

                if (existed)
                {
                    throw new ArgumentException(
                        "Enrollment này đã có hóa đơn.");
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
                EnrollmentId = invoice.EnrollmentId,
                Amount = invoice.Amount,
                InvoiceDate = invoice.InvoiceDate,
                Status = invoice.Status
            };
        }

        public async Task<bool> UpdateAsync(
      int id,
      InvoiceUpdateDto dto)
        {
            // KIỂM TRA INVOICE
            var invoice = await _context.Invoices
                .FindAsync(id);

            if (invoice == null)
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

            // ENROLLMENT
            if (dto.EnrollmentId.HasValue)
            {
                var enrollment = await _context.Enrollments
                    .FirstOrDefaultAsync(e =>
                        e.Id == dto.EnrollmentId.Value);

                if (enrollment == null)
                {
                    throw new ArgumentException(
                        "Enrollment không tồn tại.");
                }

                // ENROLLMENT THUỘC ĐÚNG STUDENT
                if (enrollment.StudentId != dto.StudentId)
                {
                    throw new ArgumentException(
                        "Enrollment không thuộc Student này.");
                }
            }

            // AMOUNT
            if (dto.Amount <= 0)
            {
                throw new ArgumentException(
                    "Số tiền phải lớn hơn 0.");
            }

            // INVOICE DATE
            if (dto.InvoiceDate > DateTime.Now)
            {
                throw new ArgumentException(
                    "Ngày lập hóa đơn không được lớn hơn ngày hiện tại.");
            }

            // STATUS
            if (dto.Status != "Unpaid" &&
                dto.Status != "Paid" &&
                dto.Status != "Cancelled")
            {
                throw new ArgumentException(
                    "Status không hợp lệ.");
            }

            // CHECK INVOICE TRÙNG
            if (dto.EnrollmentId.HasValue)
            {
                var existed = await _context.Invoices
                    .AnyAsync(i =>
                        i.Id != id &&
                        i.EnrollmentId == dto.EnrollmentId.Value &&
                        i.Status != "Cancelled");

                if (existed)
                {
                    throw new ArgumentException(
                        "Enrollment này đã có hóa đơn.");
                }
            }

            // UPDATE
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