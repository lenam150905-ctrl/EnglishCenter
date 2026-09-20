using EnglishCenter.API.Services;

namespace EnglishCenter.API.Jobs
{
    public class EmailJob : IBackgroundJob
    {
        private readonly IEmailService _emailService;
        private readonly string _to;
        private readonly string _subject;
        private readonly string _body;

        public EmailJob(
            IEmailService emailService,
            string to,
            string subject,
            string body)
        {
            _emailService = emailService;
            _to = to;
            _subject = subject;
            _body = body;
        }

        public async Task ExecuteAsync()
        {
            await _emailService.SendEmailAsync(
                _to,
                _subject,
                _body);
        }
    }
}