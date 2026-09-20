using EnglishCenter.API.DTOs;
using EnglishCenter.Application.Abstractions.Persistence;

namespace EnglishCenter.Infrastructure.Persistence.Dapper;

public sealed class ReportRepository(IDbConnectionFactory connectionFactory)
    : DapperRepository(connectionFactory), IReportRepository
{
    public async Task<ReportDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                (SELECT COUNT(1) FROM Students WHERE IsDeleted = 0) AS TotalStudents,
                (SELECT COUNT(1) FROM Courses WHERE IsDeleted = 0) AS TotalCourses,
                (SELECT COUNT(1) FROM Enrollments WHERE IsDeleted = 0) AS TotalEnrollments,
                (SELECT COUNT(1) FROM Exams WHERE IsDeleted = 0) AS TotalExams,
                (SELECT COUNT(1) FROM Grades WHERE IsDeleted = 0) AS TotalGrades,
                (SELECT COUNT(1) FROM Certificates WHERE IsDeleted = 0) AS TotalCertificates;
            """;

        var result = await QueryFirstOrDefaultAsync<ReportDashboardDto>(sql, cancellationToken: cancellationToken);
        return result ?? new ReportDashboardDto();
    }

    public async Task<List<CourseReportDto>> GetCourseReportAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                c.Id AS CourseId,
                c.CourseName,
                (SELECT COUNT(1) FROM Enrollments e WHERE e.CourseId = c.Id AND e.IsDeleted = 0) AS TotalStudents,
                (SELECT COUNT(1) FROM Exams ex WHERE ex.CourseId = c.Id AND ex.IsDeleted = 0) AS TotalExams,
                (SELECT COUNT(1) FROM Grades g INNER JOIN Exams ex ON g.ExamId = ex.Id WHERE ex.CourseId = c.Id AND g.IsDeleted = 0 AND ex.IsDeleted = 0) AS TotalGrades,
                (SELECT COUNT(1) FROM Certificates cert WHERE cert.CourseId = c.Id AND cert.IsDeleted = 0) AS TotalCertificates,
                ISNULL((SELECT AVG(CAST(g.Score AS DECIMAL(18,2))) FROM Grades g INNER JOIN Exams ex ON g.ExamId = ex.Id WHERE ex.CourseId = c.Id AND g.IsDeleted = 0 AND ex.IsDeleted = 0), 0) AS AverageScore
            FROM Courses c
            WHERE c.IsDeleted = 0;
            """;

        var list = await QueryAsync<CourseReportDto>(sql, cancellationToken: cancellationToken);
        return list.ToList();
    }
}
