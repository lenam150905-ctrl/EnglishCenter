using System.ComponentModel.DataAnnotations;

namespace EnglishCenter.API.DTOs
{
    public class CertificateUpdateDto
    {
        [Required]
        public int StudentId { get; set; }

        [Required]
        public int CourseId { get; set; }

        [Required]
        public string CertificateCode { get; set; } = string.Empty;

        [Required]
        public DateTime IssueDate { get; set; }

        public string PdfFilePath { get; set; } = string.Empty;
    }
}