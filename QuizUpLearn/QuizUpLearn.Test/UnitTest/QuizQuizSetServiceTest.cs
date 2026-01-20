using AutoMapper;
using BusinessLogic.DTOs;
using BusinessLogic.DTOs.QuizQuizSetDtos;
using BusinessLogic.MappingProfile;
using BusinessLogic.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Repository.Entities;
using Repository.Enums;
using Repository.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace QuizUpLearn.Test.UnitTest
{
    public class QuizQuizSetServiceTest : BaseServiceTest
    {
        private readonly Mock<IQuizQuizSetRepo> _mockQuizQuizSetRepo;
        private readonly Mock<IQuizRepo> _mockQuizRepo;
        private readonly Mock<IQuizSetRepo> _mockQuizSetRepo;
        private readonly IMapper _mapper;
        private readonly QuizQuizSetService _quizQuizSetService;

        public QuizQuizSetServiceTest()
        {
            _mockQuizQuizSetRepo = new Mock<IQuizQuizSetRepo>();
            _mockQuizRepo = new Mock<IQuizRepo>();
            _mockQuizSetRepo = new Mock<IQuizSetRepo>();

            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<MappingProfile>();
            }, new NullLoggerFactory());
            _mapper = mapperConfig.CreateMapper();

            _quizQuizSetService = new QuizQuizSetService(
                _mockQuizQuizSetRepo.Object,
                _mockQuizRepo.Object,
                _mockQuizSetRepo.Object,
                _mapper);
        }

        [Fact]
        public async Task CreateAsync_WithValidRequest_ShouldReturnQuizQuizSetResponse()
        {
            // Arrange
            var quizId = Guid.NewGuid();
            var quizSetId = Guid.NewGuid();
            var request = new RequestQuizQuizSetDto
            {
                QuizId = quizId,
                QuizSetId = quizSetId
            };

            var quiz = new Quiz { Id = quizId, QuestionText = "Sample question" , TOEICPart = "Part1"};
            var quizSet = new QuizSet { Id = quizSetId, Title = "Sample quiz set" };

            var createdQuizQuizSet = new QuizQuizSet
            {
                Id = Guid.NewGuid(),
                QuizId = quizId,
                QuizSetId = quizSetId,
                CreatedAt = DateTime.UtcNow
            };

            _mockQuizRepo.Setup(r => r.GetQuizByIdAsync(quizId))
                .ReturnsAsync(quiz);
            _mockQuizSetRepo.Setup(r => r.GetQuizSetByIdAsync(quizSetId))
                .ReturnsAsync(quizSet);
            _mockQuizQuizSetRepo.Setup(r => r.IsExistedAsync(quizId, quizSetId))
                .ReturnsAsync(false);
            _mockQuizQuizSetRepo.Setup(r => r.CreateAsync(It.IsAny<QuizQuizSet>()))
                .ReturnsAsync(createdQuizQuizSet);

            // Act
            var result = await _quizQuizSetService.CreateQuizQuizSetAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.QuizId.Should().Be(quizId);
            result.QuizSetId.Should().Be(quizSetId);

            _mockQuizRepo.Verify(r => r.GetQuizByIdAsync(quizId), Times.Once);
            _mockQuizSetRepo.Verify(r => r.GetQuizSetByIdAsync(quizSetId), Times.Once);
            _mockQuizQuizSetRepo.Verify(r => r.IsExistedAsync(quizId, quizSetId), Times.Once);
            _mockQuizQuizSetRepo.Verify(r => r.CreateAsync(It.IsAny<QuizQuizSet>()), Times.Once);
        }
        [Fact]
        public async Task CreateAsync_WithNullDto_ShouldThrowArgumentNullException()
        {
            // Arrange
            RequestQuizQuizSetDto? request = null;

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(
                () => _quizQuizSetService.CreateQuizQuizSetAsync(request!));

            exception.ParamName.Should().Be("dto");
            exception.Message.Should().Contain("DTO cannot be null.");
        }

        [Fact]
        public async Task CreateAsync_WithNullQuizId_ShouldThrowArgumentException()
        {
            // Arrange
            var request = new RequestQuizQuizSetDto
            {
                QuizId = null,
                QuizSetId = Guid.NewGuid()
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _quizQuizSetService.CreateQuizQuizSetAsync(request));

            exception.Message.Should().Be("QuizId cannot be null");
        }

        [Fact]
        public async Task CreateAsync_WithNullQuizSetId_ShouldThrowArgumentException()
        {
            // Arrange
            var request = new RequestQuizQuizSetDto
            {
                QuizId = Guid.NewGuid(),
                QuizSetId = null
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _quizQuizSetService.CreateQuizQuizSetAsync(request));

            exception.Message.Should().Be("QuizSetId cannot be null");
        }

        [Fact]
        public async Task CreateAsync_WithNonExistentQuizId_ShouldThrowArgumentException()
        {
            // Arrange
            var quizId = Guid.NewGuid();
            var quizSetId = Guid.NewGuid();
            var request = new RequestQuizQuizSetDto
            {
                QuizId = quizId,
                QuizSetId = quizSetId
            };

            _mockQuizRepo.Setup(r => r.GetQuizByIdAsync(quizId))
                .ReturnsAsync((Quiz?)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _quizQuizSetService.CreateQuizQuizSetAsync(request));

            exception.Message.Should().Be($"Quiz with ID {quizId} not found");
            _mockQuizRepo.Verify(r => r.GetQuizByIdAsync(quizId), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_WithNonExistentQuizSetId_ShouldThrowArgumentException()
        {
            // Arrange
            var quizId = Guid.NewGuid();
            var quizSetId = Guid.NewGuid();
            var request = new RequestQuizQuizSetDto
            {
                QuizId = quizId,
                QuizSetId = quizSetId
            };

            var quiz = new Quiz { Id = quizId, QuestionText = "Sample question", TOEICPart = "Part1" };

            _mockQuizRepo.Setup(r => r.GetQuizByIdAsync(quizId))
                .ReturnsAsync(quiz);
            _mockQuizSetRepo.Setup(r => r.GetQuizSetByIdAsync(quizSetId))
                .ReturnsAsync((QuizSet?)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _quizQuizSetService.CreateQuizQuizSetAsync(request));

            exception.Message.Should().Be($"Quiz set with ID {quizSetId} not found");
            _mockQuizRepo.Verify(r => r.GetQuizByIdAsync(quizId), Times.Once);
            _mockQuizSetRepo.Verify(r => r.GetQuizSetByIdAsync(quizSetId), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_WithValidId_ShouldReturnQuizQuizSetResponse()
        {
            // Arrange
            var id = Guid.NewGuid();
            var quizQuizSet = new QuizQuizSet
            {
                Id = id,
                QuizId = Guid.NewGuid(),
                QuizSetId = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow
            };

            _mockQuizQuizSetRepo.Setup(r => r.GetByIdAsync(id))
                .ReturnsAsync(quizQuizSet);

            // Act
            var result = await _quizQuizSetService.GetQuizQuizSetByIdAsync(id);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(id);
            result.QuizId.Should().Be(quizQuizSet.QuizId);
            result.QuizSetId.Should().Be(quizQuizSet.QuizSetId);

            _mockQuizQuizSetRepo.Verify(r => r.GetByIdAsync(id), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_WithNonExistentId_ShouldReturnNull()
        {
            // Arrange
            var id = Guid.NewGuid();

            _mockQuizQuizSetRepo.Setup(r => r.GetByIdAsync(id))
                .ReturnsAsync((QuizQuizSet?)null);

            // Act
            var result = await _quizQuizSetService.GetQuizQuizSetByIdAsync(id);

            // Assert
            result.Should().BeNull();
            _mockQuizQuizSetRepo.Verify(r => r.GetByIdAsync(id), Times.Once);
        }

        [Fact]
        public async Task GetAllAsync_WithIncludeDeletedFalse_ShouldReturnPaginatedResponse()
        {
            // Arrange
            var pagination = new PaginationRequestDto { Page = 1, PageSize = 10 };
            var quizQuizSets = new List<QuizQuizSet>
            {
                new QuizQuizSet
                {
                    Id = Guid.NewGuid(),
                    QuizId = Guid.NewGuid(),
                    QuizSetId = Guid.NewGuid(),
                    CreatedAt = DateTime.UtcNow
                },
                new QuizQuizSet
                {
                    Id = Guid.NewGuid(),
                    QuizId = Guid.NewGuid(),
                    QuizSetId = Guid.NewGuid(),
                    CreatedAt = DateTime.UtcNow
                }
            };

            _mockQuizQuizSetRepo.Setup(r => r.GetAllAsync(false))
                .ReturnsAsync(quizQuizSets);

            // Act
            var result = await _quizQuizSetService.GetAllQuizQuizSetAsync(pagination, false);

            // Assert
            result.Should().NotBeNull();
            result.Data.Should().HaveCount(2);
            result.Pagination.Should().NotBeNull();

            _mockQuizQuizSetRepo.Verify(r => r.GetAllAsync(false), Times.Once);
        }

        [Fact]
        public async Task GetAllAsync_WithNegativePageNumber_ShouldThrowValidationException()
        {
            // Arrange
            var pagination = new PaginationRequestDto { Page = -1, PageSize = 10 };

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(async () =>
                await _quizQuizSetService.GetAllQuizQuizSetAsync(pagination, false));

            _mockQuizQuizSetRepo.Verify(r => r.GetAllAsync(It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public async Task GetAllAsync_WithNegativePageSize_ShouldThrowValidationException()
        {
            // Arrange
            var pagination = new PaginationRequestDto { Page = 1, PageSize = -5 };

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(async () =>
                await _quizQuizSetService.GetAllQuizQuizSetAsync(pagination, false));

            _mockQuizQuizSetRepo.Verify(r => r.GetAllAsync(It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public async Task GetByQuizIdAsync_WithValidQuizId_ShouldReturnPaginatedResponse()
        {
            // Arrange
            var quizId = Guid.NewGuid();
            var pagination = new PaginationRequestDto { Page = 1, PageSize = 10 };
            var quizQuizSets = new List<QuizQuizSet>
            {
                new QuizQuizSet
                {
                    Id = Guid.NewGuid(),
                    QuizId = quizId,
                    QuizSetId = Guid.NewGuid(),
                    CreatedAt = DateTime.UtcNow
                }
            };

            _mockQuizQuizSetRepo.Setup(r => r.GetByQuizIdAsync(quizId, false))
                .ReturnsAsync(quizQuizSets);

            // Act
            var result = await _quizQuizSetService.GetQuizQuizSetByQuizIdAsync(quizId, pagination, false);

            // Assert
            result.Should().NotBeNull();
            result.Data.Should().HaveCount(1);
            result.Data.First().QuizId.Should().Be(quizId);

            _mockQuizQuizSetRepo.Verify(r => r.GetByQuizIdAsync(quizId, false), Times.Once);
        }

        [Fact]
        public async Task GetByQuizIdAsync_WithEmptyQuizId_ShouldThrowArgumentException()
        {
            // Arrange
            var emptyQuizId = Guid.Empty;
            var pagination = new PaginationRequestDto { Page = 1, PageSize = 10 };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _quizQuizSetService.GetQuizQuizSetByQuizIdAsync(emptyQuizId, pagination, false));

            exception.Message.Should().Be("QuizId cannot be empty");
            _mockQuizQuizSetRepo.Verify(r => r.GetByQuizIdAsync(It.IsAny<Guid>(), It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public async Task GetByQuizIdAsync_WithNegativePageNumber_ShouldThrowValidationException()
        {
            // Arrange
            var quizId = Guid.NewGuid();
            var pagination = new PaginationRequestDto { Page = -1, PageSize = 10 };

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(
                () => _quizQuizSetService.GetQuizQuizSetByQuizIdAsync(quizId, pagination, false));

            _mockQuizQuizSetRepo.Verify(r => r.GetByQuizIdAsync(It.IsAny<Guid>(), It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public async Task GetByQuizIdAsync_WithNegativePageSize_ShouldThrowValidationException()
        {
            // Arrange
            var quizId = Guid.NewGuid();
            var pagination = new PaginationRequestDto { Page = 1, PageSize = -5 };

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(
                () => _quizQuizSetService.GetQuizQuizSetByQuizIdAsync(quizId, pagination, false));

            _mockQuizQuizSetRepo.Verify(r => r.GetByQuizIdAsync(It.IsAny<Guid>(), It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public async Task GetByQuizSetIdAsync_WithValidQuizSetId_ShouldReturnPaginatedResponse()
        {
            // Arrange
            var quizSetId = Guid.NewGuid();
            var pagination = new PaginationRequestDto { Page = 1, PageSize = 10 };
            var quizQuizSets = new List<QuizQuizSet>
            {
                new QuizQuizSet
                {
                    Id = Guid.NewGuid(),
                    QuizId = Guid.NewGuid(),
                    QuizSetId = quizSetId,
                    CreatedAt = DateTime.UtcNow
                }
            };

            _mockQuizQuizSetRepo.Setup(r => r.GetByQuizSetIdAsync(quizSetId, false))
                .ReturnsAsync(quizQuizSets);

            // Act
            var result = await _quizQuizSetService.GetQuizQuizSetByQuizSetIdAsync(quizSetId, pagination, false);

            // Assert
            result.Should().NotBeNull();
            result.Data.Should().HaveCount(1);
            result.Data.First().QuizSetId.Should().Be(quizSetId);

            _mockQuizQuizSetRepo.Verify(r => r.GetByQuizSetIdAsync(quizSetId, false), Times.Once);
        }

        [Fact]
        public async Task GetByQuizSetIdAsync_WithEmptyQuizSetId_ShouldThrowArgumentException()
        {
            // Arrange
            var emptyQuizSetId = Guid.Empty;
            var pagination = new PaginationRequestDto { Page = 1, PageSize = 10 };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _quizQuizSetService.GetQuizQuizSetByQuizSetIdAsync(emptyQuizSetId, pagination, false));

            exception.Message.Should().Be("QuizSetId cannot be empty");
            _mockQuizQuizSetRepo.Verify(r => r.GetByQuizSetIdAsync(It.IsAny<Guid>(), It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public async Task GetByQuizSetIdAsync_WithNegativePageNumber_ShouldThrowValidationException()
        {
            // Arrange
            var quizSetId = Guid.NewGuid();
            var pagination = new PaginationRequestDto { Page = -1, PageSize = 10 };

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(
                () => _quizQuizSetService.GetQuizQuizSetByQuizSetIdAsync(quizSetId, pagination, false));

            _mockQuizQuizSetRepo.Verify(r => r.GetByQuizSetIdAsync(It.IsAny<Guid>(), It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public async Task GetByQuizSetIdAsync_WithNegativePageSize_ShouldThrowValidationException()
        {
            // Arrange
            var quizSetId = Guid.NewGuid();
            var pagination = new PaginationRequestDto { Page = 1, PageSize = -5 };

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(
                () => _quizQuizSetService.GetQuizQuizSetByQuizSetIdAsync(quizSetId, pagination, false));

            _mockQuizQuizSetRepo.Verify(r => r.GetByQuizSetIdAsync(It.IsAny<Guid>(), It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public async Task GetByQuizAndQuizSetAsync_WithValidIds_ShouldReturnQuizQuizSetResponse()
        {
            // Arrange
            var quizId = Guid.NewGuid();
            var quizSetId = Guid.NewGuid();
            var quizQuizSet = new QuizQuizSet
            {
                Id = Guid.NewGuid(),
                QuizId = quizId,
                QuizSetId = quizSetId,
                CreatedAt = DateTime.UtcNow
            };

            _mockQuizQuizSetRepo.Setup(r => r.GetByQuizAndQuizSetAsync(quizId, quizSetId, false))
                .ReturnsAsync(quizQuizSet);

            // Act
            var result = await _quizQuizSetService.GetQuizQuizSetByQuizAndQuizSetAsync(quizId, quizSetId, false);

            // Assert
            result.Should().NotBeNull();
            result.QuizId.Should().Be(quizId);
            result.QuizSetId.Should().Be(quizSetId);

            _mockQuizQuizSetRepo.Verify(r => r.GetByQuizAndQuizSetAsync(quizId, quizSetId, false), Times.Once);
        }

        [Fact]
        public async Task GetByQuizAndQuizSetAsync_WithNonExistentIds_ShouldReturnNull()
        {
            // Arrange
            var quizId = Guid.NewGuid();
            var quizSetId = Guid.NewGuid();

            _mockQuizQuizSetRepo.Setup(r => r.GetByQuizAndQuizSetAsync(quizId, quizSetId, false))
                .ReturnsAsync((QuizQuizSet?)null);

            // Act
            var result = await _quizQuizSetService.GetQuizQuizSetByQuizAndQuizSetAsync(quizId, quizSetId, false);

            // Assert
            result.Should().BeNull();
            _mockQuizQuizSetRepo.Verify(r => r.GetByQuizAndQuizSetAsync(quizId, quizSetId, false), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_WithValidData_ShouldReturnUpdatedQuizQuizSetResponse()
        {
            // Arrange
            var id = Guid.NewGuid();
            var quizId = Guid.NewGuid();
            var quizSetId = Guid.NewGuid();
            var request = new RequestQuizQuizSetDto
            {
                QuizId = quizId,
                QuizSetId = quizSetId
            };

            var quiz = new Quiz { Id = quizId, QuestionText = "Sample question", TOEICPart = "Part1" };
            var quizSet = new QuizSet { Id = quizSetId, Title = "Sample quiz set" };

            var updatedQuizQuizSet = new QuizQuizSet
            {
                Id = id,
                QuizId = quizId,
                QuizSetId = quizSetId,
                CreatedAt = DateTime.UtcNow.AddHours(-1),
                UpdatedAt = DateTime.UtcNow
            };

            _mockQuizRepo.Setup(r => r.GetQuizByIdAsync(quizId))
                .ReturnsAsync(quiz);
            _mockQuizSetRepo.Setup(r => r.GetQuizSetByIdAsync(quizSetId))
                .ReturnsAsync(quizSet);
            _mockQuizQuizSetRepo.Setup(r => r.UpdateAsync(id, It.IsAny<QuizQuizSet>()))
                .ReturnsAsync(updatedQuizQuizSet);

            // Act
            var result = await _quizQuizSetService.UpdateQuizQuizSetAsync(id, request);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(id);
            result.QuizId.Should().Be(quizId);
            result.QuizSetId.Should().Be(quizSetId);

            _mockQuizRepo.Verify(r => r.GetQuizByIdAsync(quizId), Times.Once);
            _mockQuizSetRepo.Verify(r => r.GetQuizSetByIdAsync(quizSetId), Times.Once);
            _mockQuizQuizSetRepo.Verify(r => r.UpdateAsync(id, It.IsAny<QuizQuizSet>()), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_WithNonExistentId_ShouldReturnNull()
        {
            // Arrange
            var id = Guid.NewGuid();
            var quizId = Guid.NewGuid();
            var quizSetId = Guid.NewGuid();
            var request = new RequestQuizQuizSetDto
            {
                QuizId = quizId,
                QuizSetId = quizSetId
            };

            var quiz = new Quiz { Id = quizId, QuestionText = "Sample question", TOEICPart = "Part1" };
            var quizSet = new QuizSet { Id = quizSetId, Title = "Sample quiz set" };

            _mockQuizRepo.Setup(r => r.GetQuizByIdAsync(quizId))
                .ReturnsAsync(quiz);
            _mockQuizSetRepo.Setup(r => r.GetQuizSetByIdAsync(quizSetId))
                .ReturnsAsync(quizSet);
            _mockQuizQuizSetRepo.Setup(r => r.UpdateAsync(id, It.IsAny<QuizQuizSet>()))
                .ReturnsAsync((QuizQuizSet?)null);

            // Act
            var result = await _quizQuizSetService.UpdateQuizQuizSetAsync(id, request);

            // Assert
            result.Should().BeNull();
            _mockQuizQuizSetRepo.Verify(r => r.UpdateAsync(id, It.IsAny<QuizQuizSet>()), Times.Once);
        }
        [Fact]
        public async Task UpdateAsync_WithNullDto_ShouldThrowNullReferenceException()
        {
            // Arrange
            var id = Guid.NewGuid();
            RequestQuizQuizSetDto? request = null;

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => _quizQuizSetService.UpdateQuizQuizSetAsync(id, request!));

            _mockQuizRepo.Verify(r => r.GetQuizByIdAsync(It.IsAny<Guid>()), Times.Never);
            _mockQuizSetRepo.Verify(r => r.GetQuizSetByIdAsync(It.IsAny<Guid>()), Times.Never);
            _mockQuizQuizSetRepo.Verify(r => r.UpdateAsync(It.IsAny<Guid>(), It.IsAny<QuizQuizSet>()), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_WithNullQuizId_ShouldThrowArgumentException()
        {
            // Arrange
            var id = Guid.NewGuid();
            var request = new RequestQuizQuizSetDto
            {
                QuizId = null,
                QuizSetId = Guid.NewGuid()
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _quizQuizSetService.UpdateQuizQuizSetAsync(id, request));

            exception.Message.Should().Be("QuizId and QuizSetId cannot be null");
            _mockQuizRepo.Verify(r => r.GetQuizByIdAsync(It.IsAny<Guid>()), Times.Never);
            _mockQuizSetRepo.Verify(r => r.GetQuizSetByIdAsync(It.IsAny<Guid>()), Times.Never);
            _mockQuizQuizSetRepo.Verify(r => r.UpdateAsync(It.IsAny<Guid>(), It.IsAny<QuizQuizSet>()), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_WithNullQuizSetId_ShouldThrowArgumentException()
        {
            // Arrange
            var id = Guid.NewGuid();
            var request = new RequestQuizQuizSetDto
            {
                QuizId = Guid.NewGuid(),
                QuizSetId = null
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _quizQuizSetService.UpdateQuizQuizSetAsync(id, request));

            exception.Message.Should().Be("QuizId and QuizSetId cannot be null");
            _mockQuizRepo.Verify(r => r.GetQuizByIdAsync(It.IsAny<Guid>()), Times.Never);
            _mockQuizSetRepo.Verify(r => r.GetQuizSetByIdAsync(It.IsAny<Guid>()), Times.Never);
            _mockQuizQuizSetRepo.Verify(r => r.UpdateAsync(It.IsAny<Guid>(), It.IsAny<QuizQuizSet>()), Times.Never);
        }
        [Fact]
        public async Task UpdateAsync_WithNonExistentQuizId_ShouldThrowArgumentException()
        {
            // Arrange
            var id = Guid.NewGuid();
            var quizId = Guid.NewGuid();
            var quizSetId = Guid.NewGuid();
            var request = new RequestQuizQuizSetDto
            {
                QuizId = quizId,
                QuizSetId = quizSetId
            };

            _mockQuizRepo.Setup(r => r.GetQuizByIdAsync(quizId))
                .ReturnsAsync((Quiz?)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _quizQuizSetService.UpdateQuizQuizSetAsync(id, request));

            exception.Message.Should().Be($"Quiz with ID {quizId} not found");
            _mockQuizRepo.Verify(r => r.GetQuizByIdAsync(quizId), Times.Once);
            _mockQuizSetRepo.Verify(r => r.GetQuizSetByIdAsync(It.IsAny<Guid>()), Times.Never);
            _mockQuizQuizSetRepo.Verify(r => r.UpdateAsync(It.IsAny<Guid>(), It.IsAny<QuizQuizSet>()), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_WithNonExistentQuizSetId_ShouldThrowArgumentException()
        {
            // Arrange
            var id = Guid.NewGuid();
            var quizId = Guid.NewGuid();
            var quizSetId = Guid.NewGuid();
            var request = new RequestQuizQuizSetDto
            {
                QuizId = quizId,
                QuizSetId = quizSetId
            };

            var quiz = new Quiz { Id = quizId, QuestionText = "Sample question", TOEICPart = "Part1" };

            _mockQuizRepo.Setup(r => r.GetQuizByIdAsync(quizId))
                .ReturnsAsync(quiz);
            _mockQuizSetRepo.Setup(r => r.GetQuizSetByIdAsync(quizSetId))
                .ReturnsAsync((QuizSet?)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _quizQuizSetService.UpdateQuizQuizSetAsync(id, request));

            exception.Message.Should().Be($"Quiz set with ID {quizSetId} not found");
            _mockQuizRepo.Verify(r => r.GetQuizByIdAsync(quizId), Times.Once);
            _mockQuizSetRepo.Verify(r => r.GetQuizSetByIdAsync(quizSetId), Times.Once);
            _mockQuizQuizSetRepo.Verify(r => r.UpdateAsync(It.IsAny<Guid>(), It.IsAny<QuizQuizSet>()), Times.Never);
        }

        [Fact]
        public async Task HardDeleteAsync_WithValidId_ShouldReturnTrue()
        {
            // Arrange
            var id = Guid.NewGuid();
            _mockQuizQuizSetRepo.Setup(r => r.HardDeleteAsync(id))
                .ReturnsAsync(true);
            _mockQuizQuizSetRepo.Setup(r => r.GetByIdAsync(id))
                .ReturnsAsync(new QuizQuizSet { Id = id, QuizId = Guid.NewGuid(), QuizSetId = Guid.NewGuid(), CreatedAt = DateTime.UtcNow });
            // Act
            var result = await _quizQuizSetService.HardDeleteQuizQuizSetAsync(id);

            // Assert
            result.Should().BeTrue();
            _mockQuizQuizSetRepo.Verify(r => r.HardDeleteAsync(id), Times.Once);
        }

        [Fact]
        public async Task HardDeleteAsync_WithEmptyId_ShouldThrowArgumentException()
        {
            // Arrange
            var emptyId = Guid.Empty;

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _quizQuizSetService.HardDeleteQuizQuizSetAsync(emptyId));

            exception.Message.Should().Be("ID cannot be empty");
            _mockQuizQuizSetRepo.Verify(r => r.HardDeleteAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task HardDeleteAsync_WithNonExistentId_ShouldThrowArgumentException()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            _mockQuizQuizSetRepo.Setup(r => r.GetByIdAsync(nonExistentId))
                .ReturnsAsync((QuizQuizSet?)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _quizQuizSetService.HardDeleteQuizQuizSetAsync(nonExistentId));

            exception.Message.Should().Be($"QuizQuizSet with ID {nonExistentId} not found");
            _mockQuizQuizSetRepo.Verify(r => r.GetByIdAsync(nonExistentId), Times.Once);
            _mockQuizQuizSetRepo.Verify(r => r.HardDeleteAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task IsExistedAsync_WithExistingAssociation_ShouldReturnTrue()
        {
            // Arrange
            var quizId = Guid.NewGuid();
            var quizSetId = Guid.NewGuid();

            _mockQuizQuizSetRepo.Setup(r => r.IsExistedAsync(quizId, quizSetId))
                .ReturnsAsync(true);

            // Act
            var result = await _quizQuizSetService.IsQuizQuizSetExistedAsync(quizId, quizSetId);

            // Assert
            result.Should().BeTrue();
            _mockQuizQuizSetRepo.Verify(r => r.IsExistedAsync(quizId, quizSetId), Times.Once);
        }

        [Fact]
        public async Task IsExistedAsync_WithEmptyQuizId_ShouldThrowArgumentException()
        {
            // Arrange
            var emptyQuizId = Guid.Empty;
            var quizSetId = Guid.NewGuid();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _quizQuizSetService.IsQuizQuizSetExistedAsync(emptyQuizId, quizSetId));

            exception.Message.Should().Be("QuizId and QuizSetId cannot be empty");
            _mockQuizQuizSetRepo.Verify(r => r.IsExistedAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task IsExistedAsync_WithEmptyQuizSetId_ShouldThrowArgumentException()
        {
            // Arrange
            var quizId = Guid.NewGuid();
            var emptyQuizSetId = Guid.Empty;

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _quizQuizSetService.IsQuizQuizSetExistedAsync(quizId, emptyQuizSetId));

            exception.Message.Should().Be("QuizId and QuizSetId cannot be empty");
            _mockQuizQuizSetRepo.Verify(r => r.IsExistedAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task GetQuizCountByQuizSetAsync_WithValidQuizSetId_ShouldReturnCount()
        {
            // Arrange
            var quizSetId = Guid.NewGuid();
            var expectedCount = 5;

            _mockQuizQuizSetRepo.Setup(r => r.GetQuizCountByQuizSetAsync(quizSetId))
                .ReturnsAsync(expectedCount);

            // Act
            var result = await _quizQuizSetService.GetQuizCountByQuizSetAsync(quizSetId);

            // Assert
            result.Should().Be(expectedCount);
            _mockQuizQuizSetRepo.Verify(r => r.GetQuizCountByQuizSetAsync(quizSetId), Times.Once);
        }

        [Fact]
        public async Task AddQuizToQuizSetAsync_WithValidIds_ShouldReturnTrue()
        {
            // Arrange
            var quizId = Guid.NewGuid();
            var quizSetId = Guid.NewGuid();

            var quiz = new Quiz { Id = quizId, QuestionText = "Sample question", TOEICPart = "Part1" };
            var quizSet = new QuizSet { Id = quizSetId, Title = "Sample quiz set" };

            var createdQuizQuizSet = new QuizQuizSet
            {
                Id = Guid.NewGuid(),
                QuizId = quizId,
                QuizSetId = quizSetId,
                CreatedAt = DateTime.UtcNow
            };

            _mockQuizRepo.Setup(r => r.GetQuizByIdAsync(quizId))
                .ReturnsAsync(quiz);
            _mockQuizSetRepo.Setup(r => r.GetQuizSetByIdAsync(quizSetId))
                .ReturnsAsync(quizSet);
            _mockQuizQuizSetRepo.Setup(r => r.IsExistedAsync(quizId, quizSetId))
                .ReturnsAsync(false);
            _mockQuizQuizSetRepo.Setup(r => r.CreateAsync(It.IsAny<QuizQuizSet>()))
                .ReturnsAsync(createdQuizQuizSet);

            // Act
            var result = await _quizQuizSetService.AddQuizToQuizSetAsync(quizId, quizSetId);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task AddQuizzesToQuizSetAsync_WithValidIds_ShouldReturnTrue()
        {
            // Arrange
            var quizSetId = Guid.NewGuid();
            var quizIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };

            var quizSet = new QuizSet { Id = quizSetId, Title = "Sample quiz set" };
            var quiz1 = new Quiz { Id = quizIds[0], QuestionText = "Question 1", TOEICPart = "Part1" };
            var quiz2 = new Quiz { Id = quizIds[1], QuestionText = "Question 2", TOEICPart = "Part1" };

            _mockQuizSetRepo.Setup(r => r.GetQuizSetByIdAsync(quizSetId))
                .ReturnsAsync(quizSet);
            _mockQuizRepo.Setup(r => r.GetQuizzesByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
                .ReturnsAsync(new List<Quiz> { quiz1, quiz2 });
            _mockQuizQuizSetRepo.Setup(r => r.GetByQuizSetIdAsync(quizSetId, false))
                .ReturnsAsync(new List<QuizQuizSet>()); // No existing associations
            _mockQuizQuizSetRepo.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<QuizQuizSet>>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _quizQuizSetService.AddQuizzesToQuizSetAsync(quizIds, quizSetId);

            // Assert
            result.Should().BeTrue();
            _mockQuizQuizSetRepo.Verify(r => r.AddRangeAsync(It.IsAny<IEnumerable<QuizQuizSet>>()), Times.Once);
        }

        [Fact]
        public async Task DeleteByQuizIdAsync_WithValidQuizId_ShouldReturnTrue()
        {
            // Arrange
            var quizId = Guid.NewGuid();

            _mockQuizQuizSetRepo.Setup(r => r.DeleteByQuizIdAsync(quizId))
                .ReturnsAsync(true);

            // Act
            var result = await _quizQuizSetService.DeleteQuizQuizSetByQuizIdAsync(quizId);

            // Assert
            result.Should().BeTrue();
            _mockQuizQuizSetRepo.Verify(r => r.DeleteByQuizIdAsync(quizId), Times.Once);
        }

        [Fact]
        public async Task DeleteByQuizSetIdAsync_WithNonPlacementQuizSet_ShouldReturnTrue()
        {
            // Arrange
            var quizSetId = Guid.NewGuid();
            var quizSet = new QuizSet
            {
                Id = quizSetId,
                Title = "Sample quiz set",
                QuizSetType = QuizSetTypeEnum.Practice
            };

            _mockQuizSetRepo.Setup(r => r.GetQuizSetByIdAsync(quizSetId))
                .ReturnsAsync(quizSet);
            _mockQuizQuizSetRepo.Setup(r => r.DeleteByQuizSetIdAsync(quizSetId))
                .ReturnsAsync(true);

            // Act
            var result = await _quizQuizSetService.DeleteQuizQuizSetByQuizSetIdAsync(quizSetId);

            // Assert
            result.Should().BeTrue();
            _mockQuizQuizSetRepo.Verify(r => r.DeleteByQuizSetIdAsync(quizSetId), Times.Once);
        }

        [Fact]
        public async Task DeleteByQuizSetIdAsync_WithPlacementQuizSet_ShouldReturnTrue()
        {
            // Arrange
            var quizSetId = Guid.NewGuid();
            var quizSet = new QuizSet
            {
                Id = quizSetId,
                Title = "Placement quiz set",
                QuizSetType = QuizSetTypeEnum.Placement
            };

            var quizzes = new List<Quiz>
            {
                new Quiz { Id = Guid.NewGuid(), QuestionText = "Question 1", TOEICPart = "Part1" },
                new Quiz { Id = Guid.NewGuid(), QuestionText = "Question 2", TOEICPart = "Part1" }
            };

            _mockQuizSetRepo.Setup(r => r.GetQuizSetByIdAsync(quizSetId))
                .ReturnsAsync(quizSet);
            _mockQuizRepo.Setup(r => r.GetQuizzesByQuizSetIdAsync(quizSetId))
                .ReturnsAsync(quizzes);
            _mockQuizRepo.Setup(r => r.HardDeleteQuizzesBatchAsync(quizzes))
                .ReturnsAsync(true);

            // Act
            var result = await _quizQuizSetService.DeleteQuizQuizSetByQuizSetIdAsync(quizSetId);

            // Assert
            result.Should().BeTrue();
            _mockQuizRepo.Verify(r => r.GetQuizzesByQuizSetIdAsync(quizSetId), Times.Once);
            _mockQuizRepo.Verify(r => r.HardDeleteQuizzesBatchAsync(quizzes), Times.Once);
        }


        [Fact]
        public async Task GetQuizCountByQuizSetAsync_WithEmptyQuizSetId_ShouldThrowArgumentException()
        {
            // Arrange
            var emptyQuizSetId = Guid.Empty;

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _quizQuizSetService.GetQuizCountByQuizSetAsync(emptyQuizSetId));

            exception.Message.Should().Be("QuizSetId cannot be null");
            _mockQuizQuizSetRepo.Verify(r => r.GetQuizCountByQuizSetAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task AddQuizToQuizSetAsync_WithEmptyQuizId_ShouldThrowArgumentException()
        {
            // Arrange
            var emptyQuizId = Guid.Empty;
            var quizSetId = Guid.NewGuid();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _quizQuizSetService.AddQuizToQuizSetAsync(emptyQuizId, quizSetId));

            exception.Message.Should().Be("QuizId and QuizSetId cannot be empty");
            _mockQuizQuizSetRepo.Verify(r => r.IsExistedAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task AddQuizToQuizSetAsync_WithEmptyQuizSetId_ShouldThrowArgumentException()
        {
            // Arrange
            var quizId = Guid.NewGuid();
            var emptyQuizSetId = Guid.Empty;

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _quizQuizSetService.AddQuizToQuizSetAsync(quizId, emptyQuizSetId));

            exception.Message.Should().Be("QuizId and QuizSetId cannot be empty");
            _mockQuizQuizSetRepo.Verify(r => r.IsExistedAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task AddQuizzesToQuizSetAsync_WithEmptyQuizSetId_ShouldThrowArgumentException()
        {
            // Arrange
            var quizIds = new List<Guid> { Guid.NewGuid() };
            var emptyQuizSetId = Guid.Empty;

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _quizQuizSetService.AddQuizzesToQuizSetAsync(quizIds, emptyQuizSetId));

            exception.Message.Should().Be("QuizSetId cannot be empty");
            _mockQuizQuizSetRepo.Verify(r => r.AddRangeAsync(It.IsAny<IEnumerable<QuizQuizSet>>()), Times.Never);
        }

        [Fact]
        public async Task AddQuizzesToQuizSetAsync_WithEmptyQuizIds_ShouldThrowArgumentException()
        {
            // Arrange
            var quizIds = new List<Guid>();
            var quizSetId = Guid.NewGuid();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _quizQuizSetService.AddQuizzesToQuizSetAsync(quizIds, quizSetId));

            exception.Message.Should().Be("QuizIds cannot be empty");
            _mockQuizQuizSetRepo.Verify(r => r.AddRangeAsync(It.IsAny<IEnumerable<QuizQuizSet>>()), Times.Never);
        }

        [Fact]
        public async Task DeleteQuizQuizSetByQuizIdAsync_WithEmptyQuizId_ShouldThrowArgumentException()
        {
            // Arrange
            var emptyQuizId = Guid.Empty;

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _quizQuizSetService.DeleteQuizQuizSetByQuizIdAsync(emptyQuizId));

            exception.Message.Should().Be("QuizId cannot be empty");
            _mockQuizQuizSetRepo.Verify(r => r.DeleteByQuizIdAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task DeleteQuizQuizSetByQuizSetIdAsync_WithEmptyQuizSetId_ShouldThrowArgumentException()
        {
            // Arrange
            var emptyQuizSetId = Guid.Empty;
            var quizSet = new QuizSet
            {
                Id = emptyQuizSetId,
                Title = "Sample quiz set",
                QuizSetType = QuizSetTypeEnum.Practice
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _quizQuizSetService.DeleteQuizQuizSetByQuizSetIdAsync(emptyQuizSetId));

            exception.Message.Should().Be("QuizSetId cannot be empty");
            _mockQuizQuizSetRepo.Verify(r => r.DeleteByQuizSetIdAsync(It.IsAny<Guid>()), Times.Never);
        }
    }
}