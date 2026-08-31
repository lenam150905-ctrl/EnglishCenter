namespace EnglishCenter.API.Models
{
    public class Certificate
    {
        public int Id { get; set; }

        public int StudentId { get; set; }

        public int CourseId { get; set; }

        public string CertificateCode { get; set; } = string.Empty;

        public DateTime IssueDate { get; set; }

        public string PdfFilePath { get; set; } = string.Empty;

        public Student? Student { get; set; }

        public Course? Course { get; set; }
    }
}