using EnglishCenter.API.DTOs;

namespace EnglishCenter.API.Services
{
    public interface IScheduleService
    {
        Task<List<ScheduleDto>> GetAllAsync();
        Task<ScheduleDto?> GetByIdAsync(int id);
        Task<ScheduleDto> CreateAsync(ScheduleCreateDto dto);
        Task<bool> UpdateAsync(int id, ScheduleUpdateDto dto);
        Task<bool> DeleteAsync(int id);
    }
}
