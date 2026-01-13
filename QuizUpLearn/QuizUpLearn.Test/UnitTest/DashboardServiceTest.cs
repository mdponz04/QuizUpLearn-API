using AutoMapper;
using BusinessLogic.DTOs.DashboardDtos;
using BusinessLogic.Interfaces;
using BusinessLogic.MappingProfile;
using BusinessLogic.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Repository.Entities;
using Repository.Interfaces;

namespace QuizUpLearn.Test.UnitTest
{
    public class DashboardServiceTest : BaseControllerTest
    {
        private readonly Mock<IQuizAttemptRepo> _mockQuizAttemptRepo;
        private readonly Mock<IQuizRepo> _mockQuizRepo;
        private readonly Mock<IQuizSetRepo> _mockQuizSetRepo;
        private readonly Mock<IAccountRepo> _mockAccountRepo;
        private readonly Mock<IUserRepo> _mockUserRepo;
        private readonly IMapper _mapper;
        private readonly DashboardService _dashboardService;

        public DashboardServiceTest()
        {
            _mockQuizAttemptRepo = new Mock<IQuizAttemptRepo>();
            _mockQuizRepo = new Mock<IQuizRepo>();
            _mockQuizSetRepo = new Mock<IQuizSetRepo>();
            _mockAccountRepo = new Mock<IAccountRepo>();
            _mockUserRepo = new Mock<IUserRepo>();

            // Setup real AutoMapper with the actual mapping profile
            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<MappingProfile>();
            }, new NullLoggerFactory());
            _mapper = mapperConfig.CreateMapper();

            // Setup default logger
            var logger = new NullLogger<DashboardService>();

