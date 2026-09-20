namespace EnglishCenter.API.Services
{
    public interface ICertificatePdfService
    {
        Task<string> GenerateCertificatePdfAsync(
    int certificateId,
    int? userId,
    string? ipAddress);
    }
}