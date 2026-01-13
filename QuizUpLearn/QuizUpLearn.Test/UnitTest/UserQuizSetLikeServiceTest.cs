using AutoMapper;
using BusinessLogic.DTOs;
using BusinessLogic.DTOs.UserQuizSetLikeDtos;
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
    public class UserQuizSetLikeServiceTest : BaseControllerTest
    {
        private readonly Mock<IUserQuizSetLikeRepo> _mockUserQuizSetLikeRepo;
        private readonly IMapper _mapper;
        private readonly UserQuizSetLikeService _userQuizSetLikeService;

        public UserQuizSetLikeServiceTest()
        {
            _mockUserQuizSetLikeRepo = new Mock<IUserQuizSetLikeRepo>();

            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<MappingProfile>();
            }, new NullLoggerFactory());
            _mapper = mapperConfig.CreateMapper();

            _userQuizSetLikeService = new UserQuizSetLikeService(
                _mockUserQuizSetLikeRepo.Object, 
                _mapper);
        }

        [Fact]
        public async Task CreateAsync_WithValidDto_ShouldReturnResponseUserQuizSetLikeDto()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var quizSetId = Guid.NewGuid();
            var requestDto = new RequestUserQuizSetLikeDto
            {
                UserId = userId,
                QuizSetId = quizSetId
            };

            var createdEntity = new UserQuizSetLike
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                QuizSetId = quizSetId,
                CreatedAt = DateTime.UtcNow
            };

            _mockUserQuizSetLikeRepo.Setup(r => r.IsExistAsync(userId, quizSetId))
                .ReturnsAsync(false);
            _mockUserQuizSetLikeRepo.Setup(r => r.CreateAsync(It.IsAny<UserQuizSetLike>()))
                .ReturnsAsync(createdEntity);

            // Act
            var result = await _userQuizSetLikeService.CreateAsync(requestDto);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(createdEntity.Id);
            result.UserId.Should().Be(userId);
            result.QuizSetId.Should().Be(quizSetId);
            result.CreatedAt.Should().Be(createdEntity.CreatedAt);

            _mockUserQuizSetLikeRepo.Verify(r => r.IsExistAsync(userId, quizSetId), Times.Once);
            _mockUserQuizSetLikeRepo.Verify(r => r.CreateAsync(It.IsAny<UserQuizSetLike>()), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_WithValidId_ShouldReturnResponseUserQuizSetLikeDto()
        {
            // Arrange
            var id = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var quizSetId = Guid.NewGuid();
            var entity = new UserQuizSetLike
            {
                Id = id,
                UserId = userId,
                QuizSetId = quizSetId,
                CreatedAt = DateTime.UtcNow
            };

            _mockUserQuizSetLikeRepo.Setup(r => r.GetByIdAsync(id))
                .ReturnsAsync(entity);

            // Act
            var result = await _userQuizSetLikeService.GetByIdAsync(id);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(id);
            result.UserId.Should().Be(userId);
            result.QuizSetId.Should().Be(quizSetId);
            result.CreatedAt.Should().Be(entity.CreatedAt);

            _mockUserQuizSetLikeRepo.Verify(r => r.GetByIdAsync(id), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_WithNonExistentId_ShouldThrowArgumentException()
        {
            // Arrange
            var id = Guid.Empty;

            // Act
            Func<Task> act = async () => await _userQuizSetLikeService.GetByIdAsync(id);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task GetAllAsync_WithValidPagination_ShouldReturnPaginatedResponse()
        {
            // Arrange
            var pagination = new PaginationRequestDto { Page = 1, PageSize = 10 };
            var entities = new List<UserQuizSetLike>
            {
                new UserQuizSetLike 
                { 
                    Id = Guid.NewGuid(), 
                    UserId = Guid.NewGuid(), 
                    QuizSetId = Guid.NewGuid(),
                    CreatedAt = DateTime.UtcNow
                },
                new UserQuizSetLike 
                { 
                    Id = Guid.NewGuid(), 
                    UserId = Guid.NewGuid(), 
                    QuizSetId = Guid.NewGuid(),
                    CreatedAt = DateTime.UtcNow.AddMinutes(-5)
                }
            };

            _mockUserQuizSetLikeRepo.Setup(r => r.GetAllAsync(false))
                .ReturnsAsync(entities);

            // Act
            var result = await _userQuizSetLikeService.GetAllAsync(pagination, false);

            // Assert
            result.Should().NotBeNull();
            result.Data.Should().HaveCount(2);
            result.Data[0].Id.Should().Be(entities[0].Id);
            result.Data[1].Id.Should().Be(entities[1].Id);
            result.Pagination.TotalCount.Should().Be(2);

            _mockUserQuizSetLikeRepo.Verify(r => r.GetAllAsync(false), Times.Once);
        }

        [Fact]
        public async Task GetByUserIdAsync_WithValidUserId_ShouldReturnPaginatedResponse()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var pagination = new PaginationRequestDto { Page = 1, PageSize = 10 };
            var entities = new List<UserQuizSetLike>
            {
                new UserQuizSetLike 
                { 
                    Id = Guid.NewGuid(), 
                    UserId = userId, 
                    QuizSetId = Guid.NewGuid(),
                    CreatedAt = DateTime.UtcNow
                }
            };

            _mockUserQuizSetLikeRepo.Setup(r => r.GetByUserIdAsync(userId, false))
                .ReturnsAsync(entities);

            // Act
            var result = await _userQuizSetLikeService.GetByUserIdAsync(userId, pagination, false);

            // Assert
            result.Should().NotBeNull();
            result.Data.Should().HaveCount(1);
            result.Data[0].UserId.Should().Be(userId);
            result.Pagination.TotalCount.Should().Be(1);

            _mockUserQuizSetLikeRepo.Verify(r => r.GetByUserIdAsync(userId, false), Times.Once);
        }

        [Fact]
        public async Task GetByUserIdAsync_WhenUserIdIsInvalid_ShouldThrowArgumentException()
        {
            // Arrange
            var userId = Guid.Empty;
            var pagination = new PaginationRequestDto { Page = 1, PageSize = 10 };

            _mockUserQuizSetLikeRepo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<bool>()))
                .ReturnsAsync(new List<UserQuizSetLike>());

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await _userQuizSetLikeService.GetByUserIdAsync(userId, pagination));
        }

        [Fact]
        public async Task GetByQuizSetIdAsync_WithValidQuizSetId_ShouldReturnPaginatedResponse()
        {
            // Arrange
            var quizSetId = Guid.NewGuid();
            var pagination = new PaginationRequestDto { Page = 1, PageSize = 10 };
            var entities = new List<UserQuizSetLike>
            {
                new UserQuizSetLike 
                { 
                    Id = Guid.NewGuid(), 
                    UserId = Guid.NewGuid(), 
                    QuizSetId = quizSetId,
                    CreatedAt = DateTime.UtcNow
                },
                new UserQuizSetLike 
                { 
                    Id = Guid.NewGuid(), 
                    UserId = Guid.NewGuid(), 
                    QuizSetId = quizSetId,
                    CreatedAt = DateTime.UtcNow.AddMinutes(-3)
                }
            };

            _mockUserQuizSetLikeRepo.Setup(r => r.GetByQuizSetIdAsync(quizSetId, false))
                .ReturnsAsync(entities);

            // Act
            var result = await _userQuizSetLikeService.GetByQuizSetIdAsync(quizSetId, pagination, false);

            // Assert
            result.Should().NotBeNull();
            result.Data.Should().HaveCount(2);
            result.Data.All(x => x.QuizSetId == quizSetId).Should().BeTrue();
            result.Pagination.TotalCount.Should().Be(2);

            _mockUserQuizSetLikeRepo.Verify(r => r.GetByQuizSetIdAsync(quizSetId, false), Times.Once);
        }

        [Fact]
        public async Task GetByQuizSetIdAsync_WhenQuizSetIdIsInvalid_ShouldThrowArgumentException()
        {
            var quizSetId = Guid.Empty;
            var pagination = new PaginationRequestDto { Page = 1, PageSize = 10 };

            _mockUserQuizSetLikeRepo.Setup(r => r.GetByQuizSetIdAsync(quizSetId, It.IsAny<bool>()))
                .ReturnsAsync(new List<UserQuizSetLike>());

            // Act
            Func<Task> act = async () => await _userQuizSetLikeService.GetByQuizSetIdAsync(quizSetId, pagination);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task GetByUserAndQuizSetAsync_WithValidIds_ShouldReturnResponseUserQuizSetLikeDto()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var quizSetId = Guid.NewGuid();
            var entity = new UserQuizSetLike
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                QuizSetId = quizSetId,
                CreatedAt = DateTime.UtcNow
            };

            _mockUserQuizSetLikeRepo.Setup(r => r.GetByUserAndQuizSetAsync(userId, quizSetId, false))
                .ReturnsAsync(entity);

            // Act
            var result = await _userQuizSetLikeService.GetByUserAndQuizSetAsync(userId, quizSetId, false);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(entity.Id);
            result.UserId.Should().Be(userId);
            result.QuizSetId.Should().Be(quizSetId);

            _mockUserQuizSetLikeRepo.Verify(r => r.GetByUserAndQuizSetAsync(userId, quizSetId, false), Times.Once);
        }

        [Fact]
        public async Task GetByUserAndQuizSetAsync_WithNonExistentPair_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var quizSetId = Guid.NewGuid();

            _mockUserQuizSetLikeRepo.Setup(r => r.GetByUserAndQuizSetAsync(userId, quizSetId, false))
                .ReturnsAsync((UserQuizSetLike?)null);

            // Act
            var result = await _userQuizSetLikeService
                .GetByUserAndQuizSetAsync(userId, quizSetId, false);

            // Assert
            result.Should().BeNull();

            _mockUserQuizSetLikeRepo.Verify(r => r.GetByUserAndQuizSetAsync(userId, quizSetId, false), Times.Once);
        }

        [Fact]
        public async Task ToggleLikeAsync_WhenLikeExists_ShouldRemoveLike()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var quizSetId = Guid.NewGuid();
            var existingLike = new UserQuizSetLike
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                QuizSetId = quizSetId,
                CreatedAt = DateTime.UtcNow
            };

            _mockUserQuizSetLikeRepo.Setup(r => r.GetByUserAndQuizSetAsync(userId, quizSetId, false))
                .ReturnsAsync(existingLike);
            _mockUserQuizSetLikeRepo.Setup(r => r.HardDeleteAsync(existingLike.Id))
                .ReturnsAsync(true);

            // Act
            var result = await _userQuizSetLikeService.ToggleLikeAsync(userId, quizSetId);

            // Assert
            result.Should().BeTrue();
            _mockUserQuizSetLikeRepo.Verify(r => r.GetByUserAndQuizSetAsync(userId, quizSetId, false), Times.Once);
            _mockUserQuizSetLikeRepo.Verify(r => r.HardDeleteAsync(existingLike.Id), Times.Once);
            _mockUserQuizSetLikeRepo.Verify(r => r.CreateAsync(It.IsAny<UserQuizSetLike>()), Times.Never);
        }

        [Fact]
        public async Task ToggleLikeAsync_WhenLikeDoesNotExist_ShouldAddLike()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var quizSetId = Guid.NewGuid();
            var createdLike = new UserQuizSetLike
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                QuizSetId = quizSetId,
                CreatedAt = DateTime.UtcNow
            };

            _mockUserQuizSetLikeRepo.Setup(r => r.GetByUserAndQuizSetAsync(userId, quizSetId, false))
                .ReturnsAsync((UserQuizSetLike?)null);
            _mockUserQuizSetLikeRepo.Setup(r => r.CreateAsync(It.IsAny<UserQuizSetLike>()))
                .ReturnsAsync(createdLike);

            // Act
            var result = await _userQuizSetLikeService.ToggleLikeAsync(userId, quizSetId);

            // Assert
            result.Should().BeTrue();
            _mockUserQuizSetLikeRepo.Verify(r => r.GetByUserAndQuizSetAsync(userId, quizSetId, false), Times.Once);
            _mockUserQuizSetLikeRepo.Verify(r => r.CreateAsync(It.Is<UserQuizSetLike>(x => 
                x.UserId == userId && x.QuizSetId == quizSetId)), Times.Once);
            _mockUserQuizSetLikeRepo.Verify(r => r.HardDeleteAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task HardDeleteAsync_WithValidId_ShouldReturnTrue()
        {
            // Arrange
            var id = Guid.NewGuid();
            _mockUserQuizSetLikeRepo.Setup(r => r.HardDeleteAsync(id))
                .ReturnsAsync(true);

            // Act
            var result = await _userQuizSetLikeService.HardDeleteAsync(id);

            // Assert
            result.Should().BeTrue();
            _mockUserQuizSetLikeRepo.Verify(r => r.HardDeleteAsync(id), Times.Once);
        }

        [Fact]
        public async Task HardDeleteAsync_WithNonExistentId_ShouldReturnFalse()
        {
            // Arrange
            var id = Guid.NewGuid();
            _mockUserQuizSetLikeRepo.Setup(r => r.HardDeleteAsync(id))
                .ReturnsAsync(false);

            // Act
            var result = await _userQuizSetLikeService.HardDeleteAsync(id);

            // Assert
            result.Should().BeFalse();
            _mockUserQuizSetLikeRepo.Verify(r => r.HardDeleteAsync(id), Times.Once);
        }

        [Fact]
        public async Task IsExistAsync_WhenLikeExists_ShouldReturnTrue()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var quizSetId = Guid.NewGuid();
            _mockUserQuizSetLikeRepo.Setup(r => r.IsExistAsync(userId, quizSetId))
                .ReturnsAsync(true);

            // Act
            var result = await _userQuizSetLikeService.IsExistAsync(userId, quizSetId);

            // Assert
            result.Should().BeTrue();
            _mockUserQuizSetLikeRepo.Verify(r => r.IsExistAsync(userId, quizSetId), Times.Once);
        }

        [Fact]
        public async Task IsExistAsync_WhenLikeDoesNotExist_ShouldReturnFalse()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var quizSetId = Guid.NewGuid();
            _mockUserQuizSetLikeRepo.Setup(r => r.IsExistAsync(userId, quizSetId))
                .ReturnsAsync(false);

            // Act
            var result = await _userQuizSetLikeService.IsExistAsync(userId, quizSetId);

            // Assert
            result.Should().BeFalse();
            _mockUserQuizSetLikeRepo.Verify(r => r.IsExistAsync(userId, quizSetId), Times.Once);
        }

        [Fact]
        public async Task GetLikeCountByQuizSetAsync_WithValidQuizSetId_ShouldReturnCount()
        {
            // Arrange
            var quizSetId = Guid.NewGuid();
            var expectedCount = 5;
            _mockUserQuizSetLikeRepo.Setup(r => r.GetLikeCountByQuizSetAsync(quizSetId))
                .ReturnsAsync(expectedCount);

            // Act
            var result = await _userQuizSetLikeService.GetLikeCountByQuizSetAsync(quizSetId);

            // Assert
            result.Should().Be(expectedCount);
            _mockUserQuizSetLikeRepo.Verify(r => r.GetLikeCountByQuizSetAsync(quizSetId), Times.Once);
        }

        [Fact]
        public async Task GetLikeCountByQuizSetAsync_WithQuizSetHavingNoLikes_ShouldReturnZero()
        {
            // Arrange
            var quizSetId = Guid.NewGuid();
            _mockUserQuizSetLikeRepo.Setup(r => r.GetLikeCountByQuizSetAsync(quizSetId))
                .ReturnsAsync(0);

            // Act
            var result = await _userQuizSetLikeService.GetLikeCountByQuizSetAsync(quizSetId);

            // Assert
            result.Should().Be(0);
            _mockUserQuizSetLikeRepo.Verify(r => r.GetLikeCountByQuizSetAsync(quizSetId), Times.Once);
        }
    }
}