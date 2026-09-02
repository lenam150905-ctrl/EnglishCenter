namespace EnglishCenter.API.DTOs
{
    public class ScheduleDto
    {
        public int Id { get; set; }
        public int CourseId { get; set; }
        public string CourseName { get; set; } = string.Empty;
        public int TeacherId { get; set; }
        public string TeacherName { get; set; } = string.Empty;
        public DateTime StartTime { get; set; } 
        public DateTime EndTime { get; set; }
        public string Room { get; set; } 
    }
}
