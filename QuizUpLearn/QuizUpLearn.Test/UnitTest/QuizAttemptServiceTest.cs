using AutoMapper;
using BusinessLogic.DTOs;
using BusinessLogic.Interfaces;
using BusinessLogic.MappingProfile;
using BusinessLogic.Services;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Repository.Entities;
using Repository.Enums;
using Repository.Interfaces;

namespace QuizUpLearn.Test.UnitTest
{
    public class QuizAttemptServiceTest : BaseServiceTest
    {
        private readonly Mock<IQuizAttemptRepo> _mockQuizAttemptRepo;
        private readonly Mock<IQuizAttemptDetailRepo> _mockDetailRepo;
        private readonly Mock<IQuizRepo> _mockQuizRepo;
        private readonly Mock<IUserMistakeRepo> _mockUserMistakeRepo;
        private readonly Mock<IUserMistakeService> _mockUserMistakeService;
        private readonly Mock<IQuizSetRepo> _mockQuizSetRepo;
        private readonly Mock<IQuizQuizSetRepo> _mockQuizQuizSetRepo;
        private readonly Mock<IAnswerOptionRepo> _mockAnswerOptionRepo;
        private readonly Mock<ITournamentQuizSetRepo> _mockTournamentQuizSetRepo;
        private readonly Mock<ITournamentParticipantRepo> _mockTournamentParticipantRepo;
        private readonly Mock<IServiceProvider> _mockServiceProvider;
        private readonly IMapper _mapper;
        private readonly QuizAttemptService _quizAttemptService;

        public QuizAttemptServiceTest()
        {
            _mockQuizAttemptRepo = new Mock<IQuizAttemptRepo>();
            _mockDetailRepo = new Mock<IQuizAttemptDetailRepo>();
            _mockQuizRepo = new Mock<IQuizRepo>();
            _mockUserMistakeRepo = new Mock<IUserMistakeRepo>();
            _mockUserMistakeService = new Mock<IUserMistakeService>();
            _mockQuizSetRepo = new Mock<IQuizSetRepo>();
            _mockQuizQuizSetRepo = new Mock<IQuizQuizSetRepo>();
            _mockAnswerOptionRepo = new Mock<IAnswerOptionRepo>();
            _mockTournamentQuizSetRepo = new Mock<ITournamentQuizSetRepo>();
            _mockTournamentParticipantRepo = new Mock<ITournamentParticipantRepo>();
            _mockServiceProvider = new Mock<IServiceProvider>();

            // Setup real AutoMapper with the actual mapping profile
            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<MappingProfile>();
            }, new NullLoggerFactory());
            _mapper = mapperConfig.CreateMapper();

