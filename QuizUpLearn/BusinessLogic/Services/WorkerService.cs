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
        private readonly ConcurrentDictionary<Guid, Guid> _activeJobs = new();

        public WorkerService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public Task EnqueueJob(Func<IServiceProvider, CancellationToken, Task> job)
        {
            if(job == null) throw  new ArgumentNullException("Background job cant be null");
            _jobQueue.Enqueue(job);
            _signal.Release();
            return Task.CompletedTask;
        }

        public void RegisterActiveJob(Guid userId, Guid jobId)
        {
            _activeJobs[userId] = (jobId);
        }

        public void RegisterActiveJob(List<Guid> userIds, Guid jobId)
        {
            foreach (var userId in userIds)
            {
                _activeJobs[userId] = (jobId);
            }
        }

        public void RemoveActiveJob(Guid jobId)
        {
            // Find and remove the job entry by jobId
            var entry = _activeJobs.FirstOrDefault(kvp => kvp.Value == jobId);
            if (entry.Key != Guid.Empty)
            {
                _activeJobs.TryRemove(entry.Key, out _);
            }
        }

        public Guid? GetActiveJobForUser(Guid userId)
        {
            if (_activeJobs.TryGetValue(userId, out var job))
            {
                return job;
            }
            return null;
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
