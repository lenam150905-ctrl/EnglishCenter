using EnglishCenter.API.Jobs;

namespace EnglishCenter.API.Services
{
    public interface IBackgroundJobQueue
    {
        void Enqueue(IBackgroundJob job);

        ValueTask<IBackgroundJob> DequeueAsync(
            CancellationToken cancellationToken);
    }
}