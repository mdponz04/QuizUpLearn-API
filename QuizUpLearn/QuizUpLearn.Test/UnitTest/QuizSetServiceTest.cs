using AutoMapper;
using BusinessLogic.DTOs;
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

            // Setup real AutoMapper with the actual mapping profile
            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<MappingProfile>();
            }, new NullLoggerFactory());
            _mapper = mapperConfig.CreateMapper();

            _quizSetService = new QuizSetService(
                _mockQuizSetRepo.Object,
                _mapper);
        }

        [Fact]
        public async Task CreateQuizSetAsync_WithValidData_ShouldReturnQuizSetResponseDto()
        {
            // Arrange
            var requestDto = new QuizSetRequestDto
            {
                Title = "Test Quiz Set",
                Description = "Test Description",
                QuizSetType = QuizSetTypeEnum.Practice,
                CreatedBy = Guid.NewGuid(),
                IsPublished = false,
                IsPremiumOnly = false
            };

            var createdQuizSet = new QuizSet
            {
                Id = Guid.NewGuid(),
                Title = requestDto.Title,
                Description = requestDto.Description,
                QuizSetType = requestDto.QuizSetType!.Value,
                CreatedBy = requestDto.CreatedBy!.Value,
                IsPublished = requestDto.IsPublished ?? false,
                IsPremiumOnly = requestDto.IsPremiumOnly ?? false,
                CreatedAt = DateTime.UtcNow
            };

            _mockQuizSetRepo.Setup(r => r.CreateQuizSetAsync(It.IsAny<QuizSet>()))
                .ReturnsAsync(createdQuizSet);

            // Act
            var result = await _quizSetService.CreateQuizSetAsync(requestDto);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(createdQuizSet.Id);
            result.Title.Should().Be(requestDto.Title);
            result.Description.Should().Be(requestDto.Description);
            result.QuizSetType.Should().Be(requestDto.QuizSetType.Value);

            _mockQuizSetRepo.Verify(r => r.CreateQuizSetAsync(It.Is<QuizSet>(q =>
                q.Title == requestDto.Title &&
                q.CreatedBy == requestDto.CreatedBy.Value)), Times.Once);
        }

        [Fact]
        public async Task CreateQuizSetAsync_WithNullCreatedBy_ShouldThrowException()
        {
            // Arrange
            var requestDto = new QuizSetRequestDto
            {
                Title = "Test Quiz Set",
                CreatedBy = null
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await _quizSetService.CreateQuizSetAsync(requestDto));

            _mockQuizSetRepo.Verify(r => r.CreateQuizSetAsync(It.IsAny<QuizSet>()), Times.Never);
        }

        [Fact]
        public async Task CreateQuizSetAsync_WithEmptyCreatedBy_ShouldThrowException()
        {
            // Arrange
            var requestDto = new QuizSetRequestDto
            {
                Title = "Test Quiz Set",
                CreatedBy = Guid.Empty
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await _quizSetService.CreateQuizSetAsync(requestDto));

            _mockQuizSetRepo.Verify(r => r.CreateQuizSetAsync(It.IsAny<QuizSet>()), Times.Never);
        }

        [Fact]
        public async Task GetQuizSetByIdAsync_WithValidId_ShouldReturnQuizSetResponseDto()
        {
            // Arrange
            var quizSetId = Guid.NewGuid();
            var quizSet = new QuizSet
            {
                Id = quizSetId,
                Title = "Test Quiz Set",
                Description = "Test Description",
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
            result.Description.Should().Be(quizSet.Description);

            _mockQuizSetRepo.Verify(r => r.GetQuizSetByIdAsync(quizSetId), Times.Once);
        }

        [Fact]
        public async Task GetAllQuizSetsAsync_WithValidPagination_ShouldReturnPagedResponse()
        {
            // Arrange
            var pagination = new PaginationRequestDto
            {
                Page = 1,
                PageSize = 10
            };

            var quizSets = new List<QuizSet>
            {
                new QuizSet
                {
                    Id = Guid.NewGuid(),
                    Title = "Quiz Set 1",
                    QuizSetType = QuizSetTypeEnum.Practice,
                    CreatedBy = Guid.NewGuid(),
                    CreatedAt = DateTime.UtcNow
                },
                new QuizSet
                {
                    Id = Guid.NewGuid(),
                    Title = "Quiz Set 2",
                    QuizSetType = QuizSetTypeEnum.Practice,
                    CreatedBy = Guid.NewGuid(),
                    CreatedAt = DateTime.UtcNow
                }
            };

            _mockQuizSetRepo.Setup(r => r.GetAllQuizSetsAsync(
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<bool?>(),
                It.IsAny<bool?>(),
                It.IsAny<bool?>(),
                It.IsAny<QuizSetTypeEnum?>()))
                .ReturnsAsync(quizSets);

            // Act
            var result = await _quizSetService.GetAllQuizSetsAsync(pagination);

            // Assert
            result.Should().NotBeNull();
            result.Data.Should().NotBeNull();
            result.Pagination.TotalCount.Should().Be(2);

            _mockQuizSetRepo.Verify(r => r.GetAllQuizSetsAsync(
                pagination.SearchTerm,
                pagination.SortBy,
                pagination.SortDirection,
                It.IsAny<bool?>(),
                It.IsAny<bool?>(),
                It.IsAny<bool?>(),
                It.IsAny<QuizSetTypeEnum?>()), Times.Once);
        }

        [Fact]
        public async Task GetQuizSetsByCreatorAsync_WithValidCreatorId_ShouldReturnPagedResponse()
        {
            // Arrange
            var creatorId = Guid.NewGuid();
            var pagination = new PaginationRequestDto
            {
                Page = 1,
                PageSize = 10
            };

            var quizSets = new List<QuizSet>
            {
                new QuizSet
                {
                    Id = Guid.NewGuid(),
                    Title = "My Quiz Set",
                    CreatedBy = creatorId,
                    CreatedAt = DateTime.UtcNow
                }
            };

            _mockQuizSetRepo.Setup(r => r.GetQuizSetsByCreatorAsync(
                creatorId,
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<bool?>(),
                It.IsAny<bool?>(),
                It.IsAny<bool?>(),
                It.IsAny<QuizSetTypeEnum?>()))
                .ReturnsAsync(quizSets);

            // Act
            var result = await _quizSetService.GetQuizSetsByCreatorAsync(creatorId, pagination);

            // Assert
            result.Should().NotBeNull();
            result.Data.Should().NotBeNull();
            result.Pagination.TotalCount.Should().Be(1);

            _mockQuizSetRepo.Verify(r => r.GetQuizSetsByCreatorAsync(
                creatorId,
                pagination.SearchTerm,
                pagination.SortBy,
                pagination.SortDirection,
                It.IsAny<bool?>(),
                It.IsAny<bool?>(),
                It.IsAny<bool?>(),
                It.IsAny<QuizSetTypeEnum?>()), Times.Once);
        }

        [Fact]
        public async Task GetPublishedQuizSetsAsync_WithValidPagination_ShouldReturnPagedResponse()
        {
            // Arrange
            var pagination = new PaginationRequestDto
            {
                Page = 1,
                PageSize = 10
            };

            var quizSets = new List<QuizSet>
            {
                new QuizSet
                {
                    Id = Guid.NewGuid(),
                    Title = "Published Quiz Set",
                    IsPublished = true,
                    CreatedBy = Guid.NewGuid(),
                    CreatedAt = DateTime.UtcNow
                }
            };

            _mockQuizSetRepo.Setup(r => r.GetPublishedQuizSetsAsync(
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<bool?>(),
                It.IsAny<QuizSetTypeEnum?>()))
                .ReturnsAsync(quizSets);

            // Act
            var result = await _quizSetService.GetPublishedQuizSetsAsync(pagination);

            // Assert
            result.Should().NotBeNull();
            result.Data.Should().NotBeNull();
            result.Pagination.TotalCount.Should().Be(1);

            _mockQuizSetRepo.Verify(r => r.GetPublishedQuizSetsAsync(
                pagination.SearchTerm,
                pagination.SortBy,
                pagination.SortDirection,
                It.IsAny<bool?>(),
                It.IsAny<QuizSetTypeEnum?>()), Times.Once);
        }

        [Fact]
        public async Task UpdateQuizSetAsync_WithValidData_ShouldReturnUpdatedQuizSetResponseDto()
        {
            // Arrange
            var quizSetId = Guid.NewGuid();
            var requestDto = new QuizSetRequestDto
            {
                Title = "Updated Quiz Set",
                Description = "Updated Description",
                QuizSetType = QuizSetTypeEnum.Practice,
                CreatedBy = Guid.NewGuid(),
                IsPublished = true,
                IsPremiumOnly = true
            };

            var updatedQuizSet = new QuizSet
            {
                Id = quizSetId,
                Title = requestDto.Title,
                Description = requestDto.Description,
                QuizSetType = requestDto.QuizSetType!.Value,
                CreatedBy = requestDto.CreatedBy!.Value,
                IsPublished = requestDto.IsPublished ?? false,
                IsPremiumOnly = requestDto.IsPremiumOnly ?? false,
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                UpdatedAt = DateTime.UtcNow
            };

            _mockQuizSetRepo.Setup(r => r.UpdateQuizSetAsync(quizSetId, It.IsAny<QuizSet>()))
                .ReturnsAsync(updatedQuizSet);

            // Act
            var result = await _quizSetService.UpdateQuizSetAsync(quizSetId, requestDto);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(quizSetId);
            result.Title.Should().Be(requestDto.Title);
            result.Description.Should().Be(requestDto.Description);

            _mockQuizSetRepo.Verify(r => r.UpdateQuizSetAsync(quizSetId, It.IsAny<QuizSet>()), Times.Once);
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
            _mockQuizSetRepo.Verify(r => r.SoftDeleteQuizSetAsync(quizSetId), Times.Once);
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
            _mockQuizSetRepo.Verify(r => r.HardDeleteQuizSetAsync(quizSetId), Times.Once);
        }

        [Fact]
        public async Task RestoreQuizSetAsync_WithValidId_ShouldReturnQuizSetResponseDto()
        {
            // Arrange
            var quizSetId = Guid.NewGuid();
            var restoredQuizSet = new QuizSet
            {
                Id = quizSetId,
                Title = "Restored Quiz Set",
                CreatedBy = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                DeletedAt = null
            };

            _mockQuizSetRepo.Setup(r => r.RestoreQuizSetAsync(quizSetId))
                .ReturnsAsync(restoredQuizSet);

            // Act
            var result = await _quizSetService.RestoreQuizSetAsync(quizSetId);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(quizSetId);
            result.Title.Should().Be(restoredQuizSet.Title);

            _mockQuizSetRepo.Verify(r => r.RestoreQuizSetAsync(quizSetId), Times.Once);
        }

        [Fact]
        public async Task RequestValidateByModAsync_WithValidId_ShouldReturnTrue()
        {
            // Arrange
            var quizSetId = Guid.NewGuid();
            _mockQuizSetRepo.Setup(r => r.RequestValidateByMod(quizSetId))
                .ReturnsAsync(true);

            // Act
            var result = await _quizSetService.RequestValidateByModAsync(quizSetId);

            // Assert
            result.Should().BeTrue();
            _mockQuizSetRepo.Verify(r => r.RequestValidateByMod(quizSetId), Times.Once);
        }

        [Fact]
        public async Task ValidateQuizSetAsync_WithValidId_ShouldReturnTrue()
        {
            // Arrange
            var quizSetId = Guid.NewGuid();
            _mockQuizSetRepo.Setup(r => r.ValidateQuizSet(quizSetId))
                .ReturnsAsync(true);

            // Act
            var result = await _quizSetService.ValidateQuizSetAsync(quizSetId);

            // Assert
            result.Should().BeTrue();
            _mockQuizSetRepo.Verify(r => r.ValidateQuizSet(quizSetId), Times.Once);
        }
    }
}

