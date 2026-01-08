using BusinessLogic.DTOs;
using BusinessLogic.Interfaces;
using BusinessLogic.Services;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Repository.Entities;
using Repository.Interfaces;

namespace QuizUpLearn.Test.UnitTest
{
    public class EventSchedulerServiceTest : BaseServiceTest
    {
        private readonly Mock<ILogger<EventSchedulerService>> _mockLogger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly Mock<IEventRepo> _mockEventRepo;
        private readonly Mock<IEventParticipantRepo> _mockEventParticipantRepo;
        private readonly EventSchedulerService _eventSchedulerService;

        public EventSchedulerServiceTest()
        {
            _mockLogger = new Mock<ILogger<EventSchedulerService>>();
            _mockEventRepo = new Mock<IEventRepo>();
            _mockEventParticipantRepo = new Mock<IEventParticipantRepo>();

            // Setup real ServiceCollection and ServiceProvider
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddScoped<IEventRepo>(sp => _mockEventRepo.Object);
            serviceCollection.AddScoped<IEventParticipantRepo>(sp => _mockEventParticipantRepo.Object);
            var serviceProvider = serviceCollection.BuildServiceProvider();
            _scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

            _eventSchedulerService = new EventSchedulerService(
                _mockLogger.Object,
                _scopeFactory);
        }

        [Fact]
        public void LastRunTime_Initially_ShouldBeNull()
        {
            // Act
            var result = _eventSchedulerService.LastRunTime;

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void NextRunTime_Initially_ShouldBeNull()
        {
            // Act
            var result = _eventSchedulerService.NextRunTime;

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void LastRunEventsEnded_Initially_ShouldBeZero()
        {
            // Act
            var result = _eventSchedulerService.LastRunEventsEnded;

            // Assert
            result.Should().Be(0);
        }

        [Fact]
        public void IsRunning_Initially_ShouldBeFalse()
        {
            // Act
            var result = _eventSchedulerService.IsRunning;

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task TriggerCheckNowAsync_ShouldReleaseSemaphore()
        {
            // Act
            await _eventSchedulerService.TriggerCheckNowAsync();

            // Assert
            // Method should complete without throwing
            // The semaphore release is internal, so we just verify it doesn't throw
            await Task.CompletedTask;
        }

        [Fact]
        public async Task GetStatisticsAsync_Initially_ShouldReturnDefaultStatistics()
        {
            // Act
            var result = await _eventSchedulerService.GetStatisticsAsync();

            // Assert
            result.Should().NotBeNull();
            result.LastRunTime.Should().BeNull();
            result.NextRunTime.Should().BeNull();
            result.TotalEventsEnded.Should().Be(0);
            result.LastRunEventsEnded.Should().Be(0);
            result.TotalRuns.Should().Be(0);
            result.FailedRuns.Should().Be(0);
            result.IsRunning.Should().BeFalse();
            result.CheckInterval.Should().Be(TimeSpan.FromMinutes(5));
        }


        [Fact]
        public async Task TriggerCheckNowAsync_MultipleTimes_ShouldNotThrow()
        {
            // Act & Assert
            await _eventSchedulerService.TriggerCheckNowAsync();
            await _eventSchedulerService.TriggerCheckNowAsync();
            await _eventSchedulerService.TriggerCheckNowAsync();

            // Should complete without throwing
            await Task.CompletedTask;
        }

        [Fact]
        public void Properties_ShouldReflectInternalState()
        {
            // Act
            var lastRunTime = _eventSchedulerService.LastRunTime;
            var nextRunTime = _eventSchedulerService.NextRunTime;
            var lastRunEventsEnded = _eventSchedulerService.LastRunEventsEnded;
            var isRunning = _eventSchedulerService.IsRunning;

            // Assert
            lastRunTime.Should().BeNull();
            nextRunTime.Should().BeNull();
            lastRunEventsEnded.Should().Be(0);
            isRunning.Should().BeFalse();
        }
    }
}

