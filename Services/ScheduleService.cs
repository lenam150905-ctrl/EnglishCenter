using EnglishCenter.API.Data;
using EnglishCenter.API.DTOs;
using Microsoft.EntityFrameworkCore;

namespace EnglishCenter.API.Services
{
    public class ScheduleService : IScheduleService
    {
        private readonly ApplicationDbContext _context;

        public ScheduleService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<ScheduleDto>> GetAllAsync()
        {
            var schedules = await _context.Schedules
                .Include(s => s.Course)
                .Include(s => s.Teacher)
                .ToListAsync();

            return schedules.Select(s => new ScheduleDto
            {
                Id = s.Id,
                CourseId = s.CourseId,
                CourseName = s.Course.CourseName,
                TeacherId = s.TeacherId,
                TeacherName = s.Teacher.FullName,
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                Room = s.Room
            }).ToList();
        }
        public async Task<ScheduleDto?> GetByIdAsync(int id)
        {
            var schedule = await _context.Schedules
                .Include(s => s.Course)
                .Include(s => s.Teacher)
                .FirstOrDefaultAsync(s => s.Id == id);
            if (schedule == null)
            {
                return null;
            }
            return new ScheduleDto
            {
                Id = schedule.Id,
                CourseId = schedule.CourseId,
                CourseName = schedule.Course.CourseName,
                TeacherId = schedule.TeacherId,
                TeacherName = schedule.Teacher.FullName,
                StartTime = schedule.StartTime,
                EndTime = schedule.EndTime,
                Room = schedule.Room
            };
        }
        public async Task<ScheduleDto> CreateAsync(ScheduleCreateDto dto)
        {
            var schedule = new  Models.Schedule
            {
                CourseId = dto.CourseId,
                TeacherId = dto.TeacherId,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                Room = dto.Room
            };
            if (schedule.StartTime<DateTime.Now)
            {
                throw new ArgumentException("Ngày giờ không được nhỏ hơn ngày hiện tại");
            }

            if (schedule.EndTime<schedule.StartTime)
            {
                throw new ArgumentException("Ngày kết thúc không được bé hơn ngày bắt đầu");
            }
            _context.Schedules.Add(schedule);
            await _context.SaveChangesAsync();
            await _context.Entry(schedule)
    .Reference(s => s.Course)
    .LoadAsync();

            await _context.Entry(schedule)
                .Reference(s => s.Teacher)
                .LoadAsync();
            return new ScheduleDto
            {
                Id = schedule.Id,
                CourseId = schedule.CourseId,
                TeacherId = schedule.TeacherId,
                StartTime = schedule.StartTime,
                CourseName = schedule.Course.CourseName,
                TeacherName = schedule.Teacher.FullName,
                EndTime = schedule.EndTime,
                Room = schedule.Room
            };
        }
    public async Task<bool> UpdateAsync(int id, ScheduleUpdateDto dto)
        {
            var schedule = await _context.Schedules.FindAsync(id);
            if (schedule == null)
            {
                return false;
            }
            if (dto.StartTime < DateTime.Now)
            {
                throw new ArgumentException(
                    "Ngày giờ không được nhỏ hơn ngày hiện tại");
            }

            if (dto.EndTime <= dto.StartTime)
            {
                throw new ArgumentException(
                    "Ngày kết thúc phải lớn hơn ngày bắt đầu");
            }
            schedule.CourseId = dto.CourseId;
            schedule.TeacherId = dto.TeacherId;
            schedule.StartTime = dto.StartTime;
            schedule.EndTime = dto.EndTime;
            schedule.Room = dto.Room;
            await _context.SaveChangesAsync();
            return true;
        }
        public async Task<bool> DeleteAsync(int id)
        {
            var schedule = await _context.Schedules.FindAsync(id);
            if (schedule == null)
            {
                return false;
            }
            _context.Schedules.Remove(schedule);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}