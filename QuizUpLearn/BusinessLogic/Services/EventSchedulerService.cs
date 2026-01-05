using BusinessLogic.DTOs;
using BusinessLogic.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Repository.Interfaces;

namespace BusinessLogic.Services
{
    /// <summary>
    /// Background Service tự động cập nhật status Events
    /// - Check Events có EndDate đã qua → Update status = "Ended"
    /// - Update rank cho participants dựa trên score
    /// - Chạy định kỳ mỗi 5 phút
    /// </summary>
    public class EventSchedulerService : BackgroundService, IEventSchedulerService
    {
        private readonly ILogger<EventSchedulerService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(5);
        private readonly SemaphoreSlim _forceTrigger = new SemaphoreSlim(0);

        // Statistics tracking
        private DateTime? _lastRunTime;
        private DateTime? _nextRunTime;
        private int _lastRunEventsEnded;
        private int _totalEventsEnded;
        private int _totalRuns;
        private int _failedRuns;
        private bool _isRunning;

        // Properties từ interface
        public DateTime? LastRunTime => _lastRunTime;
        public DateTime? NextRunTime => _nextRunTime;
        public int LastRunEventsEnded => _lastRunEventsEnded;
        public bool IsRunning => _isRunning;

        public EventSchedulerService(
            ILogger<EventSchedulerService> logger,
            IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🚀 EventSchedulerService started - checking every {Minutes} minutes", _checkInterval.TotalMinutes);

            // Delay nhỏ để đảm bảo app đã khởi động hoàn toàn
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                _nextRunTime = DateTime.UtcNow.Add(_checkInterval);

                try
                {
                    _isRunning = true;
                    _totalRuns++;
                    
                    var eventsEnded = await ProcessExpiredEventsAsync(stoppingToken);
                    
                    _lastRunTime = DateTime.UtcNow;
                    _lastRunEventsEnded = eventsEnded;
                    _totalEventsEnded += eventsEnded;
                }
                catch (Exception ex)
                {
                    _failedRuns++;
                    _logger.LogError(ex, "❌ EventSchedulerService tick failed");
                }
                finally
                {
                    _isRunning = false;
                }

                // Chờ đến lần check tiếp theo HOẶC force trigger
                var delayTask = Task.Delay(_checkInterval, stoppingToken);
                var triggerTask = _forceTrigger.WaitAsync(stoppingToken);
                
                await Task.WhenAny(delayTask, triggerTask);

                // Nếu force triggered, reset semaphore
                if (triggerTask.IsCompleted)
                {
                    _logger.LogInformation("🔔 Force trigger activated - running check now");
                }
            }

            _logger.LogInformation("⏹️ EventSchedulerService stopped");
        }

        /// <summary>
        /// Force trigger check ngay lập tức
        /// </summary>
        public Task TriggerCheckNowAsync()
        {
            _logger.LogInformation("⚡ Manual trigger requested");
            _forceTrigger.Release();
            return Task.CompletedTask;
        }

        /// <summary>
        /// Lấy statistics về scheduler
        /// </summary>
        public Task<SchedulerStatistics> GetStatisticsAsync()
        {
            return Task.FromResult(new SchedulerStatistics
            {
                LastRunTime = _lastRunTime,
                NextRunTime = _nextRunTime,
                TotalEventsEnded = _totalEventsEnded,
                LastRunEventsEnded = _lastRunEventsEnded,
                TotalRuns = _totalRuns,
                FailedRuns = _failedRuns,
                IsRunning = _isRunning,
                CheckInterval = _checkInterval
            });
        }

        /// <summary>
        /// Process các Events đã hết hạn
        /// </summary>
        /// <returns>Số Events đã được ended</returns>
        private async Task<int> ProcessExpiredEventsAsync(CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var eventRepo = scope.ServiceProvider.GetRequiredService<IEventRepo>();
            var participantRepo = scope.ServiceProvider.GetRequiredService<IEventParticipantRepo>();

            var now = DateTime.UtcNow;
            _logger.LogInformation("⏰ Checking for expired events at {Time}", now);

            // Lấy các Events cần ending (Active và EndDate đã qua)
            var eventsToEnd = await eventRepo.GetEventsNeedEndingAsync();

            if (!eventsToEnd.Any())
            {
                _logger.LogDebug("✅ No expired events found");
                return 0;
            }

            _logger.LogInformation($"📋 Found {eventsToEnd.Count()} event(s) to end");

            int successCount = 0;
            int failCount = 0;

            foreach (var eventEntity in eventsToEnd)
            {
                try
                {
                    await EndEventAsync(eventEntity, eventRepo, participantRepo);
                    successCount++;
                }
                catch (Exception ex)
                {
                    failCount++;
                    _logger.LogError(ex, $"❌ Failed to end Event {eventEntity.Id} ({eventEntity.Name})");
                }
            }

            _logger.LogInformation($"📊 Event ending summary: {successCount} ended, {failCount} failed");
            return successCount;
        }

        /// <summary>
        /// End một Event và update ranks cho participants
        /// </summary>
        private async Task EndEventAsync(
            Repository.Entities.Event eventEntity,
            IEventRepo eventRepo,
            IEventParticipantRepo participantRepo)
        {
            _logger.LogInformation($"🏁 Ending Event: {eventEntity.Name} (ID: {eventEntity.Id})");

            // Step 1: Update Event status
            eventEntity.Status = "Ended";
            eventEntity.UpdatedAt = DateTime.UtcNow;
            await eventRepo.UpdateAsync(eventEntity);

            _logger.LogInformation($"✅ Event {eventEntity.Id} status updated to 'Ended'");

            // Step 2: Update participant ranks dựa trên score
            await UpdateParticipantRanksAsync(eventEntity.Id, participantRepo);

            _logger.LogInformation($"🎉 Event {eventEntity.Id} ({eventEntity.Name}) ended successfully");
        }

        /// <summary>
        /// Update rank cho tất cả participants của Event
        /// Rank dựa trên Score (cao → thấp), sau đó Accuracy
        /// </summary>
        private async Task UpdateParticipantRanksAsync(
            Guid eventId,
            IEventParticipantRepo participantRepo)
        {
            try
            {
                // Lấy tất cả participants và sort
                var participants = await participantRepo.GetByEventIdAsync(eventId);
                var sortedParticipants = participants
                    .OrderByDescending(p => p.Score)
                    .ThenByDescending(p => p.Accuracy)
                    .ThenBy(p => p.JoinAt)
                    .ToList();

                if (!sortedParticipants.Any())
                {
                    _logger.LogDebug($"No participants found for Event {eventId}");
                    return;
                }

                _logger.LogInformation($"📊 Updating ranks for {sortedParticipants.Count} participant(s)");

                // Update rank cho từng participant
                long currentRank = 1;
                foreach (var participant in sortedParticipants)
                {
                    participant.Rank = currentRank;
                    participant.UpdatedAt = DateTime.UtcNow;
                    
                    // Set FinishAt nếu chưa có
                    if (!participant.FinishAt.HasValue)
                    {
                        participant.FinishAt = DateTime.UtcNow;
                    }

                    await participantRepo.UpdateAsync(participant);
                    
                    _logger.LogDebug($"Updated Rank {currentRank} for Participant {participant.ParticipantId}");
                    currentRank++;
                }

                _logger.LogInformation($"✅ Successfully updated ranks for {sortedParticipants.Count} participant(s)");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Failed to update participant ranks for Event {eventId}");
                throw;
            }
        }
    }
}

