using System.Diagnostics;

namespace EnglishCenter.API.Models
{
    public class Student
    {
        public int Id { get; set; }

        public string FullName { get; set; } = string.Empty;

        public DateTime DateOfBirth { get; set; }

        public string Email { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;

        public int? UserId { get; set; }

        public User? User { get; set; }

        public ICollection<Enrollment> Enrollments { get; set; }
            = new List<Enrollment>();

        public ICollection<Grade> Grades { get; set; }
            = new List<Grade>();

        public ICollection<Invoice> Invoices { get; set; }
            = new List<Invoice>();

        public ICollection<Certificate> Certificates { get; set; }
            = new List<Certificate>();
    }
}