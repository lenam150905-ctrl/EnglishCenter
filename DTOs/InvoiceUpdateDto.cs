using System.ComponentModel.DataAnnotations;

namespace EnglishCenter.API.DTOs
{
    public class InvoiceUpdateDto
    {
        [Required]
        public int StudentId { get; set; }

        public int? EnrollmentId { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Amount { get; set; }

        [Required]
        public DateTime InvoiceDate { get; set; }

        public string Status { get; set; } = "Unpaid";
    }
}