using EnglishCenter.API.DTOs;

namespace EnglishCenter.Application.Abstractions.Persistence;

public interface ITrashRepository
{
    Task<IReadOnlyList<DeletedItemDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<bool> RestoreAsync(string entity, int id, CancellationToken cancellationToken = default);
}
