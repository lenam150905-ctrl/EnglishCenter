namespace EnglishCenter.API.Models
{
    public class Invoice
    {
        public int Id { get; set; }

        public int StudentId { get; set; }

        public int? EnrollmentId { get; set; }

        public decimal Amount { get; set; }

        public DateTime InvoiceDate { get; set; }

        public string Status { get; set; } = "Unpaid";

        public Student? Student { get; set; }

        public Enrollment? Enrollment { get; set; }
    }
}