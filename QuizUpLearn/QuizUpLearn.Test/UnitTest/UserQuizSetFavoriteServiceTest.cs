using AutoMapper;
using BusinessLogic.DTOs;
using BusinessLogic.DTOs.UserQuizSetFavoriteDtos;
using BusinessLogic.MappingProfile;
using BusinessLogic.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Repository.Entities;
using Repository.Interfaces;

namespace QuizUpLearn.Test.UnitTest
{
    public class UserQuizSetFavoriteServiceTest
    {
        private readonly Mock<IUserQuizSetFavoriteRepo> _mockUserQuizSetFavoriteRepo;
        private readonly IMapper _mapper;
        private readonly UserQuizSetFavoriteService _userQuizSetFavoriteService;

        public UserQuizSetFavoriteServiceTest()
        {
            _mockUserQuizSetFavoriteRepo = new Mock<IUserQuizSetFavoriteRepo>();

            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<MappingProfile>();
            }, new NullLoggerFactory());
            _mapper = mapperConfig.CreateMapper();

            _userQuizSetFavoriteService = new UserQuizSetFavoriteService(
                _mockUserQuizSetFavoriteRepo.Object,
                _mapper);
        }

        [Fact]
        public async Task CreateAsync_WithValidData_ShouldReturnResponseDto()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var quizSetId = Guid.NewGuid();
            var requestDto = new RequestUserQuizSetFavoriteDto
            {
                UserId = userId,
                QuizSetId = quizSetId
            };

            var createdEntity = new UserQuizSetFavorite
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                QuizSetId = quizSetId,
                CreatedAt = DateTime.UtcNow
            };

            _mockUserQuizSetFavoriteRepo.Setup(r => r.IsExistAsync(userId, quizSetId))
                .ReturnsAsync(false);
            _mockUserQuizSetFavoriteRepo.Setup(r => r.CreateAsync(It.IsAny<UserQuizSetFavorite>()))
                .ReturnsAsync(createdEntity);

            // Act
            var result = await _userQuizSetFavoriteService.CreateAsync(requestDto);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(createdEntity.Id);
            result.UserId.Should().Be(userId);
            result.QuizSetId.Should().Be(quizSetId);
            result.CreatedAt.Should().Be(createdEntity.CreatedAt);

            _mockUserQuizSetFavoriteRepo.Verify(r => r.IsExistAsync(userId, quizSetId), Times.Once);
            _mockUserQuizSetFavoriteRepo.Verify(r => r.CreateAsync(It.Is<UserQuizSetFavorite>(e =>
                e.UserId == userId && e.QuizSetId == quizSetId)), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_WhenFavoriteAlreadyExists_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var quizSetId = Guid.NewGuid();
            var requestDto = new RequestUserQuizSetFavoriteDto
            {
                UserId = userId,
                QuizSetId = quizSetId
            };

            _mockUserQuizSetFavoriteRepo.Setup(r => r.IsExistAsync(userId, quizSetId))
                .ReturnsAsync(true);

            // Act
            Func<Task> act = async () => await _userQuizSetFavoriteService.CreateAsync(requestDto);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("User has already favorited this quiz set");

            _mockUserQuizSetFavoriteRepo.Verify(r => r.IsExistAsync(userId, quizSetId), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_WithExistingId_ShouldReturnResponseDto()
        {
            // Arrange
            var favoriteId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var quizSetId = Guid.NewGuid();
            
            var entity = new UserQuizSetFavorite
            {
                Id = favoriteId,
                UserId = userId,
                QuizSetId = quizSetId,
                CreatedAt = DateTime.UtcNow
            };

            _mockUserQuizSetFavoriteRepo.Setup(r => r.GetByIdAsync(favoriteId))
                .ReturnsAsync(entity);

            // Act
            var result = await _userQuizSetFavoriteService.GetByIdAsync(favoriteId);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(favoriteId);
            result.UserId.Should().Be(userId);
            result.QuizSetId.Should().Be(quizSetId);
            result.CreatedAt.Should().Be(entity.CreatedAt);

            _mockUserQuizSetFavoriteRepo.Verify(r => r.GetByIdAsync(favoriteId), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_WithNonExistingId_ShouldReturnNull()
        {
            // Arrange
            var favoriteId = Guid.NewGuid();

            _mockUserQuizSetFavoriteRepo.Setup(r => r.GetByIdAsync(favoriteId))
                .ReturnsAsync((UserQuizSetFavorite?)null);

            // Act
            var result = await _userQuizSetFavoriteService.GetByIdAsync(favoriteId);

            // Assert
            result.Should().BeNull();

            _mockUserQuizSetFavoriteRepo.Verify(r => r.GetByIdAsync(favoriteId), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_WhenIdIsInvalid_ShouldThrowArgumentException()
        {
            // Arrange
            var favoriteId = Guid.Empty;

            // Act
            Func<Task> act = async () =>
                await _userQuizSetFavoriteService.GetByIdAsync(favoriteId);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>();

            _mockUserQuizSetFavoriteRepo.Verify(
                r => r.GetByIdAsync(It.IsAny<Guid>()),
                Times.Never);
        }

        [Fact]
        public async Task GetAllAsync_WithValidPagination_ShouldReturnPaginatedResponse()
        {
            // Arrange
            var pagination = new PaginationRequestDto
            {
                Page = 1,
                PageSize = 10
            };

            var favorites = new List<UserQuizSetFavorite>
            {
                new UserQuizSetFavorite
                {
                    Id = Guid.NewGuid(),
                    UserId = Guid.NewGuid(),
                    QuizSetId = Guid.NewGuid(),
                    CreatedAt = DateTime.UtcNow.AddHours(1)
                },
                new UserQuizSetFavorite
                {
                    Id = Guid.NewGuid(),
                    UserId = Guid.NewGuid(),
                    QuizSetId = Guid.NewGuid(),
                    CreatedAt = DateTime.UtcNow.AddHours(2)
                }
            };

            _mockUserQuizSetFavoriteRepo.Setup(r => r.GetAllAsync(false))
                .ReturnsAsync(favorites);

            // Act
            var result = await _userQuizSetFavoriteService.GetAllAsync(pagination, false);

            // Assert
            result.Should().NotBeNull();
            result.Data.Should().HaveCount(2);
            result.Pagination.TotalCount.Should().Be(2);
            //Default order = descending CreatedAt
            var expectedOrder = favorites.OrderByDescending(f => f.CreatedAt).ToList();
            result.Data[0].Id.Should().Be(expectedOrder[0].Id);
            result.Data[1].Id.Should().Be(expectedOrder[1].Id);

            _mockUserQuizSetFavoriteRepo.Verify(r => r.GetAllAsync(false), Times.Once);
        }

        [Fact]
        public async Task GetAllAsync_WithIncludeDeletedTrue_ShouldReturnPaginatedResponse()
        {
            // Arrange
            var pagination = new PaginationRequestDto
            {
                Page = 1,
                PageSize = 10
            };

            var favorites = new List<UserQuizSetFavorite>
            {
                new UserQuizSetFavorite
                {
                    Id = Guid.NewGuid(),
                    UserId = Guid.NewGuid(),
                    QuizSetId = Guid.NewGuid(),
                    CreatedAt = DateTime.UtcNow.AddHours(1),
                    DeletedAt = null // Active favorite
                },
                new UserQuizSetFavorite
                {
                    Id = Guid.NewGuid(),
                    UserId = Guid.NewGuid(),
                    QuizSetId = Guid.NewGuid(),
                    CreatedAt = DateTime.UtcNow.AddHours(2),
                    DeletedAt = DateTime.UtcNow.AddMinutes(-30) // Deleted favorite
                }
            };

            _mockUserQuizSetFavoriteRepo.Setup(r => r.GetAllAsync(true))
                .ReturnsAsync(favorites);

            // Act
            var result = await _userQuizSetFavoriteService.GetAllAsync(pagination, true);

            // Assert
            result.Should().NotBeNull();
            result.Data.Should().HaveCount(2);
            result.Pagination.TotalCount.Should().Be(2);
            // Default order = descending CreatedAt
            var expectedOrder = favorites.OrderByDescending(f => f.CreatedAt).ToList();
            result.Data[0].Id.Should().Be(expectedOrder[0].Id);
            result.Data[1].Id.Should().Be(expectedOrder[1].Id);

            _mockUserQuizSetFavoriteRepo.Verify(r => r.GetAllAsync(true), Times.Once);
        }

        [Fact]
        public async Task GetByUserIdAsync_WithValidUserId_ShouldReturnPaginatedResponse()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var pagination = new PaginationRequestDto
            {
                Page = 1,
                PageSize = 10
            };

            var favorites = new List<UserQuizSetFavorite>
            {
                new UserQuizSetFavorite
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    QuizSetId = Guid.NewGuid(),
                    CreatedAt = DateTime.UtcNow.AddDays(-1)
                }
            };

            _mockUserQuizSetFavoriteRepo.Setup(r => r.GetByUserIdAsync(userId, false))
                .ReturnsAsync(favorites);

            // Act
            var result = await _userQuizSetFavoriteService.GetByUserIdAsync(userId, pagination, false);

            // Assert
            result.Should().NotBeNull();
            result.Data.Should().HaveCount(1);
            result.Data[0].UserId.Should().Be(userId);
            result.Pagination.TotalCount.Should().Be(1);

            _mockUserQuizSetFavoriteRepo.Verify(r => r.GetByUserIdAsync(userId, false), Times.Once);
        }

        [Fact]
        public async Task GetByUserIdAsync_WhenUserIdIsInvalid_ShouldThrowArgumentException()
        {
            // Arrange
            var userId = Guid.Empty;
            var pagination = new PaginationRequestDto
            {
                Page = 1,
                PageSize = 10
            };

            _mockUserQuizSetFavoriteRepo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<bool>()))
                .ReturnsAsync(new List<UserQuizSetFavorite>());

            // Act
            Func<Task> act = async () => await _userQuizSetFavoriteService.GetByUserIdAsync(userId, pagination);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task GetByQuizSetIdAsync_WithValidQuizSetId_ShouldReturnPaginatedResponse()
        {
            // Arrange
            var quizSetId = Guid.NewGuid();
            var pagination = new PaginationRequestDto
            {
                Page = 1,
                PageSize = 10
            };

            var favorites = new List<UserQuizSetFavorite>
            {
                new UserQuizSetFavorite
                {
                    Id = Guid.NewGuid(),
                    UserId = Guid.NewGuid(),
                    QuizSetId = quizSetId,
                    CreatedAt = DateTime.UtcNow.AddDays(-1)
                },
                new UserQuizSetFavorite
                {
                    Id = Guid.NewGuid(),
                    UserId = Guid.NewGuid(),
                    QuizSetId = quizSetId,
                    CreatedAt = DateTime.UtcNow.AddDays(-2)
                }
            };

            _mockUserQuizSetFavoriteRepo.Setup(r => r.GetByQuizSetIdAsync(quizSetId, false))
                .ReturnsAsync(favorites);

            // Act
            var result = await _userQuizSetFavoriteService.GetByQuizSetIdAsync(quizSetId, pagination, false);

            // Assert
            result.Should().NotBeNull();
            result.Data.Should().HaveCount(2);
            result.Data.Should().OnlyContain(f => f.QuizSetId == quizSetId);
            result.Pagination.TotalCount.Should().Be(2);

            _mockUserQuizSetFavoriteRepo.Verify(r => r.GetByQuizSetIdAsync(quizSetId, false), Times.Once);
        }

        [Fact]
        public async Task GetByQuizSetIdAsync_WhenQuizSetIdIsInvalid_ShouldThrowArgumentException()
        {
            // Arrange
            var quizSetId = Guid.Empty;
            var pagination = new PaginationRequestDto
            {
                Page = 1,
                PageSize = 10
            };

            // Act
            Func<Task> act = async () => await _userQuizSetFavoriteService.GetByQuizSetIdAsync(quizSetId, pagination);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task GetByUserAndQuizSetAsync_WithExistingFavorite_ShouldReturnResponseDto()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var quizSetId = Guid.NewGuid();
            var favoriteId = Guid.NewGuid();

            var favorite = new UserQuizSetFavorite
            {
                Id = favoriteId,
                UserId = userId,
                QuizSetId = quizSetId,
                CreatedAt = DateTime.UtcNow
            };

            _mockUserQuizSetFavoriteRepo.Setup(r => r.GetByUserAndQuizSetAsync(userId, quizSetId, false))
                .ReturnsAsync(favorite);

            // Act
            var result = await _userQuizSetFavoriteService.GetByUserAndQuizSetAsync(userId, quizSetId, false);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(favoriteId);
            result.UserId.Should().Be(userId);
            result.QuizSetId.Should().Be(quizSetId);

            _mockUserQuizSetFavoriteRepo.Verify(r => r.GetByUserAndQuizSetAsync(userId, quizSetId, false), Times.Once);
        }

        [Fact]
        public async Task GetByUserAndQuizSetAsync_WithNonExistingFavorite_ShouldReturnNull()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var quizSetId = Guid.NewGuid();

            _mockUserQuizSetFavoriteRepo.Setup(r => r.GetByUserAndQuizSetAsync(userId, quizSetId, false))
                .ReturnsAsync((UserQuizSetFavorite?)null);

            // Act
            var result = await _userQuizSetFavoriteService.GetByUserAndQuizSetAsync(userId, quizSetId, false);

            // Assert
            result.Should().BeNull();

            _mockUserQuizSetFavoriteRepo.Verify(r => r.GetByUserAndQuizSetAsync(userId, quizSetId, false), Times.Once);
        }

        [Fact]
        public async Task GetByUserAndQuizSetAsync_WhenFavoriteDoesNotExist_ShouldReturnNull()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var quizSetId = Guid.NewGuid();

            _mockUserQuizSetFavoriteRepo.Setup(r => r.GetByUserAndQuizSetAsync(userId, quizSetId, It.IsAny<bool>()))
                .ReturnsAsync((UserQuizSetFavorite?)null);

            // Act
            var result = await _userQuizSetFavoriteService.GetByUserAndQuizSetAsync(userId, quizSetId);

            // Assert
            result.Should().BeNull();

            _mockUserQuizSetFavoriteRepo.Verify(r => r.GetByUserAndQuizSetAsync(userId, quizSetId, It.IsAny<bool>()), Times.Once);
        }

        [Fact]
        public async Task GetByUserAndQuizSetAsync_WithEmptyIds_ShouldThrowArgumentException()
        {
            // Arrange
            var userId = Guid.Empty;
            var quizSetId = Guid.Empty;

            // Act
            Func<Task> act = async () => await _userQuizSetFavoriteService
                .GetByUserAndQuizSetAsync(userId, quizSetId, false);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task ToggleFavoriteAsync_WhenFavoriteDoesNotExist_ShouldCreateFavoriteAndReturnTrue()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var quizSetId = Guid.NewGuid();

            var createdEntity = new UserQuizSetFavorite
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                QuizSetId = quizSetId,
                CreatedAt = DateTime.UtcNow
            };

            _mockUserQuizSetFavoriteRepo.Setup(r => r.GetByUserAndQuizSetAsync(userId, quizSetId, It.IsAny<bool>()))
                .ReturnsAsync((UserQuizSetFavorite?)null);
            _mockUserQuizSetFavoriteRepo.Setup(r => r.CreateAsync(It.IsAny<UserQuizSetFavorite>()))
                .ReturnsAsync(createdEntity);

            // Act
            var result = await _userQuizSetFavoriteService.ToggleFavoriteAsync(userId, quizSetId);

            // Assert
            result.Should().BeTrue();

            _mockUserQuizSetFavoriteRepo.Verify(r => r.GetByUserAndQuizSetAsync(userId, quizSetId, It.IsAny<bool>()), Times.Once);
            _mockUserQuizSetFavoriteRepo.Verify(r => r.CreateAsync(It.Is<UserQuizSetFavorite>(e =>
                e.UserId == userId && e.QuizSetId == quizSetId)), Times.Once);
            _mockUserQuizSetFavoriteRepo.Verify(r => r.HardDeleteAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task ToggleFavoriteAsync_WhenFavoriteExists_ShouldRemoveFavoriteAndReturnTrue()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var quizSetId = Guid.NewGuid();
            var favoriteId = Guid.NewGuid();

            var existingFavorite = new UserQuizSetFavorite
            {
                Id = favoriteId,
                UserId = userId,
                QuizSetId = quizSetId,
                CreatedAt = DateTime.UtcNow
            };

            _mockUserQuizSetFavoriteRepo.Setup(r => r.GetByUserAndQuizSetAsync(userId, quizSetId, It.IsAny<bool>()))
                .ReturnsAsync(existingFavorite);
            _mockUserQuizSetFavoriteRepo.Setup(r => r.HardDeleteAsync(favoriteId))
                .ReturnsAsync(true);

            // Act
            var result = await _userQuizSetFavoriteService.ToggleFavoriteAsync(userId, quizSetId);

            // Assert
            result.Should().BeTrue();

            _mockUserQuizSetFavoriteRepo.Verify(r => r.GetByUserAndQuizSetAsync(userId, quizSetId, It.IsAny<bool>()), Times.Once);
            _mockUserQuizSetFavoriteRepo.Verify(r => r.HardDeleteAsync(favoriteId), Times.Once);
            _mockUserQuizSetFavoriteRepo.Verify(r => r.CreateAsync(It.IsAny<UserQuizSetFavorite>()), Times.Never);
        }

        [Fact]
        public async Task ToggleFavoriteAsync_WhenRepositoryThrowsException_ShouldPropagateException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var quizSetId = Guid.NewGuid();

            _mockUserQuizSetFavoriteRepo.Setup(r => r.GetByUserAndQuizSetAsync(userId, quizSetId, It.IsAny<bool>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            Func<Task> act = async () => await _userQuizSetFavoriteService.ToggleFavoriteAsync(userId, quizSetId);

            // Assert
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Database error");

            _mockUserQuizSetFavoriteRepo.Verify(r => r.GetByUserAndQuizSetAsync(userId, quizSetId, It.IsAny<bool>()), Times.Once);
        }

        [Fact]
        public async Task HardDeleteAsync_WithExistingId_ShouldReturnTrue()
        {
            // Arrange
            var favoriteId = Guid.NewGuid();

            _mockUserQuizSetFavoriteRepo.Setup(r => r.HardDeleteAsync(favoriteId))
                .ReturnsAsync(true);

            // Act
            var result = await _userQuizSetFavoriteService.HardDeleteAsync(favoriteId);

            // Assert
            result.Should().BeTrue();

            _mockUserQuizSetFavoriteRepo.Verify(r => r.HardDeleteAsync(favoriteId), Times.Once);
        }

        [Fact]
        public async Task HardDeleteAsync_WithNonExistingId_ShouldReturnFalse()
        {
            // Arrange
            var favoriteId = Guid.NewGuid();

            _mockUserQuizSetFavoriteRepo.Setup(r => r.HardDeleteAsync(favoriteId))
                .ReturnsAsync(false);

            // Act
            var result = await _userQuizSetFavoriteService.HardDeleteAsync(favoriteId);

            // Assert
            result.Should().BeFalse();

            _mockUserQuizSetFavoriteRepo.Verify(r => r.HardDeleteAsync(favoriteId), Times.Once);
        }

        [Fact]
        public async Task HardDeleteAsync_WithInvalidId_ShouldThrowArgumentException()
        {
            // Arrange
            var favoriteId = Guid.Empty;

            // Act
            Func<Task> act = async () => await _userQuizSetFavoriteService.HardDeleteAsync(favoriteId);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task IsExistAsync_WhenFavoriteExists_ShouldReturnTrue()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var quizSetId = Guid.NewGuid();

            _mockUserQuizSetFavoriteRepo.Setup(r => r.IsExistAsync(userId, quizSetId))
                .ReturnsAsync(true);

            // Act
            var result = await _userQuizSetFavoriteService.IsExistAsync(userId, quizSetId);

            // Assert
            result.Should().BeTrue();

            _mockUserQuizSetFavoriteRepo.Verify(r => r.IsExistAsync(userId, quizSetId), Times.Once);
        }

        [Fact]
        public async Task IsExistAsync_WhenFavoriteDoesNotExist_ShouldReturnFalse()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var quizSetId = Guid.NewGuid();

            _mockUserQuizSetFavoriteRepo.Setup(r => r.IsExistAsync(userId, quizSetId))
                .ReturnsAsync(false);

            // Act
            var result = await _userQuizSetFavoriteService.IsExistAsync(userId, quizSetId);

            // Assert
            result.Should().BeFalse();

            _mockUserQuizSetFavoriteRepo.Verify(r => r.IsExistAsync(userId, quizSetId), Times.Once);
        }

        [Fact]
        public async Task IsExistAsync_WhenRepositoryThrowsException_ShouldPropagateException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var quizSetId = Guid.NewGuid();

            _mockUserQuizSetFavoriteRepo.Setup(r => r.IsExistAsync(userId, quizSetId))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            Func<Task> act = async () => await _userQuizSetFavoriteService.IsExistAsync(userId, quizSetId);

            // Assert
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Database error");

            _mockUserQuizSetFavoriteRepo.Verify(r => r.IsExistAsync(userId, quizSetId), Times.Once);
        }

        [Fact]
        public async Task GetFavoriteCountByQuizSetAsync_WithValidQuizSetId_ShouldReturnCount()
        {
            // Arrange
            var quizSetId = Guid.NewGuid();
            var expectedCount = 5;

            _mockUserQuizSetFavoriteRepo.Setup(r => r.GetFavoriteCountByQuizSetAsync(quizSetId))
                .ReturnsAsync(expectedCount);

            // Act
            var result = await _userQuizSetFavoriteService.GetFavoriteCountByQuizSetAsync(quizSetId);

            // Assert
            result.Should().Be(expectedCount);

            _mockUserQuizSetFavoriteRepo.Verify(r => r.GetFavoriteCountByQuizSetAsync(quizSetId), Times.Once);
        }

        [Fact]
        public async Task GetFavoriteCountByQuizSetAsync_WithNoFavorites_ShouldReturnZero()
        {
            // Arrange
            var quizSetId = Guid.NewGuid();

            _mockUserQuizSetFavoriteRepo.Setup(r => r.GetFavoriteCountByQuizSetAsync(quizSetId))
                .ReturnsAsync(0);

            // Act
            var result = await _userQuizSetFavoriteService.GetFavoriteCountByQuizSetAsync(quizSetId);

            // Assert
            result.Should().Be(0);

            _mockUserQuizSetFavoriteRepo.Verify(r => r.GetFavoriteCountByQuizSetAsync(quizSetId), Times.Once);
        }

        [Fact]
        public async Task GetFavoriteCountByQuizSetAsync_WithNonExistQuizSetId_ShouldReturnZero()
        {
            // Arrange
            var quizSetId = Guid.NewGuid();

            _mockUserQuizSetFavoriteRepo.Setup(r => r.GetFavoriteCountByQuizSetAsync(quizSetId))
                .ReturnsAsync(0);

            // Act
            var result = await _userQuizSetFavoriteService.GetFavoriteCountByQuizSetAsync(quizSetId);

            // Assert
            result.Should().Be(0);

            _mockUserQuizSetFavoriteRepo.Verify(r => r.GetFavoriteCountByQuizSetAsync(quizSetId), Times.Once);
        }
    }
}