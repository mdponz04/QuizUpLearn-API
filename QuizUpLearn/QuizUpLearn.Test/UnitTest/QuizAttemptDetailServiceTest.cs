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
using Repository.Interfaces;

namespace QuizUpLearn.Test.UnitTest
{
    public class QuizAttemptDetailServiceTest : BaseServiceTest
    {
        private readonly Mock<IQuizAttemptDetailRepo> _mockDetailRepo;
        private readonly Mock<IQuizAttemptRepo> _mockAttemptRepo;
        private readonly Mock<IAnswerOptionRepo> _mockAnswerOptionRepo;
        private readonly Mock<IQuizRepo> _mockQuizRepo;
        private readonly Mock<IUserMistakeService> _mockUserMistakeService;
        private readonly Mock<IUserMistakeRepo> _mockUserMistakeRepo;
        private readonly Mock<IAIService> _mockAIService;
        private readonly Mock<IServiceScopeFactory> _mockScopeFactory;
        private readonly Mock<IUserRepo> _mockUserRepo;
        private readonly IMapper _mapper;
        private readonly QuizAttemptDetailService _quizAttemptDetailService;

        public QuizAttemptDetailServiceTest()
        {
            _mockDetailRepo = new Mock<IQuizAttemptDetailRepo>();
            _mockAttemptRepo = new Mock<IQuizAttemptRepo>();
            _mockAnswerOptionRepo = new Mock<IAnswerOptionRepo>();
            _mockQuizRepo = new Mock<IQuizRepo>();
            _mockUserMistakeService = new Mock<IUserMistakeService>();
            _mockUserMistakeRepo = new Mock<IUserMistakeRepo>();
            _mockAIService = new Mock<IAIService>();
            _mockScopeFactory = new Mock<IServiceScopeFactory>();
            _mockUserRepo = new Mock<IUserRepo>();

            // Setup real AutoMapper with the actual mapping profile
            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<MappingProfile>();
            }, new NullLoggerFactory());
            _mapper = mapperConfig.CreateMapper();

