using EnglishCenter.API.Models;

namespace EnglishCenter.Application.Abstractions.Persistence;

public interface IAttemptRepository
{
    Task<bool> HasCompletedAttemptAsync(
        int examId,
        int studentId,
        CancellationToken cancellationToken = default);
}