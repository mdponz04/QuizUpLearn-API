using AutoMapper;
using BusinessLogic.DTOs;
using BusinessLogic.MappingProfile;
using BusinessLogic.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Repository.Entities;
using Repository.Interfaces;

namespace QuizUpLearn.Test.UnitTest
{
    public class AnswerOptionServiceTest : BaseServiceTest
    {
        private readonly Mock<IAnswerOptionRepo> _mockAnswerOptionRepo;
        private readonly IMapper _mapper;
        private readonly AnswerOptionService _answerOptionService;

        public AnswerOptionServiceTest()
        {
            _mockAnswerOptionRepo = new Mock<IAnswerOptionRepo>();

            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<MappingProfile>();
            }, new NullLoggerFactory());
            _mapper = mapperConfig.CreateMapper();

            _answerOptionService = new AnswerOptionService(_mockAnswerOptionRepo.Object, _mapper);
        }

        [Fact]
        public async Task CreateAsync_WithValidRequest_ShouldReturnAnswerOptionResponse()
        {
            // Arrange
            var request = new RequestAnswerOptionDto
            {
                QuizId = Guid.NewGuid(),
                OptionLabel = "A",
                OptionText = "Sample answer option",
                OrderIndex = 1,
                IsCorrect = true
            };

            var createdAnswerOption = new AnswerOption
            {
                Id = Guid.NewGuid(),
                QuizId = request.QuizId,
                OptionLabel = request.OptionLabel,
                OptionText = request.OptionText,
                OrderIndex = request.OrderIndex,
                IsCorrect = request.IsCorrect,
                CreatedAt = DateTime.UtcNow
            };

            _mockAnswerOptionRepo.Setup(r => r.CreateAsync(It.IsAny<AnswerOption>()))
                .ReturnsAsync(createdAnswerOption);

            // Act
            var result = await _answerOptionService.CreateAnswerOptionAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.QuizId.Should().Be(request.QuizId);
            result.OptionLabel.Should().Be(request.OptionLabel);
            result.OptionText.Should().Be(request.OptionText);
            result.OrderIndex.Should().Be(request.OrderIndex);
            result.IsCorrect.Should().Be(request.IsCorrect);

            _mockAnswerOptionRepo.Verify(r => r.CreateAsync(It.IsAny<AnswerOption>()), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_WithEmptyQuizId_ShouldReturnArgumentException()
        {
            // Arrange
            var request = new RequestAnswerOptionDto
            {
                QuizId = Guid.Empty,
                OptionLabel = "A",
                OptionText = "Sample answer option",
                OrderIndex = 1,
                IsCorrect = true
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
                _answerOptionService.CreateAnswerOptionAsync(request));

            exception.Should().NotBeNull();
            exception.Message.Should().Contain("QuizId");

            // Verify that repository was never called since validation should fail early
            _mockAnswerOptionRepo.Verify(r => r.CreateAsync(It.IsAny<AnswerOption>()), Times.Never);
        }

        [Fact]
        public async Task GetByIdAsync_WithValidId_ShouldReturnAnswerOptionResponse()
        {
            // Arrange
            var answerOptionId = Guid.NewGuid();
            var answerOption = new AnswerOption
            {
                Id = answerOptionId,
                QuizId = Guid.NewGuid(),
                OptionLabel = "B",
                OptionText = "Sample answer option",
                OrderIndex = 2,
                IsCorrect = false,
                CreatedAt = DateTime.UtcNow
            };

            _mockAnswerOptionRepo.Setup(r => r.GetByIdAsync(answerOptionId))
                .ReturnsAsync(answerOption);

            // Act
            var result = await _answerOptionService.GetAnswerOptionByIdAsync(answerOptionId);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(answerOptionId);
            result.QuizId.Should().Be(answerOption.QuizId);
            result.OptionLabel.Should().Be(answerOption.OptionLabel);
            result.OptionText.Should().Be(answerOption.OptionText);
            result.OrderIndex.Should().Be(answerOption.OrderIndex);
            result.IsCorrect.Should().Be(answerOption.IsCorrect);

            _mockAnswerOptionRepo.Verify(r => r.GetByIdAsync(answerOptionId), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_WithNonExistentId_ShouldReturnNull()
        {
            // Arrange
            var answerOptionId = Guid.NewGuid();

            _mockAnswerOptionRepo.Setup(r => r.GetByIdAsync(answerOptionId))
                .ReturnsAsync((AnswerOption?)null);

            // Act
            var result = await _answerOptionService.GetAnswerOptionByIdAsync(answerOptionId);

            // Assert
            result.Should().BeNull();
            _mockAnswerOptionRepo.Verify(r => r.GetByIdAsync(answerOptionId), Times.Once);
        }

        [Fact]
        public async Task GetAllAsync_WithIncludeDeletedFalse_ShouldReturnAnswerOptions()
        {
            // Arrange
            var answerOptions = new List<AnswerOption>
            {
                new AnswerOption 
                { 
                    Id = Guid.NewGuid(), 
                    QuizId = Guid.NewGuid(),
                    OptionLabel = "A",
                    OptionText = "Option 1",
                    OrderIndex = 1,
                    IsCorrect = true,
                    CreatedAt = DateTime.UtcNow
                },
                new AnswerOption 
                { 
                    Id = Guid.NewGuid(), 
                    QuizId = Guid.NewGuid(),
                    OptionLabel = "B",
                    OptionText = "Option 2",
                    OrderIndex = 2,
                    IsCorrect = false,
                    CreatedAt = DateTime.UtcNow
                }
            };

            _mockAnswerOptionRepo.Setup(r => r.GetAllAsync(false))
                .ReturnsAsync(answerOptions);

            // Act
            var result = await _answerOptionService.GetAllAnswerOptionAsync(false);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.First().OptionLabel.Should().Be("A");
            result.Last().OptionLabel.Should().Be("B");

            _mockAnswerOptionRepo.Verify(r => r.GetAllAsync(false), Times.Once);
        }

        [Fact]
        public async Task GetAllAsync_WithIncludeDeletedTrue_ShouldReturnAllAnswerOptions()
        {
            // Arrange
            var answerOptions = new List<AnswerOption>
            {
                new AnswerOption 
                { 
                    Id = Guid.NewGuid(), 
                    QuizId = Guid.NewGuid(),
                    OptionLabel = "A",
                    OptionText = "Active option",
                    OrderIndex = 1,
                    IsCorrect = true,
                    CreatedAt = DateTime.UtcNow
                },
                new AnswerOption 
                { 
                    Id = Guid.NewGuid(), 
                    QuizId = Guid.NewGuid(),
                    OptionLabel = "B",
                    OptionText = "Deleted option",
                    OrderIndex = 2,
                    IsCorrect = false,
                    CreatedAt = DateTime.UtcNow,
                    DeletedAt = DateTime.UtcNow.AddMinutes(-5)
                }
            };

            _mockAnswerOptionRepo.Setup(r => r.GetAllAsync(true))
                .ReturnsAsync(answerOptions);

            // Act
            var result = await _answerOptionService.GetAllAnswerOptionAsync(true);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);

            _mockAnswerOptionRepo.Verify(r => r.GetAllAsync(true), Times.Once);
        }

        [Fact]
        public async Task GetByQuizIdAsync_WithValidQuizId_ShouldReturnAnswerOptionsForQuiz()
        {
            // Arrange
            var quizId = Guid.NewGuid();
            var answerOptions = new List<AnswerOption>
            {
                new AnswerOption 
                { 
                    Id = Guid.NewGuid(), 
                    QuizId = quizId,
                    OptionLabel = "A",
                    OptionText = "Correct answer",
                    OrderIndex = 1,
                    IsCorrect = true,
                    CreatedAt = DateTime.UtcNow
                },
                new AnswerOption 
                { 
                    Id = Guid.NewGuid(), 
                    QuizId = quizId,
                    OptionLabel = "B",
                    OptionText = "Incorrect answer",
                    OrderIndex = 2,
                    IsCorrect = false,
                    CreatedAt = DateTime.UtcNow
                }
            };

            _mockAnswerOptionRepo.Setup(r => r.GetByQuizIdAsync(quizId, false))
                .ReturnsAsync(answerOptions);

            // Act
            var result = await _answerOptionService.GetAnswerOptionByQuizIdAsync(quizId, false);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.All(ao => ao.QuizId == quizId).Should().BeTrue();

            _mockAnswerOptionRepo.Verify(r => r.GetByQuizIdAsync(quizId, false), Times.Once);
        }

        [Fact]
        public async Task GetByQuizIdAsync_WithIncludeDeletedTrue_ShouldReturnAllAnswerOptionsForQuiz()
        {
            // Arrange
            var quizId = Guid.NewGuid();
            var answerOptions = new List<AnswerOption>
            {
                new AnswerOption 
                { 
                    Id = Guid.NewGuid(), 
                    QuizId = quizId,
                    OptionLabel = "A",
                    OptionText = "Active answer",
                    OrderIndex = 1,
                    IsCorrect = true,
                    CreatedAt = DateTime.UtcNow
                },
                new AnswerOption 
                { 
                    Id = Guid.NewGuid(), 
                    QuizId = quizId,
                    OptionLabel = "B",
                    OptionText = "Deleted answer",
                    OrderIndex = 2,
                    IsCorrect = false,
                    CreatedAt = DateTime.UtcNow,
                    DeletedAt = DateTime.UtcNow.AddMinutes(-3)
                }
            };

            _mockAnswerOptionRepo.Setup(r => r.GetByQuizIdAsync(quizId, true))
                .ReturnsAsync(answerOptions);

            // Act
            var result = await _answerOptionService.GetAnswerOptionByQuizIdAsync(quizId, true);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.All(ao => ao.QuizId == quizId).Should().BeTrue();

            _mockAnswerOptionRepo.Verify(r => r.GetByQuizIdAsync(quizId, true), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_WithValidData_ShouldReturnUpdatedAnswerOptionResponse()
        {
            // Arrange
            var answerOptionId = Guid.NewGuid();
            var request = new RequestAnswerOptionDto
            {
                QuizId = Guid.NewGuid(),
                OptionLabel = "C",
                OptionText = "Updated answer option",
                OrderIndex = 3,
                IsCorrect = false
            };

            var updatedAnswerOption = new AnswerOption
            {
                Id = answerOptionId,
                QuizId = request.QuizId,
                OptionLabel = request.OptionLabel,
                OptionText = request.OptionText,
                OrderIndex = request.OrderIndex,
                IsCorrect = request.IsCorrect,
                CreatedAt = DateTime.UtcNow.AddHours(-1),
                UpdatedAt = DateTime.UtcNow
            };

            _mockAnswerOptionRepo.Setup(r => r.UpdateAsync(answerOptionId, It.IsAny<AnswerOption>()))
                .ReturnsAsync(updatedAnswerOption);

            // Act
            var result = await _answerOptionService.UpdateAnswerOptionAsync(answerOptionId, request);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(answerOptionId);
            result.QuizId.Should().Be(request.QuizId);
            result.OptionLabel.Should().Be(request.OptionLabel);
            result.OptionText.Should().Be(request.OptionText);
            result.OrderIndex.Should().Be(request.OrderIndex);
            result.IsCorrect.Should().Be(request.IsCorrect);

            _mockAnswerOptionRepo.Verify(r => r.UpdateAsync(answerOptionId, It.IsAny<AnswerOption>()), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_WithNonExistentId_ShouldReturnNull()
        {
            // Arrange
            var answerOptionId = Guid.NewGuid();
            var request = new RequestAnswerOptionDto
            {
                QuizId = Guid.NewGuid(),
                OptionLabel = "D",
                OptionText = "Non-existent option",
                OrderIndex = 4,
                IsCorrect = true
            };

            _mockAnswerOptionRepo.Setup(r => r.UpdateAsync(answerOptionId, It.IsAny<AnswerOption>()))
                .ReturnsAsync((AnswerOption?)null);

            // Act
            var result = await _answerOptionService.UpdateAnswerOptionAsync(answerOptionId, request);

            // Assert
            result.Should().BeNull();
            _mockAnswerOptionRepo.Verify(r => r.UpdateAsync(answerOptionId, It.IsAny<AnswerOption>()), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_WithValidId_ShouldReturnTrue()
        {
            // Arrange
            var answerOptionId = Guid.NewGuid();
            var newAnswerOption = new AnswerOption
            {
                Id = answerOptionId,
                QuizId = Guid.NewGuid(),
                OptionLabel = "E",
                OptionText = "To be deleted",
                OrderIndex = 5,
                IsCorrect = false,
                CreatedAt = DateTime.UtcNow
            };
            _mockAnswerOptionRepo.Setup(r => r.DeleteAsync(answerOptionId))
                .ReturnsAsync(true);

            // Act
            var result = await _answerOptionService.DeleteAnswerOptionAsync(answerOptionId);
            var answerOption = await _answerOptionService.GetAnswerOptionByIdAsync(answerOptionId);
            // Assert
            result.Should().BeTrue();
            answerOption.Should().BeNull();
            _mockAnswerOptionRepo.Verify(r => r.DeleteAsync(answerOptionId), Times.Once);
        }
        [Fact]
        public async Task DeleteAsync_EmptyId_ShouldThrowArgumentException()
        {
            // Arrange
            var answerOptionId = Guid.Empty;

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
                _answerOptionService.DeleteAnswerOptionAsync(answerOptionId));

            exception.Should().NotBeNull();
            exception.Message.Should().Contain("Id");

            // Verify that repository was never called since validation should fail early
            _mockAnswerOptionRepo.Verify(r => r.DeleteAsync(It.IsAny<Guid>()), Times.Never);
        }
    }
}