            _dashboardService = new DashboardService(
                _mockQuizAttemptRepo.Object,
                _mockQuizRepo.Object,
                _mockQuizSetRepo.Object,
                _mockAccountRepo.Object,
                _mockUserRepo.Object,
                _mapper,
                logger);
        }

        [Fact]
        public async Task GetDashboardDataAsync_WithValidUserId_ShouldReturnDashboardResponse()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var attempts = new List<QuizAttempt>();

            _mockQuizAttemptRepo.Setup(r => r.GetByUserIdAsync(userId, false))
                .ReturnsAsync(attempts);
            _mockAccountRepo.Setup(r => r.GetAllAsync(false))
                .ReturnsAsync(new List<Account>());
            _mockUserRepo.Setup(r => r.GetByIdAsync(userId))
                .ReturnsAsync(new User
                {
                    Id = userId,
                    Username = "testuser",
                    AvatarUrl = "https://example.com/avatar.jpg",
                    TotalPoints = 100
                });

            // Act
            var result = await _dashboardService.GetDashboardDataAsync(userId);

            // Assert
            result.Should().NotBeNull();
            result.Stats.Should().NotBeNull();
            result.Progress.Should().NotBeNull();
            result.RecentActivities.Should().NotBeNull();
            result.EventParticipations.Should().NotBeNull();
            result.RecentQuizHistory.Should().NotBeNull();
            result.WeakPoints.Should().NotBeNull();
            result.LastUpdated.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public async Task GetUserStatsAsync_WithValidUserId_ShouldReturnStats()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var quizSetId = Guid.NewGuid();
            var attempts = new List<QuizAttempt>
            {
                new QuizAttempt
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    QuizSetId = quizSetId,
                    TotalQuestions = 10,
                    CorrectAnswers = 8,
                    WrongAnswers = 2,
                    Accuracy = 80,
                    Score = 80,
                    Status = "completed",
                    CreatedAt = DateTime.UtcNow
                },
                new QuizAttempt
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    QuizSetId = quizSetId,
                    TotalQuestions = 10,
                    CorrectAnswers = 7,
                    WrongAnswers = 3,
                    Accuracy = 70,
                    Score = 70,
                    Status = "completed",
                    CreatedAt = DateTime.UtcNow.AddDays(-1)
                }
            };

            _mockQuizAttemptRepo.Setup(r => r.GetByUserIdAsync(userId, false))
                .ReturnsAsync(attempts);
            _mockAccountRepo.Setup(r => r.GetAllAsync(false))
                .ReturnsAsync(new List<Account>());
            _mockUserRepo.Setup(r => r.GetByIdAsync(userId))
                .ReturnsAsync(new User
                {
                    Id = userId,
                    Username = "testuser",
                    AvatarUrl = "https://example.com/avatar.jpg",
                    TotalPoints = 150
                });

            // Act
            var result = await _dashboardService.GetUserStatsAsync(userId);

            // Assert
            result.Should().NotBeNull();
            result.TotalQuizzes.Should().Be(2);
            result.TotalQuestions.Should().Be(20);
            result.TotalCorrectAnswers.Should().Be(15);
            result.TotalWrongAnswers.Should().Be(5);
            result.AccuracyRate.Should().Be(75.0);
            result.TotalPoints.Should().Be(150);
        }

        [Fact]
        public async Task GetUserStatsAsync_WithNoAttempts_ShouldReturnZeroStats()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var attempts = new List<QuizAttempt>();

            _mockQuizAttemptRepo.Setup(r => r.GetByUserIdAsync(userId, false))
                .ReturnsAsync(attempts);
            _mockAccountRepo.Setup(r => r.GetAllAsync(false))
                .ReturnsAsync(new List<Account>());
            _mockUserRepo.Setup(r => r.GetByIdAsync(userId))
                .ReturnsAsync(new User
                {
                    Id = userId,
                    Username = "testuser",
                    AvatarUrl = "https://example.com/avatar.jpg",
                    TotalPoints = 0
                });

            // Act
            var result = await _dashboardService.GetUserStatsAsync(userId);

            // Assert
            result.Should().NotBeNull();
            result.TotalQuizzes.Should().Be(0);
            result.TotalQuestions.Should().Be(0);
            result.AccuracyRate.Should().Be(0);
            result.CurrentStreak.Should().Be(0);
        }

        [Fact]
        public async Task GetUserProgressAsync_WithValidUserId_ShouldReturnProgressChart()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var quizSetId = Guid.NewGuid();
            var attempts = new List<QuizAttempt>
            {
                new QuizAttempt
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    QuizSetId = quizSetId,
                    TotalQuestions = 10,
                    CorrectAnswers = 8,
                    WrongAnswers = 2,
                    Accuracy = 80,
                    Status = "completed",
                    CreatedAt = DateTime.UtcNow
                }
            };

            _mockQuizAttemptRepo.Setup(r => r.GetByUserIdAsync(userId, false))
                .ReturnsAsync(attempts);

            // Act
            var result = await _dashboardService.GetUserProgressAsync(userId, 7);

            // Assert
            result.Should().NotBeNull();
            result.WeeklyProgress.Should().NotBeNull();
            result.WeeklyProgress.Should().HaveCount(7);
            result.TotalQuestions.Should().Be(10);
            result.TotalCorrectAnswers.Should().Be(8);
        }

        [Fact]
        public async Task GetUserProgressAsync_WithCustomDays_ShouldReturnCorrectNumberOfDays()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var attempts = new List<QuizAttempt>();

            _mockQuizAttemptRepo.Setup(r => r.GetByUserIdAsync(userId, false))
                .ReturnsAsync(attempts);

            // Act
            var result = await _dashboardService.GetUserProgressAsync(userId, 14);

            // Assert
            result.Should().NotBeNull();
            result.WeeklyProgress.Should().HaveCount(14);
        }

        [Fact]
        public async Task GetRecentActivitiesAsync_WithValidUserId_ShouldReturnActivities()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var quizSetId = Guid.NewGuid();
            var quizSet = new QuizSet
            {
                Id = quizSetId,
                Title = "Test Quiz Set",
                Description = "Test Description",
                CreatedAt = DateTime.UtcNow
            };

            var attempts = new List<QuizAttempt>
            {
                new QuizAttempt
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    QuizSetId = quizSetId,
                    TotalQuestions = 10,
                    CorrectAnswers = 8,
                    Accuracy = 80,
                    Score = 80,
                    Status = "completed",
                    CreatedAt = DateTime.UtcNow
                }
            };

            _mockQuizAttemptRepo.Setup(r => r.GetByUserIdAsync(userId, false))
                .ReturnsAsync(attempts);
            _mockQuizSetRepo.Setup(r => r.GetQuizSetByIdAsync(quizSetId))
                .ReturnsAsync(quizSet);

            // Act
            var result = await _dashboardService.GetRecentActivitiesAsync(userId, 10);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result.First().ActivityType.Should().Be("QuizCompleted");
            result.First().Description.Should().Contain("Test Quiz Set");
        }

        [Fact]
        public async Task GetRecentActivitiesAsync_WithCountLimit_ShouldReturnLimitedResults()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var quizSetId = Guid.NewGuid();
            var quizSet = new QuizSet
            {
                Id = quizSetId,
                Title = "Test Quiz Set",
                CreatedAt = DateTime.UtcNow
            };

            var attempts = new List<QuizAttempt>();
            for (int i = 0; i < 15; i++)
            {
                attempts.Add(new QuizAttempt
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    QuizSetId = quizSetId,
                    TotalQuestions = 10,
                    CorrectAnswers = 8,
                    Accuracy = 80,
                    Score = 80,
                    Status = "completed",
                    CreatedAt = DateTime.UtcNow.AddDays(-i)
                });
            }

            _mockQuizAttemptRepo.Setup(r => r.GetByUserIdAsync(userId, false))
                .ReturnsAsync(attempts);
            _mockQuizSetRepo.Setup(r => r.GetQuizSetByIdAsync(quizSetId))
                .ReturnsAsync(quizSet);

            // Act
            var result = await _dashboardService.GetRecentActivitiesAsync(userId, 5);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(5);
        }

        [Fact]
        public async Task GetEventParticipationsAsync_WithValidUserId_ShouldReturnParticipations()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var quizSetId = Guid.NewGuid();
            var quizSet = new QuizSet
            {
                Id = quizSetId,
                Title = "Test Quiz Set",
                Description = "Test Description",
                CreatedAt = DateTime.UtcNow
            };

            var attempts = new List<QuizAttempt>
            {
                new QuizAttempt
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    QuizSetId = quizSetId,
                    TotalQuestions = 10,
                    CorrectAnswers = 8,
                    Accuracy = 80,
                    Score = 80,
                    Status = "completed",
                    CreatedAt = DateTime.UtcNow
                }
            };

            _mockQuizAttemptRepo.Setup(r => r.GetByUserIdAsync(userId, false))
                .ReturnsAsync(attempts);
            _mockQuizSetRepo.Setup(r => r.GetQuizSetByIdAsync(quizSetId))
                .ReturnsAsync(quizSet);

            // Act
            var result = await _dashboardService.GetEventParticipationsAsync(userId, 5);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result.First().EventName.Should().Contain("Test Quiz Set");
            result.First().Status.Should().Be("Completed");
        }

        [Fact]
        public async Task GetRecentQuizHistoryAsync_WithValidUserId_ShouldReturnQuizHistory()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var quizSetId = Guid.NewGuid();
            var quizSet = new QuizSet
            {
                Id = quizSetId,
                Title = "Test Quiz Set",
                Description = "Test Description",
                CreatedAt = DateTime.UtcNow
            };

            var attempts = new List<QuizAttempt>
            {
                new QuizAttempt
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    QuizSetId = quizSetId,
                    TotalQuestions = 10,
                    CorrectAnswers = 8,
                    WrongAnswers = 2,
                    Accuracy = 80,
                    Score = 80,
                    Status = "completed",
                    TimeSpent = 15,
                    CreatedAt = DateTime.UtcNow
                }
            };

            _mockQuizAttemptRepo.Setup(r => r.GetByUserIdAsync(userId, false))
                .ReturnsAsync(attempts);
            _mockQuizSetRepo.Setup(r => r.GetQuizSetByIdAsync(quizSetId))
                .ReturnsAsync(quizSet);

            // Act
            var result = await _dashboardService.GetRecentQuizHistoryAsync(userId, 10);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result.First().QuizName.Should().Be("Test Quiz Set");
            result.First().TotalQuestions.Should().Be(10);
            result.First().CorrectAnswers.Should().Be(8);
            result.First().ScorePercentage.Should().Be(80);
            result.First().Status.Should().Be("Passed");
        }

        [Fact]
        public async Task GetRecentQuizHistoryAsync_WithLowAccuracy_ShouldReturnFailedStatus()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var quizSetId = Guid.NewGuid();
            var quizSet = new QuizSet
            {
                Id = quizSetId,
                Title = "Test Quiz Set",
                Description = "Test Description",
                CreatedAt = DateTime.UtcNow
            };

            var attempts = new List<QuizAttempt>
            {
                new QuizAttempt
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    QuizSetId = quizSetId,
                    TotalQuestions = 10,
                    CorrectAnswers = 5,
                    WrongAnswers = 5,
                    Accuracy = 50,
                    Score = 50,
                    Status = "completed",
                    CreatedAt = DateTime.UtcNow
                }
            };

            _mockQuizAttemptRepo.Setup(r => r.GetByUserIdAsync(userId, false))
                .ReturnsAsync(attempts);
            _mockQuizSetRepo.Setup(r => r.GetQuizSetByIdAsync(quizSetId))
                .ReturnsAsync(quizSet);

            // Act
            var result = await _dashboardService.GetRecentQuizHistoryAsync(userId, 10);

            // Assert
            result.Should().NotBeNull();
            result.First().Status.Should().Be("Failed");
        }

        [Fact]
        public async Task GetUserWeakPointsAsync_WithValidUserId_ShouldReturnWeakPoints()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var quizSetId = Guid.NewGuid();
            var quizSet = new QuizSet
            {
                Id = quizSetId,
                Title = "Weak Topic",
                Description = "Test Description",
                CreatedAt = DateTime.UtcNow
            };

            var attempts = new List<QuizAttempt>
            {
                new QuizAttempt
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    QuizSetId = quizSetId,
                    TotalQuestions = 10,
                    CorrectAnswers = 5,
                    WrongAnswers = 5,
                    Accuracy = 50,
                    Score = 50,
                    Status = "completed",
                    CreatedAt = DateTime.UtcNow
                }
            };

            _mockQuizAttemptRepo.Setup(r => r.GetByUserIdAsync(userId, false))
                .ReturnsAsync(attempts);
            _mockQuizSetRepo.Setup(r => r.GetQuizSetByIdAsync(quizSetId))
                .ReturnsAsync(quizSet);

            // Act
            var result = await _dashboardService.GetUserWeakPointsAsync(userId, 5);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result.First().Topic.Should().Be("Weak Topic");
            result.First().AccuracyRate.Should().Be(50.0);
        }

        [Fact]
        public async Task GetUserWeakPointsAsync_WithHighAccuracy_ShouldReturnEmpty()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var quizSetId = Guid.NewGuid();
            var attempts = new List<QuizAttempt>
            {
                new QuizAttempt
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    QuizSetId = quizSetId,
                    TotalQuestions = 10,
                    CorrectAnswers = 8,
                    WrongAnswers = 2,
                    Accuracy = 80,
                    Score = 80,
                    Status = "completed",
                    CreatedAt = DateTime.UtcNow
                }
            };

            _mockQuizAttemptRepo.Setup(r => r.GetByUserIdAsync(userId, false))
                .ReturnsAsync(attempts);

            // Act
            var result = await _dashboardService.GetUserWeakPointsAsync(userId, 5);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task RecordActivityAsync_WithValidData_ShouldReturnTrue()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var activityType = "QuizCompleted";
            var description = "Completed a quiz";
            var metadata = new Dictionary<string, object>
            {
                { "QuizId", Guid.NewGuid() },
                { "Score", 85 }
            };

            // Act
            var result = await _dashboardService.RecordActivityAsync(userId, activityType, description, metadata);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task RecordActivityAsync_WithNullMetadata_ShouldReturnTrue()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var activityType = "QuizCompleted";
            var description = "Completed a quiz";

            // Act
            var result = await _dashboardService.RecordActivityAsync(userId, activityType, description, null);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task GetUserStatsAsync_WithStreak_ShouldCalculateStreak()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var quizSetId = Guid.NewGuid();
            var attempts = new List<QuizAttempt>
            {
                new QuizAttempt
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    QuizSetId = quizSetId,
                    TotalQuestions = 10,
                    CorrectAnswers = 8,
                    Accuracy = 80,
                    Status = "completed",
                    CreatedAt = DateTime.UtcNow
                },
                new QuizAttempt
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    QuizSetId = quizSetId,
                    TotalQuestions = 10,
                    CorrectAnswers = 7,
                    Accuracy = 70,
                    Status = "completed",
                    CreatedAt = DateTime.UtcNow.AddDays(-1)
                }
            };

            _mockQuizAttemptRepo.Setup(r => r.GetByUserIdAsync(userId, false))
                .ReturnsAsync(attempts);
            _mockAccountRepo.Setup(r => r.GetAllAsync(false))
                .ReturnsAsync(new List<Account>());
            _mockUserRepo.Setup(r => r.GetByIdAsync(userId))
                .ReturnsAsync(new User
                {
                    Id = userId,
                    Username = "testuser",
                    AvatarUrl = "https://example.com/avatar.jpg",
                    TotalPoints = 100
                });

            // Act
            var result = await _dashboardService.GetUserStatsAsync(userId);

            // Assert
            result.Should().NotBeNull();
            result.CurrentStreak.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task GetUserProgressAsync_WithNoRecentAttempts_ShouldReturnZeroProgress()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var attempts = new List<QuizAttempt>();

            _mockQuizAttemptRepo.Setup(r => r.GetByUserIdAsync(userId, false))
                .ReturnsAsync(attempts);

            // Act
            var result = await _dashboardService.GetUserProgressAsync(userId, 7);

            // Assert
            result.Should().NotBeNull();
            result.WeeklyProgress.Should().HaveCount(7);
            result.WeeklyProgress.All(p => p.ScorePercentage == 0).Should().BeTrue();
            result.OverallAccuracy.Should().Be(0);
        }
    }
}

