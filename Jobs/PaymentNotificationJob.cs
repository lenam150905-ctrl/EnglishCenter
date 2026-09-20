using EnglishCenter.API.DTOs;
using EnglishCenter.API.Services;

namespace EnglishCenter.API.Jobs
{
    public class PaymentNotificationJob : IBackgroundJob
    {
        private readonly INotificationService _notificationService;

        private readonly int _userId;
        private readonly string _title;
        private readonly string _message;

        public PaymentNotificationJob(
            INotificationService notificationService,
            int userId,
            string title,
            string message)
        {
            _notificationService = notificationService;
            _userId = userId;
            _title = title;
            _message = message;
        }

        public async Task ExecuteAsync()
        {
            await _notificationService.CreateForUserAsync(
                new NotificationCreateDto
                {
                    UserId = _userId,
                    Title = _title,
                    Message = _message,
                    Type = "PAYMENT"
                });
        }
    }
}