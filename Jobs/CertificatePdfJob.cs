using EnglishCenter.API.Services;

namespace EnglishCenter.API.Jobs
{
    public class CertificatePdfJob : IBackgroundJob
    {
        private readonly IServiceScopeFactory _scopeFactory;

        private readonly int _certificateId;
        private readonly int? _userId;
        private readonly string? _ipAddress;

        public CertificatePdfJob(
            IServiceScopeFactory scopeFactory,
            int certificateId,
            int? userId,
            string? ipAddress)
        {
            _scopeFactory = scopeFactory;
            _certificateId = certificateId;
            _userId = userId;
            _ipAddress = ipAddress;
        }

        public async Task ExecuteAsync()
        {
            using var scope =
                _scopeFactory.CreateScope();

            var certificatePdfService =
                scope.ServiceProvider
                    .GetRequiredService<ICertificatePdfService>();

            await certificatePdfService
                .GenerateCertificatePdfAsync(
                    _certificateId,
                    _userId,
                    _ipAddress);
        }
    }
}