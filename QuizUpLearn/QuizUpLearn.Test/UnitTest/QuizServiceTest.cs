using AutoMapper;
using BusinessLogic.DTOs;
using BusinessLogic.DTOs.QuizDtos;
using BusinessLogic.MappingProfile;
using BusinessLogic.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Repository.Entities;
using Repository.Interfaces;

namespace QuizUpLearn.Test.UnitTest
{
    public class QuizServiceTest : BaseServiceTest
    {
        private readonly Mock<IQuizRepo> _mockQuizRepo;
        private readonly IMapper _mapper;
        private readonly QuizService _quizService;

        public QuizServiceTest()
        {
            _mockQuizRepo = new Mock<IQuizRepo>();

            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<MappingProfile>();
            }, new NullLoggerFactory());
            _mapper = mapperConfig.CreateMapper();
            
            _quizService = new QuizService(_mockQuizRepo.Object, _mapper);
        }

        [Fact]
        public async Task CreateQuizAsync_WithValidQuizRequest_ShouldReturnQuizResponse()
        {
            Guid quizId = Guid.NewGuid();
            var quizRequestDto = new QuizRequestDto
            {
                QuizGroupItemId = Guid.NewGuid(),
                VocabularyId = Guid.NewGuid(),
                GrammarId = Guid.NewGuid(),
                QuestionText = "What is the correct answer?",
                TOEICPart = "PART1",
                IsActive = true,
                IsAIGenerated = false,
                DifficultyLevel = "easy",
                CorrectAnswer = "A",
                AudioURL = "https://example.com/audio.mp3",
                ImageURL = "https://example.com/image.jpg",
                OrderIndex = 1,
                AnswerOptions = new List<BusinessLogic.DTOs.RequestAnswerOptionDto>
                {
                    new BusinessLogic.DTOs.RequestAnswerOptionDto
                    {
                        QuizId = quizId,
                        OptionLabel = "A",
                        OptionText = "Option A",
                        OrderIndex = 0,
                        IsCorrect = true
                    },
                    new BusinessLogic.DTOs.RequestAnswerOptionDto
                    {
                        QuizId = quizId,
                        OptionLabel = "B",
                        OptionText = "Option B",
                        OrderIndex = 1,
                        IsCorrect = false
                    }
                }
            };  

            var createdQuiz = new Quiz
            {
                Id = quizId,
                QuizGroupItemId = quizRequestDto.QuizGroupItemId,
                VocabularyId = quizRequestDto.VocabularyId,
                GrammarId = quizRequestDto.GrammarId,
                QuestionText = quizRequestDto.QuestionText!,
                TOEICPart = quizRequestDto.TOEICPart!,
                IsActive = quizRequestDto.IsActive,
                IsAIGenerated = quizRequestDto.IsAIGenerated,
                DifficultyLevel = quizRequestDto.DifficultyLevel,
                CorrectAnswer = quizRequestDto.CorrectAnswer,
                AudioURL = quizRequestDto.AudioURL,
                ImageURL = quizRequestDto.ImageURL,
                OrderIndex = quizRequestDto.OrderIndex,
                TimesAnswered = 0,
                TimesCorrect = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _mockQuizRepo.Setup(r => r.CreateQuizAsync(It.IsAny<Quiz>()))
                .ReturnsAsync(createdQuiz);

            var result = await _quizService.CreateQuizAsync(quizRequestDto);

            result.Should().NotBeNull();
            result.Id.Should().Be(createdQuiz.Id);
            result.QuestionText.Should().Be(quizRequestDto.QuestionText);
            result.TOEICPart.Should().Be(quizRequestDto.TOEICPart);
            result.IsActive.Should().Be(quizRequestDto.IsActive);
            result.IsAIGenerated.Should().Be(quizRequestDto.IsAIGenerated);
            result.DifficultyLevel.Should().Be(quizRequestDto.DifficultyLevel);
            result.CorrectAnswer.Should().Be(quizRequestDto.CorrectAnswer);
            result.AudioURL.Should().Be(quizRequestDto.AudioURL);
            result.ImageURL.Should().Be(quizRequestDto.ImageURL);
            result.OrderIndex.Should().Be(quizRequestDto.OrderIndex);
            result.QuizGroupItemId.Should().Be(quizRequestDto.QuizGroupItemId);
            result.VocabularyId.Should().Be(quizRequestDto.VocabularyId);
            result.GrammarId.Should().Be(quizRequestDto.GrammarId);
            result.CreatedAt.Should().Be(createdQuiz.CreatedAt);

            _mockQuizRepo.Verify(r => r.CreateQuizAsync(It.Is<Quiz>(q => 
                q.QuestionText == quizRequestDto.QuestionText &&
                q.TOEICPart == quizRequestDto.TOEICPart &&
                q.IsActive == quizRequestDto.IsActive &&
                q.DifficultyLevel == quizRequestDto.DifficultyLevel)), Times.Once);
        }

        [Fact]
        public async Task CreateQuizAsync_WithMinimalRequiredFields_ShouldReturnQuizResponse()
        {
            var quizRequestDto = new QuizRequestDto
            {
                QuestionText = "Simple question?",
                TOEICPart = "PART5",
                DifficultyLevel = "medium",
                IsActive = true,
                IsAIGenerated = true,
                AnswerOptions = new List<BusinessLogic.DTOs.RequestAnswerOptionDto>()
            };

            var createdQuiz = new Quiz
            {
                Id = Guid.NewGuid(),
                QuestionText = quizRequestDto.QuestionText!,
                TOEICPart = quizRequestDto.TOEICPart!,
                DifficultyLevel = quizRequestDto.DifficultyLevel,
                IsActive = quizRequestDto.IsActive,
                IsAIGenerated = quizRequestDto.IsAIGenerated,
                TimesAnswered = 0,
                TimesCorrect = 0,
                CreatedAt = DateTime.UtcNow
            };

            _mockQuizRepo.Setup(r => r.CreateQuizAsync(It.IsAny<Quiz>()))
                .ReturnsAsync(createdQuiz);

            var result = await _quizService.CreateQuizAsync(quizRequestDto);

            result.Should().NotBeNull();
            result.Id.Should().Be(createdQuiz.Id);
            result.QuestionText.Should().Be(quizRequestDto.QuestionText);
            result.TOEICPart.Should().Be(quizRequestDto.TOEICPart);
            result.DifficultyLevel.Should().Be(quizRequestDto.DifficultyLevel);
            result.IsActive.Should().Be(quizRequestDto.IsActive);
            result.IsAIGenerated.Should().Be(quizRequestDto.IsAIGenerated);

            _mockQuizRepo.Verify(r => r.CreateQuizAsync(It.IsAny<Quiz>()), Times.Once);
        }

        [Fact]
        public async Task GetQuizByIdAsync_WithValidId_ShouldReturnQuizResponse()
        {
            var quizId = Guid.NewGuid();
            var quiz = new Quiz
            {
                Id = quizId,
                QuestionText = "Test Question",
                TOEICPart = "PART1",
                IsActive = true,
                IsAIGenerated = true,
                DifficultyLevel = "easy",
                TimesAnswered = 5,
                TimesCorrect = 3,
                CreatedAt = DateTime.UtcNow
            };

            _mockQuizRepo.Setup(r => r.GetQuizByIdAsync(quizId))
                .ReturnsAsync(quiz);

            var result = await _quizService.GetQuizByIdAsync(quizId);

            result.Should().NotBeNull();
            result.Id.Should().Be(quizId);
            result.QuestionText.Should().Be(quiz.QuestionText);
            result.TOEICPart.Should().Be(quiz.TOEICPart);
            result.IsActive.Should().Be(quiz.IsActive);
            result.TimesAnswered.Should().Be(quiz.TimesAnswered);
            result.TimesCorrect.Should().Be(quiz.TimesCorrect);

            _mockQuizRepo.Verify(r => r.GetQuizByIdAsync(quizId), Times.Once);
        }

        [Fact]
        public async Task GetQuizByIdAsync_WithNonExistentId_ShouldReturnNull()
        {
            var quizId = Guid.NewGuid();
            _mockQuizRepo.Setup(r => r.GetQuizByIdAsync(quizId))
                .ReturnsAsync((Quiz?) null);

            var result = await _quizService.GetQuizByIdAsync(quizId);

            result.Should().BeNull();
            _mockQuizRepo.Verify(r => r.GetQuizByIdAsync(quizId), Times.Once);
        }

        [Fact]
        public async Task UpdateQuizAsync_WithValidData_ShouldReturnUpdatedQuizResponse()
        {
            // Arrange
            var quizId = Guid.NewGuid();
            var quizRequestDto = new QuizRequestDto
            {
                QuestionText = "Updated Question",
                TOEICPart = "PART2",
                DifficultyLevel = "hard",
                IsActive = false,
                IsAIGenerated = false,
                AnswerOptions = new List<BusinessLogic.DTOs.RequestAnswerOptionDto>()
            };

            var updatedQuiz = new Quiz
            {
                Id = quizId,
                QuestionText = quizRequestDto.QuestionText!,
                TOEICPart = quizRequestDto.TOEICPart!,
                DifficultyLevel = quizRequestDto.DifficultyLevel,
                IsActive = quizRequestDto.IsActive,
                IsAIGenerated = quizRequestDto.IsAIGenerated,
                TimesAnswered = 10,
                TimesCorrect = 7,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow
            };

            _mockQuizRepo.Setup(r => r.UpdateQuizAsync(quizId, It.IsAny<Quiz>()))
                .ReturnsAsync(updatedQuiz);

            // Act
            var result = await _quizService.UpdateQuizAsync(quizId, quizRequestDto);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(quizId);
            result.QuestionText.Should().Be(quizRequestDto.QuestionText);
            result.TOEICPart.Should().Be(quizRequestDto.TOEICPart);
            result.DifficultyLevel.Should().Be(quizRequestDto.DifficultyLevel);
            result.IsActive.Should().Be(quizRequestDto.IsActive);

            _mockQuizRepo.Verify(r => r.UpdateQuizAsync(quizId, It.IsAny<Quiz>()), Times.Once);
        }

        [Fact]
        public async Task SoftDeleteQuizAsync_WithValidId_ShouldReturnTrue()
        {
            // Arrange
            var quizId = Guid.NewGuid();
            _mockQuizRepo.Setup(r => r.SoftDeleteQuizAsync(quizId))
                .ReturnsAsync(true);

            // Act
            var result = await _quizService.SoftDeleteQuizAsync(quizId);

            // Assert
            result.Should().BeTrue();
            _mockQuizRepo.Verify(r => r.SoftDeleteQuizAsync(quizId), Times.Once);
        }

        [Fact]
        public async Task HardDeleteQuizAsync_WithValidId_ShouldReturnTrue()
        {
            // Arrange
            var quizId = Guid.NewGuid();
            _mockQuizRepo.Setup(r => r.HardDeleteQuizAsync(quizId))
                .ReturnsAsync(true);

            // Act
            var result = await _quizService.HardDeleteQuizAsync(quizId);

            // Assert
            result.Should().BeTrue();
            _mockQuizRepo.Verify(r => r.HardDeleteQuizAsync(quizId), Times.Once);
        }

        [Fact]
        public async Task RestoreQuizAsync_WithValidId_ShouldReturnTrue()
        {
            // Arrange
            var quizId = Guid.NewGuid();
            _mockQuizRepo.Setup(r => r.RestoreQuizAsync(quizId))
                .ReturnsAsync(true);

            // Act
            var result = await _quizService.RestoreQuizAsync(quizId);

            // Assert
            result.Should().BeTrue();
            _mockQuizRepo.Verify(r => r.RestoreQuizAsync(quizId), Times.Once);
        }

        [Fact]
        public async Task GetByGrammarIdAndVocabularyIdAsync_WithValidIds_ShouldReturnQuizResponses()
        {
            // Arrange
            var grammarId = Guid.NewGuid();
            var vocabularyId = Guid.NewGuid();
            var quizzes = new List<Quiz>
            {
                new Quiz
                {
                    Id = Guid.NewGuid(),
                    QuestionText = "Grammar Question",
                    TOEICPart = "PART5",
                    GrammarId = grammarId,
                    VocabularyId = vocabularyId,
                    CreatedAt = DateTime.UtcNow
                },
                new Quiz
                {
                    Id = Guid.NewGuid(),
                    QuestionText = "Another Grammar Question",
                    TOEICPart = "PART6",
                    GrammarId = grammarId,
                    VocabularyId = vocabularyId,
                    CreatedAt = DateTime.UtcNow
                }
            };

            _mockQuizRepo.Setup(r => r.GetByGrammarIdAndVocabularyId(grammarId, vocabularyId))
                .ReturnsAsync(quizzes);

            // Act
            var result = await _quizService.GetByGrammarIdAndVocabularyIdAsync(grammarId, vocabularyId);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.All(q => q.GrammarId == grammarId && q.VocabularyId == vocabularyId).Should().BeTrue();

            _mockQuizRepo.Verify(r => r.GetByGrammarIdAndVocabularyId(grammarId, vocabularyId), Times.Once);
        }

        [Fact]
        public async Task CreateQuizAsync_WithInvalidQuizRequest_ShouldThrowArgumentException()
        {
            var invalidQuizRequestDto = new QuizRequestDto
            {
                QuestionText = null,
                TOEICPart = null,
                DifficultyLevel = "medium",
                IsActive = true,
                IsAIGenerated = true,
                AnswerOptions = new List<RequestAnswerOptionDto>()
            };

            await Assert.ThrowsAsync<ArgumentException>(() => _quizService.CreateQuizAsync(invalidQuizRequestDto));
        }

        [Fact]
        public async Task GetQuizByIdAsync_WithInvalidId_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            var invalidQuizId = Guid.NewGuid();
            _mockQuizRepo.Setup(r => r.GetQuizByIdAsync(invalidQuizId))
                .ThrowsAsync(new KeyNotFoundException("Quiz not found"));

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() => _quizService.GetQuizByIdAsync(invalidQuizId));
        }

        [Fact]
        public async Task UpdateQuizAsync_WithNonExistentQuiz_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            var quizId = Guid.NewGuid();
            var quizRequestDto = new QuizRequestDto
            {
                QuestionText = "Updated Question",
                TOEICPart = "PART2",
                DifficultyLevel = "hard",
                IsActive = false,
                IsAIGenerated = false,
                AnswerOptions = new List<RequestAnswerOptionDto>()
            };

            _mockQuizRepo.Setup(r => r.UpdateQuizAsync(quizId, It.IsAny<Quiz>()))
                .ThrowsAsync(new KeyNotFoundException("Quiz not found"));

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() => _quizService.UpdateQuizAsync(quizId, quizRequestDto));
        }

        [Fact]
        public async Task SoftDeleteQuizAsync_WithInvalidId_ShouldReturnFalse()
        {
            // Arrange
            var invalidQuizId = Guid.NewGuid();
            _mockQuizRepo.Setup(r => r.SoftDeleteQuizAsync(invalidQuizId))
                .ReturnsAsync(false);

            // Act
            var result = await _quizService.SoftDeleteQuizAsync(invalidQuizId);

            // Assert
            result.Should().BeFalse();
            _mockQuizRepo.Verify(r => r.SoftDeleteQuizAsync(invalidQuizId), Times.Once);
        }

        [Fact]
        public async Task HardDeleteQuizAsync_WithInvalidId_ShouldReturnFalse()
        {
            // Arrange
            var invalidQuizId = Guid.NewGuid();
            _mockQuizRepo.Setup(r => r.HardDeleteQuizAsync(invalidQuizId))
                .ReturnsAsync(false);

            // Act
            var result = await _quizService.HardDeleteQuizAsync(invalidQuizId);

            // Assert
            result.Should().BeFalse();
            _mockQuizRepo.Verify(r => r.HardDeleteQuizAsync(invalidQuizId), Times.Once);
        }

        [Fact]
        public async Task RestoreQuizAsync_WithInvalidId_ShouldReturnFalse()
        {
            // Arrange
            var invalidQuizId = Guid.NewGuid();
            _mockQuizRepo.Setup(r => r.RestoreQuizAsync(invalidQuizId))
                .ReturnsAsync(false);

            // Act
            var result = await _quizService.RestoreQuizAsync(invalidQuizId);

            // Assert
            result.Should().BeFalse();
            _mockQuizRepo.Verify(r => r.RestoreQuizAsync(invalidQuizId), Times.Once);
        }

        [Fact]
        public async Task GetByGrammarIdAndVocabularyIdAsync_WithInvalidIds_ShouldReturnEmptyList()
        {
            // Arrange
            var invalidGrammarId = Guid.NewGuid();
            var invalidVocabularyId = Guid.NewGuid();
            _mockQuizRepo.Setup(r => r.GetByGrammarIdAndVocabularyId(invalidGrammarId, invalidVocabularyId))
                .ReturnsAsync(new List<Quiz>());

            // Act
            var result = await _quizService.GetByGrammarIdAndVocabularyIdAsync(invalidGrammarId, invalidVocabularyId);

            // Assert
            result.Should().BeEmpty();
            _mockQuizRepo.Verify(r => r.GetByGrammarIdAndVocabularyId(invalidGrammarId, invalidVocabularyId), Times.Once);
        }

        [Fact]
        public async Task GetQuizByIdAsync_WithNullId_ShouldThrowArgumentNullException()
        {
            // Arrange
            Guid nullQuizId = Guid.Empty;

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _quizService.GetQuizByIdAsync(nullQuizId));
        }

        [Fact]
        public async Task GetAllQuizzesAsync_WithValidPagination_ShouldReturnPaginatedQuizzes()
        {
            var quizzes = new List<Quiz>
            {
                new Quiz { Id = Guid.NewGuid(), QuestionText = "Question 1", IsAIGenerated = true, TOEICPart = "PART1", CreatedAt = DateTime.UtcNow },
                new Quiz { Id = Guid.NewGuid(), QuestionText = "Question 2", IsAIGenerated = false, TOEICPart = "PART1", CreatedAt = DateTime.UtcNow.AddDays(-1) }
            };

            var paginationRequest = new PaginationRequestDto
            {
                SearchTerm = "Question",
                SortBy = "CreatedAt",
                SortDirection = "desc",
                Filters = new Dictionary<string, object> { { "isAiGenerated", true } }
            };

            _mockQuizRepo.Setup(r => r.GetAllQuizzesAsync())
                .ReturnsAsync(quizzes);

            // Act
            var result = await _quizService.GetAllQuizzesAsync(paginationRequest);

            // Assert
            result.Should().NotBeNull();
            result.Data.Should().HaveCount(1);
            result.Data.First().QuestionText.Should().Be("Question 1");
            _mockQuizRepo.Verify(r => r.GetAllQuizzesAsync(), Times.Once);
        }

        [Fact]
        public async Task GetQuizzesByQuizSetIdAsync_WithValidQuizSetId_ShouldReturnPaginatedQuizzes()
        {
            // Arrange
            var quizSetId = Guid.NewGuid();
            var quizzes = new List<Quiz>
            {
                new Quiz { Id = Guid.NewGuid(), QuestionText = "Quiz 1", TOEICPart = "PART1", CreatedAt = DateTime.UtcNow },
                new Quiz { Id = Guid.NewGuid(), QuestionText = "Quiz 2", TOEICPart = "PART1", CreatedAt = DateTime.UtcNow.AddDays(-1) }
            };

            var paginationRequest = new PaginationRequestDto
            {
                SearchTerm = "Quiz",
                SortBy = "CreatedAt",
                SortDirection = "asc"
            };

            _mockQuizRepo.Setup(r => r.GetQuizzesByQuizSetIdAsync(quizSetId))
                .ReturnsAsync(quizzes);

            // Act
            var result = await _quizService.GetQuizzesByQuizSetIdAsync(quizSetId, paginationRequest);

            // Assert
            result.Should().NotBeNull();
            result.Data.Should().HaveCount(2);
            result.Data.First().QuestionText.Should().Be("Quiz 2");
            _mockQuizRepo.Verify(r => r.GetQuizzesByQuizSetIdAsync(quizSetId), Times.Once);
        }
    }
}
