using EnglishCenter.API.Data;
using EnglishCenter.API.DTOs;
using Microsoft.EntityFrameworkCore;

namespace EnglishCenter.API.Services
{
    public class ReportService : IReportService
    {
        private readonly ApplicationDbContext _context;

        public ReportService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ReportDashboardDto> GetDashboardAsync()
        {
            var totalStudents =
                await _context.Students.CountAsync();

            var totalCourses =
                await _context.Courses.CountAsync();

            var totalEnrollments =
                await _context.Enrollments.CountAsync();

            var totalExams =
                await _context.Exams.CountAsync();

            var totalGrades =
                await _context.Grades.CountAsync();

            var totalCertificates =
                await _context.Certificates.CountAsync();

            return new ReportDashboardDto
            {
                TotalStudents = totalStudents,
                TotalCourses = totalCourses,
                TotalEnrollments = totalEnrollments,
                TotalExams = totalExams,
                TotalGrades = totalGrades,
                TotalCertificates = totalCertificates
            };
        }

        public async Task<List<CourseReportDto>> GetCourseReportAsync()
        {
            var courses = await _context.Courses
                .Select(c => new CourseReportDto
                {
                    CourseId = c.Id,

                    CourseName = c.CourseName,

                    TotalStudents = _context.Enrollments
                        .Count(e => e.CourseId == c.Id),

                    TotalExams = _context.Exams
                        .Count(e => e.CourseId == c.Id),

                    TotalGrades = _context.Grades
                        .Count(g =>
                            g.Exam.CourseId == c.Id),

                    TotalCertificates = _context.Certificates
                        .Count(x => x.CourseId == c.Id),

                    AverageScore = _context.Grades
                        .Where(g =>
                            g.Exam.CourseId == c.Id)
                        .Select(g => (decimal?)g.Score)
                        .Average() ?? 0
                })
                .ToListAsync();

            return courses;
        }
    }
}