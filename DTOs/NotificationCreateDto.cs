namespace EnglishCenter.API.DTOs
{
    public class NotificationCreateDto
    {
        public int UserId { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public string Type { get; set; } = string.Empty;
    }
}