using System.Text;
using Dapper;
using EnglishCenter.API.Models;
using EnglishCenter.Application.Abstractions.Persistence;

namespace EnglishCenter.Infrastructure.Persistence.Dapper;

public sealed class InvoiceRepository(IDbConnectionFactory connectionFactory)
    : DapperRepository(connectionFactory), IInvoiceRepository
{
    public async Task<Invoice?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT i.Id, i.StudentId, i.EnrollmentId, i.Amount, i.InvoiceDate, i.Status, i.IsDeleted,
                   s.Id, s.FullName, s.DateOfBirth, s.Email, s.Phone, s.Address, s.UserId, s.IsDeleted,
                   e.Id, e.StudentId, e.CourseId, e.EnrollmentDate, e.Status, e.IsDeleted
            FROM Invoices i
            LEFT JOIN Students s ON i.StudentId = s.Id
            LEFT JOIN Enrollments e ON i.EnrollmentId = e.Id
            WHERE i.Id = @Id AND i.IsDeleted = 0;
            """;

        var list = await QueryAsync<Invoice, Student, Enrollment, Invoice>(
            sql,
            (invoice, student, enrollment) =>
            {
                invoice.Student = student;
                invoice.Enrollment = enrollment;
                return invoice;
            },
            new { Id = id },
            splitOn: "Id,Id",
            cancellationToken: cancellationToken);

        return list.FirstOrDefault();
    }

    public async Task<bool> ExistsByEnrollmentAsync(int enrollmentId, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        var sql = excludeId.HasValue
            ? "SELECT COUNT(1) FROM Invoices WHERE EnrollmentId = @EnrollmentId AND Status <> 'Cancelled' AND Id <> @ExcludeId AND IsDeleted = 0;"
            : "SELECT COUNT(1) FROM Invoices WHERE EnrollmentId = @EnrollmentId AND Status <> 'Cancelled' AND IsDeleted = 0;";

        var count = await ExecuteScalarAsync<int>(sql, new { EnrollmentId = enrollmentId, ExcludeId = excludeId }, cancellationToken);
        return count > 0;
    }

    public async Task<bool> HasPaidInvoiceForEnrollmentAsync(int enrollmentId, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT COUNT(1) FROM Invoices WHERE EnrollmentId = @EnrollmentId AND Status = 'Paid' AND IsDeleted = 0;";
        var count = await ExecuteScalarAsync<int>(sql, new { EnrollmentId = enrollmentId }, cancellationToken);
        return count > 0;
    }

    public async Task<bool> HasPaidInvoiceForStudentAndCourseAsync(int studentId, int courseId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT COUNT(1)
            FROM Invoices i
            INNER JOIN Enrollments e ON i.EnrollmentId = e.Id
            WHERE i.StudentId = @StudentId
              AND e.CourseId = @CourseId
              AND i.Status = 'Paid'
              AND i.IsDeleted = 0
              AND e.IsDeleted = 0;
            """;
        return await ExecuteScalarAsync<int>(sql, new { StudentId = studentId, CourseId = courseId }, cancellationToken) > 0;
    }

    public async Task<(IReadOnlyList<Invoice> Invoices, int TotalCount)> GetAllAsync(
        string? search,
        int? studentId,
        int? enrollmentId,
        string? status,
        decimal? minAmount,
        decimal? maxAmount,
        string? sortBy,
        bool sortDesc,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var whereClause = new StringBuilder("WHERE i.IsDeleted = 0");
        var parameters = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(search))
        {
            whereClause.Append(" AND (s.FullName LIKE @Search OR s.Email LIKE @Search OR i.Status LIKE @Search OR c.CourseName LIKE @Search)");
            parameters.Add("Search", $"%{search}%");
        }

        if (studentId.HasValue)
        {
            whereClause.Append(" AND i.StudentId = @StudentId");
            parameters.Add("StudentId", studentId.Value);
        }

        if (enrollmentId.HasValue)
        {
            whereClause.Append(" AND i.EnrollmentId = @EnrollmentId");
            parameters.Add("EnrollmentId", enrollmentId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            whereClause.Append(" AND i.Status = @Status");
            parameters.Add("Status", status);
        }

        if (minAmount.HasValue)
        {
            whereClause.Append(" AND i.Amount >= @MinAmount");
            parameters.Add("MinAmount", minAmount.Value);
        }

        if (maxAmount.HasValue)
        {
            whereClause.Append(" AND i.Amount <= @MaxAmount");
            parameters.Add("MaxAmount", maxAmount.Value);
        }

        var orderColumn = (sortBy?.ToLower()) switch
        {
            "amount" => "i.Amount",
            "invoicedate" => "i.InvoiceDate",
            "status" => "i.Status",
            _ => "i.Id"
        };
        var orderDirection = sortDesc ? "DESC" : "ASC";

        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 20 : pageSize;
        var offset = (page - 1) * pageSize;

        parameters.Add("Offset", offset);
        parameters.Add("PageSize", pageSize);

        var countSql = $"""
            SELECT COUNT(1)
            FROM Invoices i
            LEFT JOIN Students s ON i.StudentId = s.Id
            LEFT JOIN Enrollments e ON i.EnrollmentId = e.Id
            LEFT JOIN Courses c ON e.CourseId = c.Id
            {whereClause};
            """;

        var querySql = $"""
            SELECT i.Id, i.StudentId, i.EnrollmentId, i.Amount, i.InvoiceDate, i.Status, i.IsDeleted,
                   s.Id, s.FullName, s.DateOfBirth, s.Email, s.Phone, s.Address, s.UserId, s.IsDeleted,
                   e.Id, e.StudentId, e.CourseId, e.EnrollmentDate, e.Status, e.IsDeleted,
                   c.Id, c.CourseCode, c.CourseName, c.Description, c.Duration, c.TuitionFee, c.Status, c.IsDeleted
            FROM Invoices i
            LEFT JOIN Students s ON i.StudentId = s.Id
            LEFT JOIN Enrollments e ON i.EnrollmentId = e.Id
            LEFT JOIN Courses c ON e.CourseId = c.Id
            {whereClause}
            ORDER BY {orderColumn} {orderDirection}
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
            """;

        using var connection = CreateConnection();
        var totalCount = await connection.ExecuteScalarAsync<int>(new CommandDefinition(countSql, parameters, cancellationToken: cancellationToken));
        var invoices = (await connection.QueryAsync<Invoice, Student, Enrollment, Course, Invoice>(
            new CommandDefinition(querySql, parameters, cancellationToken: cancellationToken),
            (invoice, student, enrollment, course) =>
            {
                invoice.Student = student;
                if (enrollment != null)
                {
                    enrollment.Course = course;
                    invoice.Enrollment = enrollment;
                }
                return invoice;
            },
            splitOn: "Id,Id,Id")).AsList();

        return (invoices, totalCount);
    }

    public async Task CreateAsync(Invoice invoice, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO Invoices (StudentId, EnrollmentId, Amount, InvoiceDate, Status, IsDeleted)
            OUTPUT INSERTED.Id
            VALUES (@StudentId, @EnrollmentId, @Amount, @InvoiceDate, @Status, @IsDeleted);
            """;
        invoice.Id = await ExecuteScalarAsync<int>(sql, invoice, cancellationToken);
    }

    public async Task<bool> UpdateAsync(Invoice invoice, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE Invoices
            SET StudentId = @StudentId,
                EnrollmentId = @EnrollmentId,
                Amount = @Amount,
                InvoiceDate = @InvoiceDate,
                Status = @Status
            WHERE Id = @Id AND IsDeleted = 0;
            """;
        return await ExecuteAsync(sql, invoice, cancellationToken) > 0;
    }

    public async Task<bool> UpdateStatusAsync(int id, string status, CancellationToken cancellationToken = default)
    {
        const string sql = "UPDATE Invoices SET Status = @Status WHERE Id = @Id AND IsDeleted = 0;";
        return await ExecuteAsync(sql, new { Id = id, Status = status }, cancellationToken) > 0;
    }

    public async Task<bool> SoftDeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = "UPDATE Invoices SET IsDeleted = 1 WHERE Id = @Id AND IsDeleted = 0;";
        return await ExecuteAsync(sql, new { Id = id }, cancellationToken) > 0;
    }
}
