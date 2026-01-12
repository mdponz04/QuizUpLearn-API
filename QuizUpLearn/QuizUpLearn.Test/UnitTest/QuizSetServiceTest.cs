using AutoMapper;
using BusinessLogic.DTOs;
using BusinessLogic.DTOs.QuizGroupItemDtos;
using BusinessLogic.DTOs.QuizSetDtos;
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
    public class QuizSetServiceTest : BaseServiceTest
    {
        private readonly Mock<IQuizSetRepo> _mockQuizSetRepo;
        private readonly IMapper _mapper;
        private readonly QuizSetService _quizSetService;

        public QuizSetServiceTest()
        {
            _mockQuizSetRepo = new Mock<IQuizSetRepo>();

            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<MappingProfile>();
            }, new NullLoggerFactory());
            _mapper = mapperConfig.CreateMapper();

            _quizSetService = new QuizSetService(_mockQuizSetRepo.Object, _mapper);
        }

        #region CreateQuizSetAsync Tests

        [Fact]
        public async Task CreateQuizSetAsync_WithValidRequest_ShouldReturnQuizSetResponse()
        {
            // Arrange
            var request = new QuizSetRequestDto
            {
                Title = "Sample Set",
                Description = "A test quiz set",
                QuizSetType = QuizSetTypeEnum.Practice,
                CreatedBy = Guid.NewGuid(),
                IsPublished = true,
                IsPremiumOnly = false,
                QuizGroupItems = new List<RequestQuizGroupItemDto>()
            };

            var createdQuizSet = new QuizSet
            {
                Id = Guid.NewGuid(),
                Title = request.Title,
                Description = request.Description,
                QuizSetType = request.QuizSetType.Value,
                CreatedBy = request.CreatedBy.Value,
                IsPublished = request.IsPublished.Value,
                IsPremiumOnly = request.IsPremiumOnly.Value,
                CreatedAt = DateTime.UtcNow
            };

            _mockQuizSetRepo.Setup(r => r.CreateQuizSetAsync(It.IsAny<QuizSet>()))
                .ReturnsAsync(createdQuizSet);

            // Act
            var result = await _quizSetService.CreateQuizSetAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Title.Should().Be(request.Title);
            result.Description.Should().Be(request.Description);
            result.QuizSetType.Should().Be(request.QuizSetType);
            result.CreatedBy.Should().Be(request.CreatedBy.Value);
            result.IsPublished.Should().Be(request.IsPublished.Value);
            result.IsPremiumOnly.Should().Be(request.IsPremiumOnly.Value);

            _mockQuizSetRepo.Verify(r => r.CreateQuizSetAsync(It.IsAny<QuizSet>()), Times.Once);
        }

        [Fact]
        public async Task CreateQuizSetAsync_WithNullCreatedBy_ShouldThrowArgumentException()
        {
            // Arrange
            var request = new QuizSetRequestDto
            {
                Title = "Sample Set",
                Description = "A test quiz set",
                QuizSetType = QuizSetTypeEnum.Practice,
                CreatedBy = null,
                IsPublished = true,
                IsPremiumOnly = false,
                QuizGroupItems = new List<RequestQuizGroupItemDto>()
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _quizSetService.CreateQuizSetAsync(request));
            _mockQuizSetRepo.Verify(r => r.CreateQuizSetAsync(It.IsAny<QuizSet>()), Times.Never);
        }

        [Fact]
        public async Task CreateQuizSetAsync_WithEmptyCreatedBy_ShouldThrowArgumentException()
        {
            // Arrange
            var request = new QuizSetRequestDto
            {
                Title = "Sample Set",
                Description = "A test quiz set",
                QuizSetType = QuizSetTypeEnum.Practice,
                CreatedBy = Guid.Empty,
                IsPublished = true,
                IsPremiumOnly = false,
                QuizGroupItems = new List<RequestQuizGroupItemDto>()
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _quizSetService.CreateQuizSetAsync(request));
            _mockQuizSetRepo.Verify(r => r.CreateQuizSetAsync(It.IsAny<QuizSet>()), Times.Never);
        }

        #endregion

        #region GetQuizSetByIdAsync Tests

        [Fact]
        public async Task GetQuizSetByIdAsync_WithValidId_ShouldReturnQuizSetResponse()
        {
            // Arrange
            var quizSetId = Guid.NewGuid();
            var quizSet = new QuizSet
            {
                Id = quizSetId,
                Title = "Set 1",
                Description = "Desc",
                QuizSetType = QuizSetTypeEnum.Practice,
                CreatedBy = Guid.NewGuid(),
                IsPublished = true,
                IsPremiumOnly = false,
                CreatedAt = DateTime.UtcNow
            };

            _mockQuizSetRepo.Setup(r => r.GetQuizSetByIdAsync(quizSetId))
                .ReturnsAsync(quizSet);

            // Act
            var result = await _quizSetService.GetQuizSetByIdAsync(quizSetId);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(quizSetId);
            result.Title.Should().Be(quizSet.Title);

            _mockQuizSetRepo.Verify(r => r.GetQuizSetByIdAsync(quizSetId), Times.Once);
        }

        [Fact]
        public async Task GetQuizSetByIdAsync_WithNonExistentId_ShouldReturnNull()
        {
            // Arrange
            var quizSetId = Guid.NewGuid();
            _mockQuizSetRepo.Setup(r => r.GetQuizSetByIdAsync(quizSetId))
                .ReturnsAsync((QuizSet?)null);

            // Act
            var result = await _quizSetService.GetQuizSetByIdAsync(quizSetId);

            // Assert
            result.Should().BeNull();
            _mockQuizSetRepo.Verify(r => r.GetQuizSetByIdAsync(quizSetId), Times.Once);
        }

        [Fact]
        public async Task GetQuizSetByIdAsync_WithEmptyGuid_ShouldReturnNull()
        {
            // Arrange
            var emptyId = Guid.Empty;
            _mockQuizSetRepo.Setup(r => r.GetQuizSetByIdAsync(emptyId))
                .ReturnsAsync((QuizSet?)null);

            // Act
            var result = await _quizSetService.GetQuizSetByIdAsync(emptyId);

            // Assert
            result.Should().BeNull();
            _mockQuizSetRepo.Verify(r => r.GetQuizSetByIdAsync(emptyId), Times.Once);
        }

        #endregion

        #region GetAllQuizSetsAsync Tests

        [Fact]
        public async Task GetAllQuizSetsAsync_WithValidPagination_ShouldReturnPaginatedQuizSets()
        {
            // Arrange
            var pagination = new PaginationRequestDto { Page = 1, PageSize = 10 };
            var quizSets = new List<QuizSet>
            {
                new QuizSet { Id = Guid.NewGuid(), Title = "Set 1", CreatedAt = DateTime.UtcNow },
                new QuizSet { Id = Guid.NewGuid(), Title = "Set 2", CreatedAt = DateTime.UtcNow }
            };

            _mockQuizSetRepo.Setup(r => r.GetAllQuizSetsAsync())
                .ReturnsAsync(quizSets);

            // Act
            var result = await _quizSetService.GetAllQuizSetsAsync(pagination);

            // Assert
            result.Should().NotBeNull();
            result.Data.Should().HaveCount(2);
            result.Pagination.TotalCount.Should().Be(2);

            _mockQuizSetRepo.Verify(r => r.GetAllQuizSetsAsync(), Times.Once);
        }

        [Fact]
        public async Task GetAllQuizSetsAsync_WithInvalidPagination_ShouldThrowValidationException()
        {
            var pagination = new PaginationRequestDto { Page = -1, PageSize = -10 };
            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(() => _quizSetService.GetAllQuizSetsAsync(pagination));
            _mockQuizSetRepo.Verify(r => r.GetAllQuizSetsAsync(), Times.Never);
        }

        [Fact]
        public async Task GetAllQuizSetsAsync_WhenRepositoryThrowsException_ShouldPropagateException()
        {
            // Arrange
            var pagination = new PaginationRequestDto { Page = 1, PageSize = 10 };
            _mockQuizSetRepo.Setup(r => r.GetAllQuizSetsAsync())
                .ThrowsAsync(new InvalidOperationException("Database error"));

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _quizSetService.GetAllQuizSetsAsync(pagination));
        }

        #endregion

        #region GetQuizSetsByCreatorAsync Tests

        [Fact]
        public async Task GetQuizSetsByCreatorAsync_WithValidCreatorId_ShouldReturnPaginatedQuizSets()
        {
            // Arrange
            var creatorId = Guid.NewGuid();
            var pagination = new PaginationRequestDto { Page = 1, PageSize = 10 };
            var quizSets = new List<QuizSet>
            {
                new QuizSet { Id = Guid.NewGuid(), Title = "Set 1", CreatedBy = creatorId, CreatedAt = DateTime.UtcNow }
            };

            _mockQuizSetRepo.Setup(r => r.GetQuizSetsByCreatorAsync(creatorId))
                .ReturnsAsync(quizSets);

            // Act
            var result = await _quizSetService.GetQuizSetsByCreatorAsync(creatorId, pagination);

            // Assert
            result.Should().NotBeNull();
            result.Data.Should().HaveCount(1);
            result.Data[0].CreatedBy.Should().Be(creatorId);

            _mockQuizSetRepo.Verify(r => r.GetQuizSetsByCreatorAsync(creatorId), Times.Once);
        }

        [Fact]
        public async Task GetQuizSetsByCreatorAsync_WithEmptyCreatorId_ShouldReturnEmptyResult()
        {
            // Arrange
            var emptyCreatorId = Guid.Empty;
            var pagination = new PaginationRequestDto { Page = 1, PageSize = 10 };
            _mockQuizSetRepo.Setup(r => r.GetQuizSetsByCreatorAsync(emptyCreatorId))
                .ReturnsAsync(new List<QuizSet>());

            // Act
            var result = await _quizSetService.GetQuizSetsByCreatorAsync(emptyCreatorId, pagination);

            // Assert
            result.Should().NotBeNull();
            result.Data.Should().BeEmpty();
            result.Pagination.TotalCount.Should().Be(0);
        }

        #endregion

        #region GetPublishedQuizSetsAsync Tests

        [Fact]
        public async Task GetPublishedQuizSetsAsync_WithValidPagination_ShouldReturnPaginatedQuizSets()
        {
            // Arrange
            var pagination = new PaginationRequestDto { Page = 1, PageSize = 10 };
            var quizSets = new List<QuizSet>
            {
                new QuizSet { Id = Guid.NewGuid(), Title = "Set 1", IsPublished = true, CreatedAt = DateTime.UtcNow }
            };

            _mockQuizSetRepo.Setup(r => r.GetPublishedQuizSetsAsync())
                .ReturnsAsync(quizSets);

            // Act
            var result = await _quizSetService.GetPublishedQuizSetsAsync(pagination);

            // Assert
            result.Should().NotBeNull();
            result.Data.Should().HaveCount(1);
            result.Data[0].IsPublished.Should().BeTrue();

            _mockQuizSetRepo.Verify(r => r.GetPublishedQuizSetsAsync(), Times.Once);
        }

        [Fact]
        public async Task GetPublishedQuizSetsAsync_WhenRepositoryReturnsNull_ShouldHandleGracefully()
        {
            // Arrange
            var pagination = new PaginationRequestDto { Page = 1, PageSize = 10 };
            _mockQuizSetRepo.Setup(r => r.GetPublishedQuizSetsAsync())
                .ReturnsAsync((IEnumerable<QuizSet>)null);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _quizSetService.GetPublishedQuizSetsAsync(pagination));
        }

        #endregion

        #region UpdateQuizSetAsync Tests

        [Fact]
        public async Task UpdateQuizSetAsync_WithValidData_ShouldReturnUpdatedQuizSetResponse()
        {
            // Arrange
            var quizSetId = Guid.NewGuid();
            var request = new QuizSetRequestDto
            {
                Title = "Updated Title",
                Description = "Updated Desc",
                QuizSetType = QuizSetTypeEnum.Event,
                CreatedBy = Guid.NewGuid(),
                IsPublished = false,
                IsPremiumOnly = true,
                QuizGroupItems = new List<RequestQuizGroupItemDto>()
            };

            var updatedQuizSet = new QuizSet
            {
                Id = quizSetId,
                Title = request.Title,
                Description = request.Description,
                QuizSetType = request.QuizSetType.Value,
                CreatedBy = request.CreatedBy.Value,
                IsPublished = request.IsPublished.Value,
                IsPremiumOnly = request.IsPremiumOnly.Value,
                UpdatedAt = DateTime.UtcNow
            };

            _mockQuizSetRepo.Setup(r => r.UpdateQuizSetAsync(quizSetId, It.IsAny<QuizSet>()))
                .ReturnsAsync(updatedQuizSet);

            // Act
            var result = await _quizSetService.UpdateQuizSetAsync(quizSetId, request);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(quizSetId);
            result.Title.Should().Be(request.Title);
            result.Description.Should().Be(request.Description);

            _mockQuizSetRepo.Verify(r => r.UpdateQuizSetAsync(quizSetId, It.IsAny<QuizSet>()), Times.Once);
        }

        [Fact]
        public async Task UpdateQuizSetAsync_WithNonExistentId_ShouldReturnNull()
        {
            // Arrange
            var quizSetId = Guid.NewGuid();
            var request = new QuizSetRequestDto
            {
                Title = "Updated Title",
                CreatedBy = Guid.NewGuid(),
                QuizGroupItems = new List<RequestQuizGroupItemDto>()
            };

            _mockQuizSetRepo.Setup(r => r.UpdateQuizSetAsync(quizSetId, It.IsAny<QuizSet>()))
                .ReturnsAsync((QuizSet?)null);

            // Act
            var result = await _quizSetService.UpdateQuizSetAsync(quizSetId, request);

            // Assert
            result.Should().BeNull();
            _mockQuizSetRepo.Verify(r => r.UpdateQuizSetAsync(quizSetId, It.IsAny<QuizSet>()), Times.Once);
        }

        [Fact]
        public async Task UpdateQuizSetAsync_WithNullRequest_ShouldThrowArgumentNullException()
        {
            // Arrange
            var quizSetId = Guid.NewGuid();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _quizSetService.UpdateQuizSetAsync(quizSetId, null!));
            _mockQuizSetRepo.Verify(r => r.UpdateQuizSetAsync(It.IsAny<Guid>(), It.IsAny<QuizSet>()), Times.Never);
        }

        #endregion

        #region SoftDeleteQuizSetAsync Tests

        [Fact]
        public async Task SoftDeleteQuizSetAsync_WithValidId_ShouldReturnTrue()
        {
            // Arrange
            var quizSetId = Guid.NewGuid();
            _mockQuizSetRepo.Setup(r => r.SoftDeleteQuizSetAsync(quizSetId))
                .ReturnsAsync(true);

            // Act
            var result = await _quizSetService.SoftDeleteQuizSetAsync(quizSetId);

            // Assert
            result.Should().BeTrue();
            _mockQuizSetRepo.Verify(r => r.SoftDeleteQuizSetAsync(quizSetId), Times.Once);
        }

        [Fact]
        public async Task SoftDeleteQuizSetAsync_WithNonExistentId_ShouldReturnFalse()
        {
            // Arrange
            var quizSetId = Guid.NewGuid();
            _mockQuizSetRepo.Setup(r => r.SoftDeleteQuizSetAsync(quizSetId))
                .ReturnsAsync(false);

            // Act
            var result = await _quizSetService.SoftDeleteQuizSetAsync(quizSetId);

            // Assert
            result.Should().BeFalse();
            _mockQuizSetRepo.Verify(r => r.SoftDeleteQuizSetAsync(quizSetId), Times.Once);
        }

        [Fact]
        public async Task SoftDeleteQuizSetAsync_WithEmptyGuid_ShouldReturnFalse()
        {
            // Arrange
            var emptyId = Guid.Empty;
            _mockQuizSetRepo.Setup(r => r.SoftDeleteQuizSetAsync(emptyId))
                .ReturnsAsync(false);

            // Act
            var result = await _quizSetService.SoftDeleteQuizSetAsync(emptyId);

            // Assert
            result.Should().BeFalse();
            _mockQuizSetRepo.Verify(r => r.SoftDeleteQuizSetAsync(emptyId), Times.Once);
        }

        #endregion

        #region HardDeleteQuizSetAsync Tests

        [Fact]
        public async Task HardDeleteQuizSetAsync_WithValidId_ShouldReturnTrue()
        {
            // Arrange
            var quizSetId = Guid.NewGuid();
            _mockQuizSetRepo.Setup(r => r.HardDeleteQuizSetAsync(quizSetId))
                .ReturnsAsync(true);

            // Act
            var result = await _quizSetService.HardDeleteQuizSetAsync(quizSetId);

            // Assert
            result.Should().BeTrue();
            _mockQuizSetRepo.Verify(r => r.HardDeleteQuizSetAsync(quizSetId), Times.Once);
        }

        [Fact]
        public async Task HardDeleteQuizSetAsync_WithNonExistentId_ShouldReturnFalse()
        {
            // Arrange
            var quizSetId = Guid.NewGuid();
            _mockQuizSetRepo.Setup(r => r.HardDeleteQuizSetAsync(quizSetId))
                .ReturnsAsync(false);

            // Act
            var result = await _quizSetService.HardDeleteQuizSetAsync(quizSetId);

            // Assert
            result.Should().BeFalse();
            _mockQuizSetRepo.Verify(r => r.HardDeleteQuizSetAsync(quizSetId), Times.Once);
        }

        [Fact]
        public async Task HardDeleteQuizSetAsync_WhenRepositoryThrowsException_ShouldPropagateException()
        {
            // Arrange
            var quizSetId = Guid.NewGuid();
            _mockQuizSetRepo.Setup(r => r.HardDeleteQuizSetAsync(quizSetId))
                .ThrowsAsync(new InvalidOperationException("Cannot delete quiz set"));

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _quizSetService.HardDeleteQuizSetAsync(quizSetId));
        }

        #endregion

        #region RequestValidateByModAsync Tests

        [Fact]
        public async Task RequestValidateByModAsync_WithValidId_ShouldReturnTrue()
        {
            // Arrange
            var quizSetId = Guid.NewGuid();
            _mockQuizSetRepo.Setup(r => r.RequestValidateByModAsync(quizSetId))
                .ReturnsAsync(true);

            // Act
            var result = await _quizSetService.RequestValidateByModAsync(quizSetId);

            // Assert
            result.Should().BeTrue();
            _mockQuizSetRepo.Verify(r => r.RequestValidateByModAsync(quizSetId), Times.Once);
        }

        [Fact]
        public async Task RequestValidateByModAsync_WithNonExistentId_ShouldReturnFalse()
        {
            // Arrange
            var quizSetId = Guid.NewGuid();
            _mockQuizSetRepo.Setup(r => r.RequestValidateByModAsync(quizSetId))
                .ReturnsAsync(false);

            // Act
            var result = await _quizSetService.RequestValidateByModAsync(quizSetId);

            // Assert
            result.Should().BeFalse();
            _mockQuizSetRepo.Verify(r => r.RequestValidateByModAsync(quizSetId), Times.Once);
        }

        [Fact]
        public async Task RequestValidateByModAsync_WithEmptyGuid_ShouldReturnFalse()
        {
            // Arrange
            var emptyId = Guid.Empty;
            _mockQuizSetRepo.Setup(r => r.RequestValidateByModAsync(emptyId))
                .ReturnsAsync(false);

            // Act
            var result = await _quizSetService.RequestValidateByModAsync(emptyId);

            // Assert
            result.Should().BeFalse();
            _mockQuizSetRepo.Verify(r => r.RequestValidateByModAsync(emptyId), Times.Once);
        }

        #endregion

        #region ValidateQuizSetAsync Tests

        [Fact]
        public async Task ValidateQuizSetAsync_WithValidId_ShouldReturnTrue()
        {
            // Arrange
            var quizSetId = Guid.NewGuid();
            _mockQuizSetRepo.Setup(r => r.ValidateQuizSetAsync(quizSetId))
                .ReturnsAsync(true);

            // Act
            var result = await _quizSetService.ValidateQuizSetAsync(quizSetId);

            // Assert
            result.Should().BeTrue();
            _mockQuizSetRepo.Verify(r => r.ValidateQuizSetAsync(quizSetId), Times.Once);
        }

        [Fact]
        public async Task ValidateQuizSetAsync_WithNonExistentId_ShouldReturnFalse()
        {
            // Arrange
            var quizSetId = Guid.NewGuid();
            _mockQuizSetRepo.Setup(r => r.ValidateQuizSetAsync(quizSetId))
                .ReturnsAsync(false);

            // Act
            var result = await _quizSetService.ValidateQuizSetAsync(quizSetId);

            // Assert
            result.Should().BeFalse();
            _mockQuizSetRepo.Verify(r => r.ValidateQuizSetAsync(quizSetId), Times.Once);
        }

        [Fact]
        public async Task ValidateQuizSetAsync_WhenRepositoryThrowsException_ShouldPropagateException()
        {
            // Arrange
            var quizSetId = Guid.NewGuid();
            _mockQuizSetRepo.Setup(r => r.ValidateQuizSetAsync(quizSetId))
                .ThrowsAsync(new InvalidOperationException("Validation failed"));

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _quizSetService.ValidateQuizSetAsync(quizSetId));
        }

        #endregion

        #region RestoreQuizSetAsync Tests

        [Fact]
        public async Task RestoreQuizSetAsync_WithValidId_ShouldReturnQuizSetResponse()
        {
            // Arrange
            var quizSetId = Guid.NewGuid();
            var quizSet = new QuizSet
            {
                Id = quizSetId,
                Title = "Restored Set",
                CreatedAt = DateTime.UtcNow
            };

            _mockQuizSetRepo.Setup(r => r.RestoreQuizSetAsync(quizSetId))
                .ReturnsAsync(quizSet);

            // Act
            var result = await _quizSetService.RestoreQuizSetAsync(quizSetId);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(quizSetId);
            result.Title.Should().Be("Restored Set");
            _mockQuizSetRepo.Verify(r => r.RestoreQuizSetAsync(quizSetId), Times.Once);
        }

        [Fact]
        public async Task RestoreQuizSetAsync_WithNonExistentId_ShouldReturnNull()
        {
            // Arrange
            var quizSetId = Guid.NewGuid();
            _mockQuizSetRepo.Setup(r => r.RestoreQuizSetAsync(quizSetId))
                .ReturnsAsync((QuizSet?)null);

            // Act
            var result = await _quizSetService.RestoreQuizSetAsync(quizSetId);

            // Assert
            result.Should().BeNull();
            _mockQuizSetRepo.Verify(r => r.RestoreQuizSetAsync(quizSetId), Times.Once);
        }

        [Fact]
        public async Task RestoreQuizSetAsync_WhenRepositoryThrowsException_ShouldPropagateException()
        {
            // Arrange
            var quizSetId = Guid.NewGuid();
            _mockQuizSetRepo.Setup(r => r.RestoreQuizSetAsync(quizSetId))
                .ThrowsAsync(new InvalidOperationException("Restore failed"));

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _quizSetService.RestoreQuizSetAsync(quizSetId));
        }

        #endregion
    }
}

