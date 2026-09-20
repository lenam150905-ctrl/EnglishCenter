using System.Threading.Channels;
using EnglishCenter.API.Jobs;

namespace EnglishCenter.API.Services
{
    public class BackgroundJobQueue : IBackgroundJobQueue
    {
        private readonly Channel<IBackgroundJob> _queue;

        public BackgroundJobQueue()
        {
            _queue = Channel.CreateUnbounded<IBackgroundJob>();
        }

        public void Enqueue(IBackgroundJob job)
        {
            _queue.Writer.TryWrite(job);
        }

        public async ValueTask<IBackgroundJob> DequeueAsync(
            CancellationToken cancellationToken)
        {
            return await _queue.Reader.ReadAsync(cancellationToken);
        }
    }
}