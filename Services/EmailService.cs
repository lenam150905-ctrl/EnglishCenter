using System.Net;
using System.Net.Mail;
using EnglishCenter.API.Models;
using Microsoft.Extensions.Options;

namespace EnglishCenter.API.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _settings;

        public EmailService(
            IOptions<EmailSettings> settings)
        {
            _settings = settings.Value;
        }

        public async Task SendEmailAsync(
            string to,
            string subject,
            string body)
        {
            if (string.IsNullOrWhiteSpace(_settings.SmtpServer) ||
                _settings.Port <= 0 ||
                string.IsNullOrWhiteSpace(_settings.SenderEmail) ||
                string.IsNullOrWhiteSpace(_settings.Password))
            {
                throw new InvalidOperationException("Cấu hình EmailSettings chưa đầy đủ.");
            }

            using var message = new MailMessage();

            message.From = new MailAddress(
                _settings.SenderEmail,
                _settings.SenderName);

            message.To.Add(to);

            message.Subject = subject;

            message.Body = body;

            message.IsBodyHtml = false;

            using var smtp = new SmtpClient(
                _settings.SmtpServer,
                _settings.Port);

            smtp.UseDefaultCredentials = false;
            smtp.Credentials = new NetworkCredential(
                _settings.SenderEmail,
                _settings.Password);

            smtp.EnableSsl = _settings.EnableSsl;
            smtp.Timeout = 30_000;

            await smtp.SendMailAsync(message);
        }
    }
}
