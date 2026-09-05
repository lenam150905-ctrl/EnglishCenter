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

        public async Task<PagedResultDto<ScheduleDto>> GetAllAsync(
     string? search,
     int? courseId,
     int? teacherId,
     string? sortBy,
     bool sortDesc,
     int page,
     int pageSize)
        {
            var query = _context.Schedules
                .Include(s => s.Course)
                .Include(s => s.Teacher)
                .AsQueryable();

            // SEARCH
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(s =>
                    s.Room.Contains(search) ||
                    s.Course.CourseName.Contains(search) ||
                    s.Teacher.FullName.Contains(search));
            }

            // FILTER
            if (courseId.HasValue)
            {
                query = query.Where(s =>
                    s.CourseId == courseId.Value);
            }

            if (teacherId.HasValue)
            {
                query = query.Where(s =>
                    s.TeacherId == teacherId.Value);
            }

            // SORT
            if (!string.IsNullOrWhiteSpace(sortBy))
            {
                switch (sortBy.ToLower())
                {
                    case "id":
                        query = sortDesc
                            ? query.OrderByDescending(s => s.Id)
                            : query.OrderBy(s => s.Id);
                        break;

                    case "starttime":
                        query = sortDesc
                            ? query.OrderByDescending(s => s.StartTime)
                            : query.OrderBy(s => s.StartTime);
                        break;

                    case "endtime":
                        query = sortDesc
                            ? query.OrderByDescending(s => s.EndTime)
                            : query.OrderBy(s => s.EndTime);
                        break;

                    case "room":
                        query = sortDesc
                            ? query.OrderByDescending(s => s.Room)
                            : query.OrderBy(s => s.Room);
                        break;
                }
            }
            else
            {
                query = query.OrderBy(s => s.StartTime);
            }

            // PAGINATION
            if (page < 1)
            {
                page = 1;
            }

            if (pageSize < 1)
            {
                pageSize = 20;
            }

            var totalItems = await query.CountAsync();

            var totalPages = (int)Math.Ceiling(
                (double)totalItems / pageSize);

            var schedules = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // DTO
            var data = schedules.Select(s => new ScheduleDto
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

            return new PagedResultDto<ScheduleDto>
            {
                Data = data,
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages
            };
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