            _quizAttemptDetailService = new QuizAttemptDetailService(
                _mockDetailRepo.Object,
                _mockAttemptRepo.Object,
                _mockAnswerOptionRepo.Object,
                _mockQuizRepo.Object,
                _mockUserMistakeService.Object,
                _mockUserMistakeRepo.Object,
                _mockAIService.Object,
                _mapper,
                _mockScopeFactory.Object,
                _mockUserRepo.Object);
        }

        [Fact]
        public async Task CreateAsync_WithValidData_ShouldReturnResponseQuizAttemptDetailDto()
        {
            // Arrange
            var requestDto = new RequestQuizAttemptDetailDto
            {
                AttemptId = Guid.NewGuid(),
                QuestionId = Guid.NewGuid(),
                UserAnswer = Guid.NewGuid().ToString(),
                IsCorrect = true,
                TimeSpent = 30
            };

            var createdDetail = new QuizAttemptDetail
            {
                Id = Guid.NewGuid(),
                AttemptId = requestDto.AttemptId,
                QuestionId = requestDto.QuestionId,
                UserAnswer = requestDto.UserAnswer,
                IsCorrect = requestDto.IsCorrect,
                TimeSpent = requestDto.TimeSpent,
                CreatedAt = DateTime.UtcNow
            };

            _mockDetailRepo.Setup(r => r.CreateAsync(It.IsAny<QuizAttemptDetail>()))
                .ReturnsAsync(createdDetail);

            // Act
            var result = await _quizAttemptDetailService.CreateAsync(requestDto);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(createdDetail.Id);
            result.AttemptId.Should().Be(requestDto.AttemptId);
            result.QuestionId.Should().Be(requestDto.QuestionId);
            result.UserAnswer.Should().Be(requestDto.UserAnswer);

            _mockDetailRepo.Verify(r => r.CreateAsync(It.Is<QuizAttemptDetail>(d =>
                d.AttemptId == requestDto.AttemptId &&
                d.QuestionId == requestDto.QuestionId)), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_WithAutoCalculateIsCorrect_ShouldSetIsCorrectFromAnswerOption()
        {
            // Arrange
            var answerOptionId = Guid.NewGuid();
            var questionId = Guid.NewGuid();
            var requestDto = new RequestQuizAttemptDetailDto
            {
                AttemptId = Guid.NewGuid(),
                QuestionId = questionId,
                UserAnswer = answerOptionId.ToString(),
                IsCorrect = null, // Not set, should be calculated
                TimeSpent = 30
            };

            var answerOption = new AnswerOption
            {
                Id = answerOptionId,
                QuizId = questionId,
                IsCorrect = true
            };

            var createdDetail = new QuizAttemptDetail
            {
                Id = Guid.NewGuid(),
                AttemptId = requestDto.AttemptId,
                QuestionId = requestDto.QuestionId,
                UserAnswer = requestDto.UserAnswer,
                IsCorrect = true,
                TimeSpent = requestDto.TimeSpent,
                CreatedAt = DateTime.UtcNow
            };

            _mockAnswerOptionRepo.Setup(r => r.GetByIdAsync(answerOptionId))
                .ReturnsAsync(answerOption);
            _mockDetailRepo.Setup(r => r.CreateAsync(It.IsAny<QuizAttemptDetail>()))
                .ReturnsAsync(createdDetail);

            // Act
            var result = await _quizAttemptDetailService.CreateAsync(requestDto);

            // Assert
            result.Should().NotBeNull();
            result.IsCorrect.Should().BeTrue();
            _mockAnswerOptionRepo.Verify(r => r.GetByIdAsync(answerOptionId), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_WithValidId_ShouldReturnResponseQuizAttemptDetailDto()
        {
            // Arrange
            var detailId = Guid.NewGuid();
            var detail = new QuizAttemptDetail
            {
                Id = detailId,
                AttemptId = Guid.NewGuid(),
                QuestionId = Guid.NewGuid(),
                UserAnswer = "answer",
                IsCorrect = true,
                CreatedAt = DateTime.UtcNow
            };

            _mockDetailRepo.Setup(r => r.GetByIdAsync(detailId))
                .ReturnsAsync(detail);

            // Act
            var result = await _quizAttemptDetailService.GetByIdAsync(detailId);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(detailId);
            result.IsCorrect.Should().BeTrue();

            _mockDetailRepo.Verify(r => r.GetByIdAsync(detailId), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_WithNonExistentId_ShouldReturnNull()
        {
            // Arrange
            var detailId = Guid.NewGuid();
            _mockDetailRepo.Setup(r => r.GetByIdAsync(detailId))
                .ReturnsAsync((QuizAttemptDetail?)null);

            // Act
            var result = await _quizAttemptDetailService.GetByIdAsync(detailId);

            // Assert
            result.Should().BeNull();
            _mockDetailRepo.Verify(r => r.GetByIdAsync(detailId), Times.Once);
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllDetails()
        {
            // Arrange
            var details = new List<QuizAttemptDetail>
            {
                new QuizAttemptDetail
                {
                    Id = Guid.NewGuid(),
                    AttemptId = Guid.NewGuid(),
                    QuestionId = Guid.NewGuid(),
                    UserAnswer = "answer1",
                    IsCorrect = true,
                    CreatedAt = DateTime.UtcNow
                },
                new QuizAttemptDetail
                {
                    Id = Guid.NewGuid(),
                    AttemptId = Guid.NewGuid(),
                    QuestionId = Guid.NewGuid(),
                    UserAnswer = "answer2",
                    IsCorrect = false,
                    CreatedAt = DateTime.UtcNow
                }
            };

            _mockDetailRepo.Setup(r => r.GetAllAsync(false))
                .ReturnsAsync(details);

            // Act
            var result = await _quizAttemptDetailService.GetAllAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            _mockDetailRepo.Verify(r => r.GetAllAsync(false), Times.Once);
        }

        [Fact]
        public async Task GetByAttemptIdAsync_WithValidAttemptId_ShouldReturnDetails()
        {
            // Arrange
            var attemptId = Guid.NewGuid();
            var details = new List<QuizAttemptDetail>
            {
                new QuizAttemptDetail
                {
                    Id = Guid.NewGuid(),
                    AttemptId = attemptId,
                    QuestionId = Guid.NewGuid(),
                    UserAnswer = "answer1",
                    IsCorrect = true,
                    CreatedAt = DateTime.UtcNow
                }
            };

            _mockDetailRepo.Setup(r => r.GetByAttemptIdAsync(attemptId, false))
                .ReturnsAsync(details);

            // Act
            var result = await _quizAttemptDetailService.GetByAttemptIdAsync(attemptId);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result.All(d => d.AttemptId == attemptId).Should().BeTrue();
            _mockDetailRepo.Verify(r => r.GetByAttemptIdAsync(attemptId, false), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_WithValidData_ShouldReturnUpdatedResponse()
        {
            // Arrange
            var detailId = Guid.NewGuid();
            var requestDto = new RequestQuizAttemptDetailDto
            {
                AttemptId = Guid.NewGuid(),
                QuestionId = Guid.NewGuid(),
                UserAnswer = "updated_answer",
                IsCorrect = false,
                TimeSpent = 45
            };

            var updatedDetail = new QuizAttemptDetail
            {
                Id = detailId,
                AttemptId = requestDto.AttemptId,
                QuestionId = requestDto.QuestionId,
                UserAnswer = requestDto.UserAnswer,
                IsCorrect = requestDto.IsCorrect,
                TimeSpent = requestDto.TimeSpent,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _mockDetailRepo.Setup(r => r.UpdateAsync(detailId, It.IsAny<QuizAttemptDetail>()))
                .ReturnsAsync(updatedDetail);

            // Act
            var result = await _quizAttemptDetailService.UpdateAsync(detailId, requestDto);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(detailId);
            result.UserAnswer.Should().Be(requestDto.UserAnswer);
            result.IsCorrect.Should().Be(requestDto.IsCorrect);

            _mockDetailRepo.Verify(r => r.UpdateAsync(detailId, It.IsAny<QuizAttemptDetail>()), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_WithNonExistentId_ShouldReturnNull()
        {
            // Arrange
            var detailId = Guid.NewGuid();
            var requestDto = new RequestQuizAttemptDetailDto
            {
                AttemptId = Guid.NewGuid(),
                QuestionId = Guid.NewGuid(),
                UserAnswer = "answer",
                IsCorrect = true
            };

            _mockDetailRepo.Setup(r => r.UpdateAsync(detailId, It.IsAny<QuizAttemptDetail>()))
                .ReturnsAsync((QuizAttemptDetail?)null);

            // Act
            var result = await _quizAttemptDetailService.UpdateAsync(detailId, requestDto);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task SoftDeleteAsync_WithValidId_ShouldReturnTrue()
        {
            // Arrange
            var detailId = Guid.NewGuid();
            _mockDetailRepo.Setup(r => r.SoftDeleteAsync(detailId))
                .ReturnsAsync(true);

            // Act
            var result = await _quizAttemptDetailService.SoftDeleteAsync(detailId);

            // Assert
            result.Should().BeTrue();
            _mockDetailRepo.Verify(r => r.SoftDeleteAsync(detailId), Times.Once);
        }

        [Fact]
        public async Task RestoreAsync_WithValidId_ShouldReturnTrue()
        {
            // Arrange
            var detailId = Guid.NewGuid();
            _mockDetailRepo.Setup(r => r.RestoreAsync(detailId))
                .ReturnsAsync(true);

            // Act
            var result = await _quizAttemptDetailService.RestoreAsync(detailId);

            // Assert
            result.Should().BeTrue();
            _mockDetailRepo.Verify(r => r.RestoreAsync(detailId), Times.Once);
        }

        [Fact]
        public async Task GetPlacementTestByAttemptIdAsync_WithValidPlacementAttempt_ShouldReturnPlacementTestDto()
        {
            // Arrange
            var attemptId = Guid.NewGuid();
            var attempt = new QuizAttempt
            {
                Id = attemptId,
                UserId = Guid.NewGuid(),
                QuizSetId = Guid.NewGuid(),
                AttemptType = "placement",
                TotalQuestions = 2,
                Status = "completed",
                CreatedAt = DateTime.UtcNow
            };

            var quizId1 = Guid.NewGuid();
            var quizId2 = Guid.NewGuid();

            var details = new List<QuizAttemptDetail>
            {
                new QuizAttemptDetail
                {
                    Id = Guid.NewGuid(),
                    AttemptId = attemptId,
                    QuestionId = quizId1,
                    IsCorrect = true,
                    CreatedAt = DateTime.UtcNow
                },
                new QuizAttemptDetail
                {
                    Id = Guid.NewGuid(),
                    AttemptId = attemptId,
                    QuestionId = quizId2,
                    IsCorrect = false,
                    CreatedAt = DateTime.UtcNow
                }
            };

            var quiz1 = new Quiz
            {
                Id = quizId1,
                TOEICPart = "PART1",
                QuestionText = "hi !!!",
                CreatedAt = DateTime.UtcNow
            };

            var quiz2 = new Quiz
            {
                Id = quizId2,
                TOEICPart = "PART5",
                QuestionText= "hello ???",
                CreatedAt = DateTime.UtcNow
            };

            _mockAttemptRepo.Setup(r => r.GetByIdAsync(attemptId))
                .ReturnsAsync(attempt);
            _mockDetailRepo.Setup(r => r.GetByAttemptIdAsync(attemptId, false))
                .ReturnsAsync(details);
            _mockQuizRepo.Setup(r => r.GetQuizByIdAsync(quizId1))
                .ReturnsAsync(quiz1);
            _mockQuizRepo.Setup(r => r.GetQuizByIdAsync(quizId2))
                .ReturnsAsync(quiz2);

            // Act
            var result = await _quizAttemptDetailService.GetPlacementTestByAttemptIdAsync(attemptId);

            // Assert
            result.Should().NotBeNull();
            result.AttemptId.Should().Be(attemptId);
            result.TotalCorrectLisAns.Should().Be(1);
            result.TotalCorrectReaAns.Should().Be(0);
            result.TotalQuestions.Should().Be(2);

            _mockAttemptRepo.Verify(r => r.GetByIdAsync(attemptId), Times.Once);
        }

        [Fact]
        public async Task GetPlacementTestByAttemptIdAsync_WithNonExistentAttempt_ShouldThrowException()
        {
            // Arrange
            var attemptId = Guid.NewGuid();
            _mockAttemptRepo.Setup(r => r.GetByIdAsync(attemptId))
                .ReturnsAsync((QuizAttempt?)null);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await _quizAttemptDetailService.GetPlacementTestByAttemptIdAsync(attemptId));
        }

        [Fact]
        public async Task GetPlacementTestByAttemptIdAsync_WithNonPlacementAttempt_ShouldThrowException()
        {
            // Arrange
            var attemptId = Guid.NewGuid();
            var attempt = new QuizAttempt
            {
                Id = attemptId,
                AttemptType = "single",
                CreatedAt = DateTime.UtcNow
            };

            _mockAttemptRepo.Setup(r => r.GetByIdAsync(attemptId))
                .ReturnsAsync(attempt);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await _quizAttemptDetailService.GetPlacementTestByAttemptIdAsync(attemptId));
        }

        [Fact]
        public async Task SubmitAnswersAsync_WithValidData_ShouldReturnResponseSubmitAnswersDto()
        {
            // Arrange
            var attemptId = Guid.NewGuid();
            var questionId1 = Guid.NewGuid();
            var questionId2 = Guid.NewGuid();
            var answerOptionId1 = Guid.NewGuid();
            var answerOptionId2 = Guid.NewGuid();
            var correctAnswerOptionId2 = Guid.NewGuid();

            var attempt = new QuizAttempt
            {
                Id = attemptId,
                UserId = Guid.NewGuid(),
                QuizSetId = Guid.NewGuid(),
                AttemptType = "single",
                TotalQuestions = 2,
                Status = "in_progress",
                CreatedAt = DateTime.UtcNow
            };

            var requestDto = new RequestSubmitAnswersDto
            {
                AttemptId = attemptId,
                Answers = new List<AnswerDto>
                {
                    new AnswerDto
                    {
                        QuestionId = questionId1,
                        UserAnswer = answerOptionId1.ToString(),
                        TimeSpent = 30
                    },
                    new AnswerDto
                    {
                        QuestionId = questionId2,
                        UserAnswer = answerOptionId2.ToString(),
                        TimeSpent = 25
                    }
                }
            };

            var answerOption1 = new AnswerOption
            {
                Id = answerOptionId1,
                QuizId = questionId1,
                IsCorrect = true
            };

            var answerOption2 = new AnswerOption
            {
                Id = answerOptionId2,
                QuizId = questionId2,
                IsCorrect = false
            };

            var correctAnswerOption2 = new AnswerOption
            {
                Id = correctAnswerOptionId2,
                QuizId = questionId2,
                IsCorrect = true
            };

            _mockAttemptRepo.Setup(r => r.GetByIdAsync(attemptId))
                .ReturnsAsync(attempt);
            _mockAnswerOptionRepo.Setup(r => r.GetByIdAsync(answerOptionId1))
                .ReturnsAsync(answerOption1);
            _mockAnswerOptionRepo.Setup(r => r.GetByIdAsync(answerOptionId2))
                .ReturnsAsync(answerOption2);
            _mockAnswerOptionRepo.Setup(r => r.GetByQuizIdAsync(questionId2,false))
                .ReturnsAsync(new List<AnswerOption> { answerOption2, correctAnswerOption2 });
            _mockDetailRepo.Setup(r => r.CreateAsync(It.IsAny<QuizAttemptDetail>()))
                .ReturnsAsync((QuizAttemptDetail d) => new QuizAttemptDetail
                {
                    Id = Guid.NewGuid(),
                    AttemptId = d.AttemptId,
                    QuestionId = d.QuestionId,
                    UserAnswer = d.UserAnswer,
                    IsCorrect = d.IsCorrect,
                    TimeSpent = d.TimeSpent,
                    CreatedAt = DateTime.UtcNow
                });
            _mockAttemptRepo.Setup(r => r.UpdateAsync(attemptId, It.IsAny<QuizAttempt>()))
                .ReturnsAsync((Guid id, QuizAttempt a) => a);

            // Act
            var result = await _quizAttemptDetailService.SubmitAnswersAsync(requestDto);

            // Assert
            result.Should().NotBeNull();
            result.AttemptId.Should().Be(attemptId);
            result.CorrectAnswers.Should().Be(1);
            result.WrongAnswers.Should().Be(1);
            result.TotalQuestions.Should().Be(2);
            result.Status.Should().Be("completed");
            result.AnswerResults.Should().HaveCount(2);

            _mockAttemptRepo.Verify(r => r.UpdateAsync(attemptId, It.Is<QuizAttempt>(a =>
                a.Status == "completed" &&
                a.CorrectAnswers == 1 &&
                a.WrongAnswers == 1)), Times.Once);
        }

        [Fact]
        public async Task SubmitAnswersAsync_WithNonExistentAttempt_ShouldThrowException()
        {
            // Arrange
            var attemptId = Guid.NewGuid();
            var requestDto = new RequestSubmitAnswersDto
            {
                AttemptId = attemptId,
                Answers = new List<AnswerDto>()
            };

            _mockAttemptRepo.Setup(r => r.GetByIdAsync(attemptId))
                .ReturnsAsync((QuizAttempt?)null);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await _quizAttemptDetailService.SubmitAnswersAsync(requestDto));
        }

        [Fact]
        public async Task SubmitMistakeQuizAnswersAsync_WithValidMistakeQuiz_ShouldReturnResponseSubmitAnswersDto()
        {
            // Arrange
            var attemptId = Guid.NewGuid();
            var questionId = Guid.NewGuid();
            var answerOptionId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var attempt = new QuizAttempt
            {
                Id = attemptId,
                UserId = userId,
                QuizSetId = Guid.NewGuid(),
                AttemptType = "mistake_quiz",
                TotalQuestions = 1,
                Status = "in_progress",
                CreatedAt = DateTime.UtcNow
            };

            var requestDto = new RequestSubmitAnswersDto
            {
                AttemptId = attemptId,
                Answers = new List<AnswerDto>
                {
                    new AnswerDto
                    {
                        QuestionId = questionId,
                        UserAnswer = answerOptionId.ToString(),
                        TimeSpent = 30
                    }
                }
            };

            var answerOption = new AnswerOption
            {
                Id = answerOptionId,
                QuizId = questionId,
                IsCorrect = true
            };

            _mockAttemptRepo.Setup(r => r.GetByIdAsync(attemptId))
                .ReturnsAsync(attempt);
            _mockAnswerOptionRepo.Setup(r => r.GetByIdAsync(answerOptionId))
                .ReturnsAsync(answerOption);
            _mockDetailRepo.Setup(r => r.CreateAsync(It.IsAny<QuizAttemptDetail>()))
                .ReturnsAsync((QuizAttemptDetail d) => new QuizAttemptDetail
                {
                    Id = Guid.NewGuid(),
                    AttemptId = d.AttemptId,
                    QuestionId = d.QuestionId,
                    UserAnswer = d.UserAnswer,
                    IsCorrect = d.IsCorrect,
                    TimeSpent = d.TimeSpent,
                    CreatedAt = DateTime.UtcNow
                });
            _mockAttemptRepo.Setup(r => r.UpdateAsync(attemptId, It.IsAny<QuizAttempt>()))
                .ReturnsAsync((Guid id, QuizAttempt a) => a);

            // Setup scope factory for UserMistake deletion using real ServiceCollection
            var mockUserWeakPointService = new Mock<IUserWeakPointService>();
            var services = new ServiceCollection();
            services.AddSingleton(_mockUserMistakeRepo.Object);
            services.AddSingleton(_mockUserMistakeService.Object);
            services.AddSingleton(mockUserWeakPointService.Object);
            var serviceProvider = services.BuildServiceProvider();
            
            var mockScope = new Mock<IServiceScope>();
            mockScope.Setup(s => s.ServiceProvider)
                .Returns(serviceProvider);
            mockScope.Setup(s => s.Dispose())
                .Callback(() => serviceProvider.Dispose());
            
            _mockScopeFactory.Setup(f => f.CreateScope())
                .Returns(mockScope.Object);

            _mockUserMistakeRepo.Setup(r => r.GetByUserIdAndQuizIdAsync(userId, questionId))
                .ReturnsAsync((UserMistake?)null);
            _mockUserMistakeRepo.Setup(r => r.GetByUserWeakPointIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(new List<UserMistake>());

            // Act
            var result = await _quizAttemptDetailService.SubmitMistakeQuizAnswersAsync(requestDto);

            // Assert
            result.Should().NotBeNull();
            result.AttemptId.Should().Be(attemptId);
            result.CorrectAnswers.Should().Be(1);
            result.Status.Should().Be("completed");

            _mockAttemptRepo.Verify(r => r.UpdateAsync(attemptId, It.Is<QuizAttempt>(a =>
                a.Status == "completed" &&
                a.AttemptType == "mistake_quiz")), Times.Once);
        }

        [Fact]
        public async Task SubmitMistakeQuizAnswersAsync_WithNonMistakeQuizAttempt_ShouldThrowException()
        {
            // Arrange
            var attemptId = Guid.NewGuid();
            var attempt = new QuizAttempt
            {
                Id = attemptId,
                AttemptType = "single",
                CreatedAt = DateTime.UtcNow
            };

            var requestDto = new RequestSubmitAnswersDto
            {
                AttemptId = attemptId,
                Answers = new List<AnswerDto>()
            };

            _mockAttemptRepo.Setup(r => r.GetByIdAsync(attemptId))
                .ReturnsAsync(attempt);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await _quizAttemptDetailService.SubmitMistakeQuizAnswersAsync(requestDto));
        }
    }
}