            _quizAttemptService = new QuizAttemptService(
                _mockQuizAttemptRepo.Object,
                _mockDetailRepo.Object,
                _mockQuizRepo.Object,
                _mockUserMistakeRepo.Object,
                _mockUserMistakeService.Object,
                _mockQuizSetRepo.Object,
                _mockQuizQuizSetRepo.Object,
                _mockAnswerOptionRepo.Object,
                _mockTournamentQuizSetRepo.Object,
                _mockTournamentParticipantRepo.Object,
                _mapper,
                _mockServiceProvider.Object);
        }

        [Fact]
        public async Task CreateAsync_WithValidData_ShouldReturnResponseQuizAttemptDto()
        {
            // Arrange
            var requestDto = new RequestQuizAttemptDto
            {
                UserId = Guid.NewGuid(),
                QuizSetId = Guid.NewGuid(),
                AttemptType = "single",
                TotalQuestions = 10,
                CorrectAnswers = 0,
                WrongAnswers = 0,
                Score = 0,
                Accuracy = 0,
                Status = "in_progress"
            };

            var createdAttempt = new QuizAttempt
            {
                Id = Guid.NewGuid(),
                UserId = requestDto.UserId,
                QuizSetId = requestDto.QuizSetId,
                AttemptType = requestDto.AttemptType,
                TotalQuestions = requestDto.TotalQuestions,
                CorrectAnswers = requestDto.CorrectAnswers,
                WrongAnswers = requestDto.WrongAnswers,
                Score = requestDto.Score,
                Accuracy = requestDto.Accuracy,
                Status = requestDto.Status,
                CreatedAt = DateTime.UtcNow
            };

            _mockQuizAttemptRepo.Setup(r => r.CreateAsync(It.IsAny<QuizAttempt>()))
                .ReturnsAsync(createdAttempt);

            // Act
            var result = await _quizAttemptService.CreateAsync(requestDto);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(createdAttempt.Id);
            result.UserId.Should().Be(requestDto.UserId);
            result.QuizSetId.Should().Be(requestDto.QuizSetId);
            result.AttemptType.Should().Be(requestDto.AttemptType);
            result.Status.Should().Be(requestDto.Status);

            _mockQuizAttemptRepo.Verify(r => r.CreateAsync(It.Is<QuizAttempt>(a =>
                a.UserId == requestDto.UserId &&
                a.QuizSetId == requestDto.QuizSetId &&
                a.AttemptType == requestDto.AttemptType)), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_WithValidId_ShouldReturnResponseQuizAttemptDto()
        {
            // Arrange
            var attemptId = Guid.NewGuid();
            var attempt = new QuizAttempt
            {
                Id = attemptId,
                UserId = Guid.NewGuid(),
                QuizSetId = Guid.NewGuid(),
                AttemptType = "single",
                TotalQuestions = 10,
                CorrectAnswers = 8,
                WrongAnswers = 2,
                Score = 80,
                Accuracy = 80,
                Status = "completed",
                CreatedAt = DateTime.UtcNow
            };

            _mockQuizAttemptRepo.Setup(r => r.GetByIdAsync(attemptId))
                .ReturnsAsync(attempt);

            // Act
            var result = await _quizAttemptService.GetByIdAsync(attemptId);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(attemptId);
            result.Status.Should().Be(attempt.Status);
            result.Score.Should().Be(attempt.Score);

            _mockQuizAttemptRepo.Verify(r => r.GetByIdAsync(attemptId), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_WithNonExistentId_ShouldReturnNull()
        {
            // Arrange
            var attemptId = Guid.NewGuid();
            _mockQuizAttemptRepo.Setup(r => r.GetByIdAsync(attemptId))
                .ReturnsAsync((QuizAttempt?)null);

            // Act
            var result = await _quizAttemptService.GetByIdAsync(attemptId);

            // Assert
            result.Should().BeNull();
            _mockQuizAttemptRepo.Verify(r => r.GetByIdAsync(attemptId), Times.Once);
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllAttempts()
        {
            // Arrange
            var attempts = new List<QuizAttempt>
            {
                new QuizAttempt
                {
                    Id = Guid.NewGuid(),
                    UserId = Guid.NewGuid(),
                    QuizSetId = Guid.NewGuid(),
                    AttemptType = "single",
                    Status = "completed",
                    CreatedAt = DateTime.UtcNow
                },
                new QuizAttempt
                {
                    Id = Guid.NewGuid(),
                    UserId = Guid.NewGuid(),
                    QuizSetId = Guid.NewGuid(),
                    AttemptType = "single",
                    Status = "in_progress",
                    CreatedAt = DateTime.UtcNow
                }
            };

            _mockQuizAttemptRepo.Setup(r => r.GetAllAsync(false))
                .ReturnsAsync(attempts);

            // Act
            var result = await _quizAttemptService.GetAllAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            _mockQuizAttemptRepo.Verify(r => r.GetAllAsync(false), Times.Once);
        }

        [Fact]
        public async Task GetByUserIdAsync_WithValidUserId_ShouldReturnUserAttempts()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var attempts = new List<QuizAttempt>
            {
                new QuizAttempt
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    QuizSetId = Guid.NewGuid(),
                    AttemptType = "single",
                    Status = "completed",
                    CreatedAt = DateTime.UtcNow
                }
            };

            _mockQuizAttemptRepo.Setup(r => r.GetByUserIdAsync(userId, false))
                .ReturnsAsync(attempts);

            // Act
            var result = await _quizAttemptService.GetByUserIdAsync(userId);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result.All(a => a.UserId == userId).Should().BeTrue();
            _mockQuizAttemptRepo.Verify(r => r.GetByUserIdAsync(userId, false), Times.Once);
        }

        [Fact]
        public async Task GetByQuizSetIdAsync_WithValidQuizSetId_ShouldReturnQuizSetAttempts()
        {
            // Arrange
            var quizSetId = Guid.NewGuid();
            var attempts = new List<QuizAttempt>
            {
                new QuizAttempt
                {
                    Id = Guid.NewGuid(),
                    UserId = Guid.NewGuid(),
                    QuizSetId = quizSetId,
                    AttemptType = "single",
                    Status = "completed",
                    CreatedAt = DateTime.UtcNow
                }
            };

            _mockQuizAttemptRepo.Setup(r => r.GetByQuizSetIdAsync(quizSetId, false))
                .ReturnsAsync(attempts);

            // Act
            var result = await _quizAttemptService.GetByQuizSetIdAsync(quizSetId);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result.All(a => a.QuizSetId == quizSetId).Should().BeTrue();
            _mockQuizAttemptRepo.Verify(r => r.GetByQuizSetIdAsync(quizSetId, false), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_WithValidData_ShouldReturnUpdatedResponse()
        {
            // Arrange
            var attemptId = Guid.NewGuid();
            var requestDto = new RequestQuizAttemptDto
            {
                UserId = Guid.NewGuid(),
                QuizSetId = Guid.NewGuid(),
                AttemptType = "single",
                TotalQuestions = 10,
                CorrectAnswers = 8,
                WrongAnswers = 2,
                Score = 80,
                Accuracy = 80,
                Status = "completed"
            };

            var updatedAttempt = new QuizAttempt
            {
                Id = attemptId,
                UserId = requestDto.UserId,
                QuizSetId = requestDto.QuizSetId,
                AttemptType = requestDto.AttemptType,
                TotalQuestions = requestDto.TotalQuestions,
                CorrectAnswers = requestDto.CorrectAnswers,
                WrongAnswers = requestDto.WrongAnswers,
                Score = requestDto.Score,
                Accuracy = requestDto.Accuracy,
                Status = requestDto.Status,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _mockQuizAttemptRepo.Setup(r => r.UpdateAsync(attemptId, It.IsAny<QuizAttempt>()))
                .ReturnsAsync(updatedAttempt);

            // Act
            var result = await _quizAttemptService.UpdateAsync(attemptId, requestDto);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(attemptId);
            result.Status.Should().Be(requestDto.Status);
            result.Score.Should().Be(requestDto.Score);

            _mockQuizAttemptRepo.Verify(r => r.UpdateAsync(attemptId, It.IsAny<QuizAttempt>()), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_WithNonExistentId_ShouldReturnNull()
        {
            // Arrange
            var attemptId = Guid.NewGuid();
            var requestDto = new RequestQuizAttemptDto
            {
                UserId = Guid.NewGuid(),
                QuizSetId = Guid.NewGuid(),
                AttemptType = "single",
                TotalQuestions = 10,
                Status = "completed"
            };

            _mockQuizAttemptRepo.Setup(r => r.UpdateAsync(attemptId, It.IsAny<QuizAttempt>()))
                .ReturnsAsync((QuizAttempt?)null);

            // Act
            var result = await _quizAttemptService.UpdateAsync(attemptId, requestDto);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task SoftDeleteAsync_WithValidId_ShouldReturnTrue()
        {
            // Arrange
            var attemptId = Guid.NewGuid();
            _mockQuizAttemptRepo.Setup(r => r.SoftDeleteAsync(attemptId))
                .ReturnsAsync(true);

            // Act
            var result = await _quizAttemptService.SoftDeleteAsync(attemptId);

            // Assert
            result.Should().BeTrue();
            _mockQuizAttemptRepo.Verify(r => r.SoftDeleteAsync(attemptId), Times.Once);
        }

        [Fact]
        public async Task RestoreAsync_WithValidId_ShouldReturnTrue()
        {
            // Arrange
            var attemptId = Guid.NewGuid();
            _mockQuizAttemptRepo.Setup(r => r.RestoreAsync(attemptId))
                .ReturnsAsync(true);

            // Act
            var result = await _quizAttemptService.RestoreAsync(attemptId);

            // Assert
            result.Should().BeTrue();
            _mockQuizAttemptRepo.Verify(r => r.RestoreAsync(attemptId), Times.Once);
        }

        [Fact]
        public async Task StartSingleAsync_WithValidData_ShouldReturnResponseSingleStartDto()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var quizSetId = Guid.NewGuid();
            var requestDto = new RequestSingleStartDto
            {
                UserId = userId,
                QuizSetId = quizSetId
            };

            var quizSet = new QuizSet
            {
                Id = quizSetId,
                Title = "Test Quiz Set",
                QuizSetType = QuizSetTypeEnum.Practice,
                CreatedAt = DateTime.UtcNow
            };

            var quizzes = new List<Quiz>
            {
                new Quiz
                {
                    Id = Guid.NewGuid(),
                    QuestionText = "Question 1",
                    TOEICPart = "PART1",
                    IsActive = true,
                    OrderIndex = 1,
                    CreatedAt = DateTime.UtcNow
                }
            };

            _mockQuizSetRepo.Setup(r => r.GetQuizSetByIdAsync(quizSetId))
                .ReturnsAsync(quizSet);
            _mockUserMistakeRepo.Setup(r => r.GetAlByUserIdAsync(userId))
                .ReturnsAsync(new List<UserMistake>());
            _mockTournamentQuizSetRepo.Setup(r => r.GetActiveByQuizSetIdAsync(quizSetId))
                .ReturnsAsync(new List<TournamentQuizSet>());
            _mockQuizRepo.Setup(r => r.GetQuizzesByQuizSetIdAsync(quizSetId))
                .ReturnsAsync(quizzes);
            _mockQuizAttemptRepo.Setup(r => r.CreateAsync(It.IsAny<QuizAttempt>()))
                .ReturnsAsync((QuizAttempt a) => new QuizAttempt
                {
                    Id = Guid.NewGuid(),
                    UserId = a.UserId,
                    QuizSetId = a.QuizSetId,
                    AttemptType = a.AttemptType,
                    TotalQuestions = a.TotalQuestions,
                    Status = a.Status,
                    CreatedAt = DateTime.UtcNow
                });

            // Act
            var result = await _quizAttemptService.StartSingleAsync(requestDto);

            // Assert
            result.Should().NotBeNull();
            result.AttemptId.Should().NotBeEmpty();
            result.TotalQuestions.Should().Be(1);
            result.Questions.Should().NotBeNull();
            result.Questions.Should().HaveCount(1);

            _mockQuizSetRepo.Verify(r => r.GetQuizSetByIdAsync(quizSetId), Times.Once);
            _mockQuizAttemptRepo.Verify(r => r.CreateAsync(It.Is<QuizAttempt>(a =>
                a.UserId == userId &&
                a.QuizSetId == quizSetId &&
                a.AttemptType == "single" &&
                a.Status == "in_progress")), Times.Once);
        }

        [Fact]
        public async Task StartSingleAsync_WithNonExistentQuizSet_ShouldThrowException()
        {
            // Arrange
            var requestDto = new RequestSingleStartDto
            {
                UserId = Guid.NewGuid(),
                QuizSetId = Guid.NewGuid()
            };

            _mockQuizSetRepo.Setup(r => r.GetQuizSetByIdAsync(requestDto.QuizSetId))
                .ReturnsAsync((QuizSet?)null);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await _quizAttemptService.StartSingleAsync(requestDto));

            _mockQuizAttemptRepo.Verify(r => r.CreateAsync(It.IsAny<QuizAttempt>()), Times.Never);
        }

        [Fact]
        public async Task StartSingleAsync_WithPlacementTestAndUnresolvedMistakes_ShouldThrowException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var quizSetId = Guid.NewGuid();
            var requestDto = new RequestSingleStartDto
            {
                UserId = userId,
                QuizSetId = quizSetId
            };

            var quizSet = new QuizSet
            {
                Id = quizSetId,
                Title = "Placement Test",
                QuizSetType = QuizSetTypeEnum.Placement,
                CreatedAt = DateTime.UtcNow
            };

            var userMistakes = new List<UserMistake>
            {
                new UserMistake
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    QuizId = Guid.NewGuid()
                }
            };

            _mockQuizSetRepo.Setup(r => r.GetQuizSetByIdAsync(quizSetId))
                .ReturnsAsync(quizSet);
            _mockUserMistakeRepo.Setup(r => r.GetAlByUserIdAsync(userId))
                .ReturnsAsync(userMistakes);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await _quizAttemptService.StartSingleAsync(requestDto));

            _mockQuizAttemptRepo.Verify(r => r.CreateAsync(It.IsAny<QuizAttempt>()), Times.Never);
        }

        [Fact]
        public async Task FinishAsync_WithValidId_ShouldReturnCompletedAttempt()
        {
            // Arrange
            var attemptId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var quizSetId = Guid.NewGuid();
            var attempt = new QuizAttempt
            {
                Id = attemptId,
                UserId = userId,
                QuizSetId = quizSetId,
                AttemptType = "single",
                TotalQuestions = 2,
                Status = "in_progress",
                CreatedAt = DateTime.UtcNow
            };

            var quizId1 = Guid.NewGuid();
            var quizId2 = Guid.NewGuid();
            var answerOptionId1 = Guid.NewGuid();
            var answerOptionId2 = Guid.NewGuid();

            var details = new List<QuizAttemptDetail>
            {
                new QuizAttemptDetail
                {
                    Id = Guid.NewGuid(),
                    QuizAttemptId = attemptId,
                    QuestionId = quizId1,
                    UserAnswer = answerOptionId1.ToString(),
                    IsCorrect = false
                },
                new QuizAttemptDetail
                {
                    Id = Guid.NewGuid(),
                    QuizAttemptId = attemptId,
                    QuestionId = quizId2,
                    UserAnswer = answerOptionId2.ToString(),
                    IsCorrect = false
                }
            };

            var answerOption1 = new AnswerOption
            {
                Id = answerOptionId1,
                QuizId = quizId1,
                IsCorrect = true
            };

            var answerOption2 = new AnswerOption
            {
                Id = answerOptionId2,
                QuizId = quizId2,
                IsCorrect = false
            };

            _mockQuizAttemptRepo.Setup(r => r.GetByIdAsync(attemptId))
                .ReturnsAsync(attempt);
            _mockTournamentQuizSetRepo.Setup(r => r.GetActiveByQuizSetIdAsync(quizSetId))
                .ReturnsAsync(new List<TournamentQuizSet>());
            _mockDetailRepo.Setup(r => r.GetByAttemptIdAsync(attemptId, false))
                .ReturnsAsync(details);
            _mockAnswerOptionRepo.Setup(r => r.GetByIdAsync(answerOptionId1))
                .ReturnsAsync(answerOption1);
            _mockAnswerOptionRepo.Setup(r => r.GetByIdAsync(answerOptionId2))
                .ReturnsAsync(answerOption2);
            _mockDetailRepo.Setup(r => r.UpdateAsync(It.IsAny<Guid>(), It.IsAny<QuizAttemptDetail>()))
                .ReturnsAsync((Guid id, QuizAttemptDetail d) => d);
            _mockQuizAttemptRepo.Setup(r => r.UpdateAsync(attemptId, It.IsAny<QuizAttempt>()))
                .ReturnsAsync((Guid id, QuizAttempt a) => a);

            // Act
            var result = await _quizAttemptService.FinishAsync(attemptId);

            // Assert
            result.Should().NotBeNull();
            result!.Status.Should().Be("completed");
            result.CorrectAnswers.Should().Be(1);
            result.WrongAnswers.Should().Be(1);

            _mockQuizAttemptRepo.Verify(r => r.UpdateAsync(attemptId, It.Is<QuizAttempt>(a =>
                a.Status == "completed" &&
                a.CorrectAnswers == 1 &&
                a.WrongAnswers == 1)), Times.Once);
        }

        [Fact]
        public async Task FinishAsync_WithNonExistentId_ShouldReturnNull()
        {
            // Arrange
            var attemptId = Guid.NewGuid();
            _mockQuizAttemptRepo.Setup(r => r.GetByIdAsync(attemptId))
                .ReturnsAsync((QuizAttempt?)null);

            // Act
            var result = await _quizAttemptService.FinishAsync(attemptId);

            // Assert
            result.Should().BeNull();
            _mockQuizAttemptRepo.Verify(r => r.UpdateAsync(It.IsAny<Guid>(), It.IsAny<QuizAttempt>()), Times.Never);
        }

        [Fact]
        public async Task GetPlayerHistoryAsync_WithValidRequest_ShouldReturnHistory()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var request = new PlayerHistoryRequestDto
            {
                UserId = userId,
                Page = 1,
                PageSize = 10,
                SortBy = "CreatedAt",
                SortOrder = "desc"
            };

            var attempts = new List<QuizAttempt>
            {
                new QuizAttempt
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    QuizSetId = Guid.NewGuid(),
                    AttemptType = "single",
                    Status = "completed",
                    Score = 80,
                    Accuracy = 80,
                    CreatedAt = DateTime.UtcNow
                }
            };

            _mockQuizAttemptRepo.Setup(r => r.GetUserHistoryPagedAsync(
                    userId,
                    It.IsAny<Guid?>(),
                    It.IsAny<string?>(),
                    It.IsAny<string?>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    false))
                .ReturnsAsync((attempts.AsEnumerable(), attempts.Count));

            // Act
            var result = await _quizAttemptService.GetPlayerHistoryAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Attempts.Should().NotBeNull();
            result.TotalCount.Should().Be(1);
            result.Page.Should().Be(1);
            result.PageSize.Should().Be(10);
        }

        [Fact]
        public async Task GetPlayerHistoryAsync_WithFilters_ShouldReturnFilteredHistory()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var quizSetId = Guid.NewGuid();
            var request = new PlayerHistoryRequestDto
            {
                UserId = userId,
                QuizSetId = quizSetId,
                Status = "completed",
                AttemptType = "single",
                Page = 1,
                PageSize = 10
            };

            var attempts = new List<QuizAttempt>
            {
                new QuizAttempt
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    QuizSetId = quizSetId,
                    AttemptType = "single",
                    Status = "completed",
                    CreatedAt = DateTime.UtcNow
                }
            };

            _mockQuizAttemptRepo.Setup(r => r.GetUserHistoryPagedAsync(
                    userId,
                    It.IsAny<Guid?>(),
                    It.IsAny<string?>(),
                    It.IsAny<string?>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    false))
                .ReturnsAsync((attempts.AsEnumerable(), attempts.Count));

            // Act
            var result = await _quizAttemptService.GetPlayerHistoryAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Attempts.Should().NotBeNull();
        }

        [Fact]
        public async Task GetPlacementTestHistoryAsync_WithValidRequest_ShouldReturnPlacementHistory()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var request = new PlayerHistoryRequestDto
            {
                UserId = userId,
                Page = 1,
                PageSize = 10
            };

            var attempts = new List<QuizAttempt>
            {
                new QuizAttempt
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    QuizSetId = Guid.NewGuid(),
                    AttemptType = "placement",
                    Status = "completed",
                    CreatedAt = DateTime.UtcNow
                }
            };

            _mockQuizAttemptRepo.Setup(r => r.GetPlacementTestHistoryPagedAsync(
                    userId,
                    It.IsAny<Guid?>(),
                    It.IsAny<string?>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    false))
                .ReturnsAsync((attempts.AsEnumerable(), attempts.Count));

            // Act
            var result = await _quizAttemptService.GetPlacementTestHistoryAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Attempts.Should().NotBeNull();
            result.TotalCount.Should().Be(1);
        }

        [Fact]
        public async Task GetPlayerStatsAsync_WithValidUserId_ShouldReturnStats()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var attempts = new List<QuizAttempt>
            {
                new QuizAttempt
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    QuizSetId = Guid.NewGuid(),
                    TotalQuestions = 10,
                    CorrectAnswers = 8,
                    WrongAnswers = 2,
                    Score = 80,
                    Accuracy = 80,
                    Status = "completed",
                    TimeSpent = 300,
                    CreatedAt = DateTime.UtcNow
                },
                new QuizAttempt
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    QuizSetId = Guid.NewGuid(),
                    TotalQuestions = 10,
                    CorrectAnswers = 7,
                    WrongAnswers = 3,
                    Score = 70,
                    Accuracy = 70,
                    Status = "completed",
                    TimeSpent = 250,
                    CreatedAt = DateTime.UtcNow.AddDays(-1)
                },
                new QuizAttempt
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    QuizSetId = Guid.NewGuid(),
                    Status = "in_progress",
                    CreatedAt = DateTime.UtcNow
                }
            };

            _mockQuizAttemptRepo.Setup(r => r.GetByUserIdAsync(userId, false))
                .ReturnsAsync(attempts);

            // Act
            var result = await _quizAttemptService.GetPlayerStatsAsync(userId);

            // Assert
            result.Should().NotBeNull();
            result.UserId.Should().Be(userId);
            result.TotalAttempts.Should().Be(3);
            result.CompletedAttempts.Should().Be(2);
            result.InProgressAttempts.Should().Be(1);
            result.AverageScore.Should().Be(75);
            result.AverageAccuracy.Should().Be(75);
            result.BestScore.Should().Be(80);
            result.BestAccuracy.Should().Be(80);
            result.TotalQuestionsAnswered.Should().Be(20);
            result.TotalCorrectAnswers.Should().Be(15);
        }

        [Fact]
        public async Task GetPlayerStatsAsync_WithNoAttempts_ShouldReturnZeroStats()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var attempts = new List<QuizAttempt>();

            _mockQuizAttemptRepo.Setup(r => r.GetByUserIdAsync(userId, false))
                .ReturnsAsync(attempts);

            // Act
            var result = await _quizAttemptService.GetPlayerStatsAsync(userId);

            // Assert
            result.Should().NotBeNull();
            result.UserId.Should().Be(userId);
            result.TotalAttempts.Should().Be(0);
            result.CompletedAttempts.Should().Be(0);
            result.AverageScore.Should().Be(0);
            result.BestScore.Should().Be(0);
        }
    }
}

