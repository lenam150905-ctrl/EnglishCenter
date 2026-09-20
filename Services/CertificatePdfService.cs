using EnglishCenter.API.DTOs;
using EnglishCenter.Application.Abstractions.Persistence;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace EnglishCenter.API.Services
{
    public class CertificatePdfService : ICertificatePdfService
    {
        private readonly ICertificateRepository _certificateRepository;
        private readonly IGradeRepository _gradeRepository;
        private readonly IWebHostEnvironment _environment;
        private readonly IAuditLogService _auditLogService;
        private readonly INotificationService _notificationService;

        public CertificatePdfService(
            ICertificateRepository certificateRepository,
            IGradeRepository gradeRepository,
            IWebHostEnvironment environment,
            IAuditLogService auditLogService,
            INotificationService notificationService)
        {
            _certificateRepository = certificateRepository;
            _gradeRepository = gradeRepository;
            _environment = environment;
            _auditLogService = auditLogService;
            _notificationService = notificationService;
        }

        public async Task<string> GenerateCertificatePdfAsync(
            int certificateId,
            int? userId,
            string? ipAddress)
        {
            var certificate = await _certificateRepository.GetByIdAsync(certificateId);
            if (certificate == null)
            {
                throw new ArgumentException("Certificate không tồn tại.");
            }

            var grade = await _gradeRepository.GetByStudentAndCourseAsync(certificate.StudentId, certificate.CourseId);
            if (grade == null)
            {
                throw new ArgumentException("Student chưa có điểm.");
            }

            var folder = Path.Combine(_environment.WebRootPath, "certificates");
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            var fileName = $"{certificate.CertificateCode}.pdf";
            var filePath = Path.Combine(folder, fileName);

            var pdfBytes = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(35);
                    page.DefaultTextStyle(x => x.FontFamily("Arial"));

                    page.Content()
                        .Border(5)
                        .BorderColor("#D4AF37")
                        .Padding(30)
                        .Column(column =>
                        {
                            column.Spacing(15);

                            column.Item().AlignCenter().Text("ENGLISH CENTER")
                                .FontSize(26)
                                .Bold()
                                .FontColor("#1A365D");

                            column.Item().AlignCenter().Text("CERTIFICATE OF COMPLETION")
                                .FontSize(22)
                                .Bold()
                                .FontColor("#D4AF37");

                            column.Item().AlignCenter().Text("This is to certify that")
                                .FontSize(14)
                                .Italic();

                            column.Item().AlignCenter().Text(certificate.Student?.FullName ?? string.Empty)
                                .FontSize(28)
                                .Bold()
                                .FontColor("#2B6CB0");

                            column.Item().AlignCenter().Text($"has successfully completed the course")
                                .FontSize(14);

                            column.Item().AlignCenter().Text(certificate.Course?.CourseName ?? string.Empty)
                                .FontSize(20)
                                .Bold()
                                .FontColor("#2D3748");

                            column.Item().AlignCenter().Text($"Final Score: {grade.Score:0.0} / 10")
                                .FontSize(16)
                                .Bold();

                            column.Item().PaddingTop(25).Row(row =>
                            {
                                row.RelativeItem().Column(col =>
                                {
                                    col.Item().Text($"Certificate Code: {certificate.CertificateCode}").FontSize(11);
                                    col.Item().Text($"Issue Date: {certificate.IssueDate:dd/MM/yyyy}").FontSize(11);
                                });

                                row.RelativeItem().AlignRight().Column(col =>
                                {
                                    col.Item().Text("Director of Center").FontSize(12).Bold();
                                    col.Item().PaddingTop(30).Text("English Center").FontSize(11).Italic();
                                });
                            });
                        });
                });
            }).GeneratePdf();

            await File.WriteAllBytesAsync(filePath, pdfBytes);

            certificate.PdfFilePath = $"/certificates/{fileName}";
            await _certificateRepository.UpdatePdfPathAsync(certificate.Id, certificate.PdfFilePath);

            await _auditLogService.CreateAsync(
                userId,
                "EXPORT_PDF",
                "Certificate",
                certificate.Id,
                $"Xuất PDF chứng chỉ {certificate.CertificateCode}",
                ipAddress);

            if (userId.HasValue)
            {
                await _notificationService.CreateForUserAsync(new NotificationCreateDto
                {
                    UserId = userId.Value,
                    Title = "Chứng chỉ đã được tạo",
                    Message = $"Chứng chỉ {certificate.CertificateCode} đã được tạo thành công.",
                    Type = "CERTIFICATE"
                });
            }

            var studentUserId = certificate.Student?.UserId;
            if (studentUserId.HasValue)
            {
                await _notificationService.CreateForUserAsync(new NotificationCreateDto
                {
                    UserId = studentUserId.Value,
                    Title = "Chứng chỉ được cập nhật",
                    Message = $"Chứng chỉ {certificate.CertificateCode} của bạn đã được cấp.",
                    Type = "CERTIFICATE"
                });
            }

            return certificate.PdfFilePath;
        }
    }
}