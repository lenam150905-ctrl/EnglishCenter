using EnglishCenter.API.Models;

namespace EnglishCenter.Application.Abstractions.Persistence;

public interface IScheduleRepository
{
    Task<Schedule?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Schedule> Schedules, int TotalCount)> GetAllAsync(
        string? search,
        int? courseId,
        int? teacherId,
        string? sortBy,
        bool sortDesc,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<bool> HasRoomConflictAsync(
        string room,
        DateTime startTime,
        DateTime endTime,
        int? excludeId = null,
        CancellationToken cancellationToken = default);
    Task<bool> HasTeacherConflictAsync(
        int teacherId,
        DateTime startTime,
        DateTime endTime,
        int? excludeId = null,
        CancellationToken cancellationToken = default);
    Task CreateAsync(Schedule schedule, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(Schedule schedule, CancellationToken cancellationToken = default);
    Task<bool> SoftDeleteAsync(int id, CancellationToken cancellationToken = default);
}
