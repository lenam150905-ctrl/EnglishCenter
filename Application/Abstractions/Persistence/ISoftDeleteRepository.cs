namespace EnglishCenter.Application.Abstractions.Persistence;

public interface ISoftDeleteRepository
{
    Task<bool> RestoreAsync(string tableName, int id, CancellationToken cancellationToken = default);
}
