using EnglishCenter.API.Jobs;
using EnglishCenter.API.Services;

namespace EnglishCenter.API.Background
{
    public class BackgroundJobWorker : BackgroundService
    {
        private readonly IBackgroundJobQueue _queue;
        private readonly ILogger<BackgroundJobWorker> _logger;

        public BackgroundJobWorker(
            IBackgroundJobQueue queue,
            ILogger<BackgroundJobWorker> logger)
        {
            _queue = queue;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var job = await _queue.DequeueAsync(stoppingToken);

                    int maxRetry = 3;
                    int retryCount = 0;

                    while (retryCount < maxRetry)
                    {
                        try
                        {
                            await job.ExecuteAsync();

                            // Job thành công
                            break;
                        }
                        catch (Exception ex)
                        {
                            retryCount++;

                            _logger.LogError(
                                ex,
                                "Job thất bại lần {RetryCount}/{MaxRetry}.",
                                retryCount,
                                maxRetry);

                            if (retryCount >= maxRetry)
                            {
                                _logger.LogError(
                                    "Job thất bại sau {MaxRetry} lần thử.",
                                    maxRetry);

                                break;
                            }

                            // Chờ trước khi thử lại
                            await Task.Delay(
                                TimeSpan.FromSeconds(2),
                                stoppingToken);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Lỗi khi xử lý Background Job.");
                }
            }
        }
    }
}