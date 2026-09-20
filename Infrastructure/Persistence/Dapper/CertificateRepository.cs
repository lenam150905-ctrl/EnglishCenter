using System.Text;
using Dapper;
using EnglishCenter.API.Models;
using EnglishCenter.Application.Abstractions.Persistence;

namespace EnglishCenter.Infrastructure.Persistence.Dapper;

public sealed class CertificateRepository(IDbConnectionFactory connectionFactory)
    : DapperRepository(connectionFactory), ICertificateRepository
{
    public async Task<Certificate?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT c.Id, c.StudentId, c.CourseId, c.CertificateCode, c.IssueDate, c.PdfFilePath, c.IsDeleted,
                   s.Id, s.FullName, s.DateOfBirth, s.Email, s.Phone, s.Address, s.UserId, s.IsDeleted,
                   co.Id, co.CourseCode, co.CourseName, co.Description, co.Duration, co.TuitionFee, co.Status, co.IsDeleted
            FROM Certificates c
            LEFT JOIN Students s ON c.StudentId = s.Id
            LEFT JOIN Courses co ON c.CourseId = co.Id
            WHERE c.Id = @Id AND c.IsDeleted = 0;
            """;

        var list = await QueryAsync<Certificate, Student, Course, Certificate>(
            sql,
            (certificate, student, course) =>
            {
                certificate.Student = student;
                certificate.Course = course;
                return certificate;
            },
            new { Id = id },
            splitOn: "Id,Id",
            cancellationToken: cancellationToken);

        return list.FirstOrDefault();
    }

    public async Task<bool> ExistsAsync(int studentId, int courseId, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        var sql = excludeId.HasValue
            ? "SELECT COUNT(1) FROM Certificates WHERE StudentId = @StudentId AND CourseId = @CourseId AND Id <> @ExcludeId AND IsDeleted = 0;"
            : "SELECT COUNT(1) FROM Certificates WHERE StudentId = @StudentId AND CourseId = @CourseId AND IsDeleted = 0;";

        var count = await ExecuteScalarAsync<int>(sql, new { StudentId = studentId, CourseId = courseId, ExcludeId = excludeId }, cancellationToken);
        return count > 0;
    }

    public async Task<bool> ExistsByCodeAsync(string code, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        var sql = excludeId.HasValue
            ? "SELECT COUNT(1) FROM Certificates WHERE CertificateCode = @Code AND Id <> @ExcludeId AND IsDeleted = 0;"
            : "SELECT COUNT(1) FROM Certificates WHERE CertificateCode = @Code AND IsDeleted = 0;";

        var count = await ExecuteScalarAsync<int>(sql, new { Code = code, ExcludeId = excludeId }, cancellationToken);
        return count > 0;
    }

    public async Task<(IReadOnlyList<Certificate> Certificates, int TotalCount)> GetAllAsync(
        string? search,
        int? studentId,
        int? courseId,
        string? sortBy,
        bool sortDesc,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var whereClause = new StringBuilder("WHERE c.IsDeleted = 0");
        var parameters = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(search))
        {
            whereClause.Append(" AND (c.CertificateCode LIKE @Search OR s.FullName LIKE @Search OR co.CourseName LIKE @Search)");
            parameters.Add("Search", $"%{search}%");
        }

        if (studentId.HasValue)
        {
            whereClause.Append(" AND c.StudentId = @StudentId");
            parameters.Add("StudentId", studentId.Value);
        }

        if (courseId.HasValue)
        {
            whereClause.Append(" AND c.CourseId = @CourseId");
            parameters.Add("CourseId", courseId.Value);
        }

        var orderColumn = (sortBy?.ToLower()) switch
        {
            "certificatecode" => "c.CertificateCode",
            "issuedate" => "c.IssueDate",
            _ => "c.Id"
        };
        var orderDirection = sortDesc ? "DESC" : "ASC";

        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 20 : pageSize;
        var offset = (page - 1) * pageSize;

        parameters.Add("Offset", offset);
        parameters.Add("PageSize", pageSize);

        var countSql = $"""
            SELECT COUNT(1)
            FROM Certificates c
            LEFT JOIN Students s ON c.StudentId = s.Id
            LEFT JOIN Courses co ON c.CourseId = co.Id
            {whereClause};
            """;

        var querySql = $"""
            SELECT c.Id, c.StudentId, c.CourseId, c.CertificateCode, c.IssueDate, c.PdfFilePath, c.IsDeleted,
                   s.Id, s.FullName, s.DateOfBirth, s.Email, s.Phone, s.Address, s.UserId, s.IsDeleted,
                   co.Id, co.CourseCode, co.CourseName, co.Description, co.Duration, co.TuitionFee, co.Status, co.IsDeleted
            FROM Certificates c
            LEFT JOIN Students s ON c.StudentId = s.Id
            LEFT JOIN Courses co ON c.CourseId = co.Id
            {whereClause}
            ORDER BY {orderColumn} {orderDirection}
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
            """;

        using var connection = CreateConnection();
        var totalCount = await connection.ExecuteScalarAsync<int>(new CommandDefinition(countSql, parameters, cancellationToken: cancellationToken));
        var certificates = (await connection.QueryAsync<Certificate, Student, Course, Certificate>(
            new CommandDefinition(querySql, parameters, cancellationToken: cancellationToken),
            (certificate, student, course) =>
            {
                certificate.Student = student;
                certificate.Course = course;
                return certificate;
            },
            splitOn: "Id,Id")).AsList();

        return (certificates, totalCount);
    }

    public async Task CreateAsync(Certificate certificate, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO Certificates (StudentId, CourseId, CertificateCode, IssueDate, PdfFilePath, IsDeleted)
            OUTPUT INSERTED.Id
            VALUES (@StudentId, @CourseId, @CertificateCode, @IssueDate, @PdfFilePath, @IsDeleted);
            """;
        certificate.Id = await ExecuteScalarAsync<int>(sql, certificate, cancellationToken);
    }

    public async Task<bool> UpdateAsync(Certificate certificate, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE Certificates
            SET StudentId = @StudentId,
                CourseId = @CourseId,
                CertificateCode = @CertificateCode,
                IssueDate = @IssueDate,
                PdfFilePath = @PdfFilePath
            WHERE Id = @Id AND IsDeleted = 0;
            """;
        return await ExecuteAsync(sql, certificate, cancellationToken) > 0;
    }

    public async Task<bool> UpdatePdfPathAsync(int id, string pdfPath, CancellationToken cancellationToken = default)
    {
        const string sql = "UPDATE Certificates SET PdfFilePath = @PdfFilePath WHERE Id = @Id AND IsDeleted = 0;";
        return await ExecuteAsync(sql, new { Id = id, PdfFilePath = pdfPath }, cancellationToken) > 0;
    }

    public async Task<bool> SoftDeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = "UPDATE Certificates SET IsDeleted = 1 WHERE Id = @Id AND IsDeleted = 0;";
        return await ExecuteAsync(sql, new { Id = id }, cancellationToken) > 0;
    }
}
