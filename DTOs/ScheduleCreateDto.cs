using System.ComponentModel.DataAnnotations;

public class ScheduleCreateDto
{
    [Required(ErrorMessage = "Mã khóa học không được để trống")]
    public int CourseId { get; set; }

    [Required(ErrorMessage = "Mã giáo viên  không được để trống")]
    public int TeacherId { get; set; }

    [Required(ErrorMessage = "Ngày bắt đầu không được để trống")]
    public DateTime StartTime { get; set; }

    [Required(ErrorMessage = "Ngày kết thúc không được để trống")]
    public DateTime EndTime { get; set; }

    [Required(ErrorMessage = "Phòng học không được để trống")]
    [StringLength(50,ErrorMessage = "Phòng học phải dài tối đa 50 kí tự")]
    public string Room { get; set; } = string.Empty;
}