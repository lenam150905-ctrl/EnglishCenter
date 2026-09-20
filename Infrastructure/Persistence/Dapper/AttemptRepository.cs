using Dapper;
using EnglishCenter.Application.Abstractions.Persistence;

namespace EnglishCenter.Infrastructure.Persistence.Dapper;

public sealed class AttemptRepository(
    IDbConnectionFactory connectionFactory)
    : DapperRepository(connectionFactory), IAttemptRepository
{
    public async Task<bool> HasCompletedAttemptAsync(
        int examId,
        int studentId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT COUNT(1)
            FROM ExamAttempts
            WHERE ExamId = @ExamId
              AND StudentId = @StudentId
              AND Status = 'Completed';
            """;

        var count = await ExecuteScalarAsync<int>(
            sql,
            new
            {
                ExamId = examId,
                StudentId = studentId
            },
            cancellationToken);

        return count > 0;
    }
}