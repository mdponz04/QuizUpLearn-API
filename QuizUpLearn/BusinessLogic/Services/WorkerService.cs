using BusinessLogic.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Collections.Concurrent;

namespace BusinessLogic.Services
{
    public class WorkerService : BackgroundService, IWorkerService
    {
        private readonly ConcurrentQueue<Func<IServiceProvider, CancellationToken, Task>> _jobQueue = new();
        private readonly SemaphoreSlim _signal = new(0);
        private readonly IServiceScopeFactory _scopeFactory;

        public WorkerService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public Task EnqueueJob(Func<IServiceProvider, CancellationToken, Task> job)
        {
            _jobQueue.Enqueue(job);
            _signal.Release();
            return Task.CompletedTask;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await _signal.WaitAsync(stoppingToken);

                if (_jobQueue.TryDequeue(out var job) && job is not null)
                {
                    using var scope = _scopeFactory.CreateScope();
                    var sp = scope.ServiceProvider;

                    try
                    {
                        await job(sp, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Job failed: {ex.Message}");
                    }
                }
            }
        }
    }

}
