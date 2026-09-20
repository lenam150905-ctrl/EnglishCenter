using EnglishCenter.API.Services;
using Microsoft.AspNetCore.Http;

namespace EnglishCenter.API.Jobs
{
    public class ImportStudentJob : IBackgroundJob
    {
        private readonly IServiceScopeFactory _scopeFactory;

        private readonly string _filePath;
        private readonly int _userId;
        private readonly string? _ipAddress;

        public ImportStudentJob(
            IServiceScopeFactory scopeFactory,
            string filePath,
            int userId,
            string? ipAddress)
        {
            _scopeFactory = scopeFactory;
            _filePath = filePath;
            _userId = userId;
            _ipAddress = ipAddress;
        }

        public async Task ExecuteAsync()
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();

                var studentService =
                    scope.ServiceProvider
                        .GetRequiredService<IStudentService>();

                await using var stream = new FileStream(
                    _filePath,
                    FileMode.Open,
                    FileAccess.Read);

                var file = new FormFile(
                    stream,
                    0,
                    stream.Length,
                    "file",
                    Path.GetFileName(_filePath))
                {
                    Headers = new HeaderDictionary(),
                    ContentType =
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
                };

                await studentService.ImportExcelAsync(
                    file,
                    _userId,
                    _ipAddress);
            }
            finally
            {
                if (File.Exists(_filePath))
                {
                    File.Delete(_filePath);
                }
            }
        }
    }
}