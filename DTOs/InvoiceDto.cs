namespace EnglishCenter.API.DTOs
{
    public class InvoiceDto
    {
        public int Id { get; set; }

        public int StudentId { get; set; }

        public string StudentName { get; set; } = string.Empty;

        public int? EnrollmentId { get; set; }

        public string? CourseName { get; set; }

        public decimal Amount { get; set; }

        public DateTime InvoiceDate { get; set; }

        public string Status { get; set; } = string.Empty;
    }
}