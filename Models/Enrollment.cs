namespace EnglishCenter.API.Models
{
    public class Enrollment
    {
        public int Id { get; set; }

        public int StudentId { get; set; }

        public int CourseId { get; set; }

        public DateTime EnrollmentDate { get; set; }

        public string Status { get; set; } = "Active";

        public Student? Student { get; set; }

        public Course? Course { get; set; }

        public ICollection<Invoice> Invoices { get; set; }
            = new List<Invoice>();
    }
}
