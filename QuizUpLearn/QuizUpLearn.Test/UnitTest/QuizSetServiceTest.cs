using AutoMapper;
using BusinessLogic.DTOs;
using BusinessLogic.DTOs.QuizGroupItemDtos;
using BusinessLogic.DTOs.QuizSetDtos;
using BusinessLogic.Interfaces;
using BusinessLogic.MappingProfile;
using BusinessLogic.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Repository.Entities;
using Repository.Enums;
using Repository.Interfaces;
using Xunit;

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
        }

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

            _mockQuizSetRepo.Setup(r => r.GetAllQuizSetsAsync(pagination))
                .ReturnsAsync((quizSets, 2));

            // Act
            var result = await _quizSetService.GetAllQuizSetsAsync(pagination);

            // Assert
            result.Should().NotBeNull();
            result.Data.Should().HaveCount(2);
            result.Pagination.TotalCount.Should().Be(2);
        }

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

            _mockQuizSetRepo.Setup(r => r.GetQuizSetsByCreatorAsync(creatorId, pagination))
                .ReturnsAsync((quizSets, 1));

            // Act
            var result = await _quizSetService.GetQuizSetsByCreatorAsync(creatorId, pagination);

            // Assert
            result.Should().NotBeNull();
            result.Data.Should().HaveCount(1);
            result.Data[0].CreatedBy.Should().Be(creatorId);
        }

        [Fact]
        public async Task GetPublishedQuizSetsAsync_WithValidPagination_ShouldReturnPaginatedQuizSets()
        {
            // Arrange
            var pagination = new PaginationRequestDto { Page = 1, PageSize = 10 };
            var quizSets = new List<QuizSet>
            {
                new QuizSet { Id = Guid.NewGuid(), Title = "Set 1", IsPublished = true, CreatedAt = DateTime.UtcNow }
            };

            _mockQuizSetRepo.Setup(r => r.GetPublishedQuizSetsAsync(pagination))
                .ReturnsAsync((quizSets, 1));

            // Act
            var result = await _quizSetService.GetPublishedQuizSetsAsync(pagination);

            // Assert
            result.Should().NotBeNull();
            result.Data.Should().HaveCount(1);
            result.Data[0].IsPublished.Should().BeTrue();
        }

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
        }

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
        }

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
        }

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
        }

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
        }

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
        }
    }
}