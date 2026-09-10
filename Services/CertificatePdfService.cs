using EnglishCenter.API.Data;
using EnglishCenter.API.DTOs;
using EnglishCenter.API.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace EnglishCenter.API.Services
{
    public class CertificatePdfService : ICertificatePdfService
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly IAuditLogService _auditLogService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly INotificationService _notificationService;
        public CertificatePdfService(
      ApplicationDbContext context,
      IWebHostEnvironment environment,
      IAuditLogService auditLogService,
      IHttpContextAccessor httpContextAccessor,
      INotificationService notificationService)
        {
            _context = context;
            _environment = environment;
            _auditLogService = auditLogService;
            _httpContextAccessor = httpContextAccessor;
            _notificationService = notificationService;
        }
        private int? userid =>
AuditContext.GetUserId(
 _httpContextAccessor.HttpContext!);

        private string? ipaddress =>
            AuditContext.GetIPAddress(
                _httpContextAccessor.HttpContext!);
        public async Task<string> GenerateCertificatePdfAsync(
            int certificateId)
        {
            var certificate = await _context.Certificates
                .Include(c => c.Student)
                .Include(c => c.Course)
                .FirstOrDefaultAsync(c => c.Id == certificateId);

            if (certificate == null)
            {
                throw new ArgumentException(
                    "Certificate không tồn tại.");
            }

            // LẤY GRADE
            var grade = await _context.Grades
                .Include(g => g.Exam)
                .FirstOrDefaultAsync(g =>
                    g.StudentId == certificate.StudentId &&
                    g.Exam.CourseId == certificate.CourseId);

            if (grade == null)
            {
                throw new ArgumentException(
                    "Student chưa có điểm.");
            }

            // THƯ MỤC PDF
            var folder = Path.Combine(
                _environment.WebRootPath,
                "certificates");

            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            var fileName =
                $"{certificate.CertificateCode}.pdf";

            var filePath = Path.Combine(
                folder,
                fileName);

            // TẠO PDF
            // TẠO PDF
            var pdfBytes = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());

                    page.Margin(35);

                    page.DefaultTextStyle(
                        x => x.FontFamily("Arial"));

                    page.Content()
                        .Border(5)
                        .BorderColor("#D4AF37")
                        .Padding(30)
                        .Column(column =>
                        {
                            column.Spacing(15);

                            column.Item()
                                .AlignCenter()
                                .Text("ENGLISH CENTER")
                                .FontSize(24)
                                .Bold();

                            column.Item()
                                .AlignCenter()
                                .Text("CERTIFICATE")
                                .FontSize(36)
                                .Bold();

                            column.Item()
                                .AlignCenter()
                                .Text("OF ACHIEVEMENT")
                                .FontSize(18);

                            column.Item()
                                .PaddingTop(15)
                                .AlignCenter()
                                .Text("This certificate is proudly presented to")
                                .FontSize(14);

                            column.Item()
                                .AlignCenter()
                                .Text(certificate.Student?.FullName ?? "")
                                .FontSize(28)
                                .Bold();

                            column.Item()
                                .AlignCenter()
                                .Text(
                                    $"for successfully completing {certificate.Course?.CourseName}")
                                .FontSize(16);

                            column.Item()
                                .AlignCenter()
                                .Text($"Score: {grade.Score:0.0} / 10")
                                .FontSize(18)
                                .Bold();

                            column.Item()
                                .PaddingTop(15)
                                .AlignCenter()
                                .Text(
                                    $"Certificate Code: {certificate.CertificateCode}")
                                .FontSize(12);

                            column.Item()
                                .PaddingTop(15)
                                .Row(row =>
                                {
                                    row.RelativeItem()
                                        .AlignLeft()
                                        .Text(
                                            $"Issue Date: {certificate.IssueDate:dd/MM/yyyy}")
                                        .FontSize(12);

                                    row.RelativeItem()
                                        .AlignRight()
                                        .Text("Signature")
                                        .FontSize(12);
                                });
                        });
                });
            }).GeneratePdf();

            // GHI FILE PDF
            await File.WriteAllBytesAsync(filePath, pdfBytes);

            // LƯU ĐƯỜNG DẪN VÀO DATABASE
            certificate.PdfFilePath =
                $"/certificates/{fileName}";

            await _context.SaveChangesAsync();
            // AUDIT LOG
            await _auditLogService.CreateAsync(
                userid,
                "EXPORT_PDF",
                "Certificate",
                certificate.Id,
                $"Xuất PDF chứng chỉ {certificate.CertificateCode}",
                ipaddress);

            await _notificationService.CreateAsync(
    new NotificationCreateDto
    {
        UserId = userid.Value,
        Title = "Chứng chỉ đã được cấp",
        Message = $"Chứng chỉ {certificate.CertificateCode} của bạn đã được tạo thành công.",
        Type = "CERTIFICATE"
    });
            var student = await _context.Students
   .FirstOrDefaultAsync(s => s.Id == certificate.StudentId);
            if (student != null)
            {
                await _notificationService.CreateAsync(
                    new NotificationCreateDto
                    {
                        UserId = student.UserId.Value,
                        Title = "Chứng chỉ được cập nhật",
                        Message = $"Chứng chỉ {certificate.CertificateCode} của bạn đã được cấp.",
                        Type = "CERTIFICATE"
                    });
            }
            return certificate.PdfFilePath;
        }
    }
}