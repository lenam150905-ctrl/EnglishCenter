namespace EnglishCenter.API.DTOs
{
    public class CertificateDto
    {
        public int Id { get; set; }

        public int StudentId { get; set; }

        public string StudentName { get; set; } = string.Empty;

        public int CourseId { get; set; }

        public string CourseName { get; set; } = string.Empty;

        public string CertificateCode { get; set; } = string.Empty;

        public DateTime IssueDate { get; set; }

        public string PdfFilePath { get; set; } = string.Empty;
    }
}