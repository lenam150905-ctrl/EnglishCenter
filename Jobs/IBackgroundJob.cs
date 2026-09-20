namespace EnglishCenter.API.Jobs
{
    public interface IBackgroundJob
    {
        Task ExecuteAsync();
    }
}