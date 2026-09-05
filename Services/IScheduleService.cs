using EnglishCenter.API.DTOs;

namespace EnglishCenter.API.Services
{
    public interface IScheduleService
    {
        Task<PagedResultDto<ScheduleDto>> GetAllAsync(
            string? search,
            int? courseId,
            int? teacherId,
            string? sortBy,
            bool sortDesc,
            int page,
            int pageSize);
        Task<ScheduleDto?> GetByIdAsync(int id);
        Task<ScheduleDto> CreateAsync(ScheduleCreateDto dto);
        Task<bool> UpdateAsync(int id, ScheduleUpdateDto dto);
        Task<bool> DeleteAsync(int id);
    }
}
