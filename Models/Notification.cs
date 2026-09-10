namespace EnglishCenter.API.Models
{
    public class Notification
    {
        public int Id { get; set; }

        // Người nhận thông báo
        public int UserId { get; set; }

        // Tiêu đề
        public string Title { get; set; } = string.Empty;

        // Nội dung
        public string Message { get; set; } = string.Empty;

        // Loại thông báo
        // COURSE, STUDENT, GRADE, PAYMENT...
        public string Type { get; set; } = string.Empty;

        // Đã đọc hay chưa
        public bool IsRead { get; set; } = false;

        // Thời gian tạo
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Quan hệ User
        public User? User { get; set; }
    }
}