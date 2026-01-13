using AutoMapper;
using BusinessLogic.DTOs;
using BusinessLogic.DTOs.UserWeakPointDtos;
using BusinessLogic.MappingProfile;
using BusinessLogic.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Repository.Entities;
using Repository.Interfaces;

namespace QuizUpLearn.Test.UnitTest
{
    public class UserWeakPointServiceTest : BaseControllerTest
    {
        private readonly Mock<IUserWeakPointRepo> _mockUserWeakPointRepo;
        private readonly IMapper _mapper;
        private readonly UserWeakPointService _userWeakPointService;

        public UserWeakPointServiceTest()
        {
            _mockUserWeakPointRepo = new Mock<IUserWeakPointRepo>();

            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<MappingProfile>();
            }, new NullLoggerFactory());
            _mapper = mapperConfig.CreateMapper();

            _userWeakPointService = new UserWeakPointService(_mockUserWeakPointRepo.Object, _mapper);
        }

        [Fact]
        public async Task GetByUserIdAsync_WithValidUserId_ShouldReturnPaginatedUserWeakPoints()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var pagination = new PaginationRequestDto { Page = 1, PageSize = 10 };
            var userWeakPoints = new List<UserWeakPoint>
            {
                new UserWeakPoint
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    WeakPoint = "Grammar - Present Perfect",
                    ToeicPart = "Part 5",
                    DifficultyLevel = "Medium",
                    Advice = "Practice more present perfect exercises",
                    CreatedAt = DateTime.UtcNow
                },
                new UserWeakPoint
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    WeakPoint = "Vocabulary - Business Terms",
                    ToeicPart = "Part 6",
                    DifficultyLevel = "Hard",
                    Advice = "Focus on common business vocabulary",
                    CreatedAt = DateTime.UtcNow
                }
            };

            _mockUserWeakPointRepo.Setup(r => r.GetByUserIdAsync(userId))
                .ReturnsAsync(userWeakPoints);

            // Act
            var result = await _userWeakPointService.GetByUserIdAsync(userId, pagination);

            // Assert
            result.Should().NotBeNull();
            result.Data.Should().HaveCount(2);
            result.Data[0].UserId.Should().Be(userId);
            result.Data[0].WeakPoint.Should().Be("Grammar - Present Perfect");
            result.Data[0].ToeicPart.Should().Be("Part 5");
            result.Data[0].DifficultyLevel.Should().Be("Medium");
            result.Data[0].Advice.Should().Be("Practice more present perfect exercises");
            result.Data[1].UserId.Should().Be(userId);
            result.Data[1].WeakPoint.Should().Be("Vocabulary - Business Terms");
            result.Data[1].ToeicPart.Should().Be("Part 6");
            result.Data[1].DifficultyLevel.Should().Be("Hard");
            result.Data[1].Advice.Should().Be("Focus on common business vocabulary");
            result.Pagination.TotalCount.Should().Be(2);

            _mockUserWeakPointRepo.Verify(r => r.GetByUserIdAsync(userId), Times.Once);
        }

        [Fact]
        public async Task GetByUserIdAsync_WithEmptyResult_ShouldReturnEmptyPaginatedList()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var pagination = new PaginationRequestDto { Page = 1, PageSize = 10 };
            var emptyUserWeakPoints = new List<UserWeakPoint>();

            _mockUserWeakPointRepo.Setup(r => r.GetByUserIdAsync(userId))
                .ReturnsAsync(emptyUserWeakPoints);

            // Act
            var result = await _userWeakPointService.GetByUserIdAsync(userId, pagination);

            // Assert
            result.Should().NotBeNull();
            result.Data.Should().BeEmpty();
            result.Pagination.TotalCount.Should().Be(0);

            _mockUserWeakPointRepo.Verify(r => r.GetByUserIdAsync(userId), Times.Once);
        }

        [Fact]
        public async Task GetByUserIdAsync_WithNullPagination_ShouldReturnAllUserWeakPoints()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var userWeakPoints = new List<UserWeakPoint>
            {
                new UserWeakPoint
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    WeakPoint = "Listening - Fast Speech",
                    ToeicPart = "Part 3",
                    DifficultyLevel = "Hard",
                    Advice = "Practice with native speed audio",
                    CreatedAt = DateTime.UtcNow
                }
            };

            _mockUserWeakPointRepo.Setup(r => r.GetByUserIdAsync(userId))
                .ReturnsAsync(userWeakPoints);

            // Act
            var result = await _userWeakPointService.GetByUserIdAsync(userId, null!);

            // Assert
            result.Should().NotBeNull();
            result.Data.Should().HaveCount(1);
            result.Data[0].WeakPoint.Should().Be("Listening - Fast Speech");

            _mockUserWeakPointRepo.Verify(r => r.GetByUserIdAsync(userId), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_WithValidId_ShouldReturnUserWeakPoint()
        {
            // Arrange
            var userWeakPointId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var userWeakPoint = new UserWeakPoint
            {
                Id = userWeakPointId,
                UserId = userId,
                WeakPoint = "Reading - Inference Questions",
                ToeicPart = "Part 7",
                DifficultyLevel = "Medium",
                Advice = "Practice identifying implied meanings",
                CreatedAt = DateTime.UtcNow
            };

            _mockUserWeakPointRepo.Setup(r => r.GetByIdAsync(userWeakPointId))
                .ReturnsAsync(userWeakPoint);

            // Act
            var result = await _userWeakPointService.GetByIdAsync(userWeakPointId);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(userWeakPointId);
            result.UserId.Should().Be(userId);
            result.WeakPoint.Should().Be("Reading - Inference Questions");
            result.ToeicPart.Should().Be("Part 7");
            result.DifficultyLevel.Should().Be("Medium");
            result.Advice.Should().Be("Practice identifying implied meanings");

            _mockUserWeakPointRepo.Verify(r => r.GetByIdAsync(userWeakPointId), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_WithNonExistentId_ShouldReturnNull()
        {
            // Arrange
            var userWeakPointId = Guid.NewGuid();

            _mockUserWeakPointRepo.Setup(r => r.GetByIdAsync(userWeakPointId))
                .ReturnsAsync((UserWeakPoint?)null);

            // Act
            var result = await _userWeakPointService.GetByIdAsync(userWeakPointId);

            // Assert
            result.Should().BeNull();

            _mockUserWeakPointRepo.Verify(r => r.GetByIdAsync(userWeakPointId), Times.Once);
        }

        [Fact]
        public async Task AddAsync_WithValidRequest_ShouldReturnCreatedUserWeakPoint()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var request = new RequestUserWeakPointDto
            {
                UserId = userId,
                WeakPoint = "Grammar - Conditional Sentences",
                ToeicPart = "Part 5",
                DifficultyLevel = "Hard",
                Advice = "Focus on if-clauses and result patterns"
            };

            var createdUserWeakPoint = new UserWeakPoint
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                WeakPoint = request.WeakPoint,
                ToeicPart = request.ToeicPart,
                DifficultyLevel = request.DifficultyLevel,
                Advice = request.Advice,
                CreatedAt = DateTime.UtcNow
            };

            _mockUserWeakPointRepo.Setup(r => r.AddAsync(It.IsAny<UserWeakPoint>()))
                .ReturnsAsync(createdUserWeakPoint);

            // Act
            var result = await _userWeakPointService.AddAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.UserId.Should().Be(request.UserId);
            result.WeakPoint.Should().Be(request.WeakPoint);
            result.ToeicPart.Should().Be(request.ToeicPart);
            result.DifficultyLevel.Should().Be(request.DifficultyLevel);
            result.Advice.Should().Be(request.Advice);

            _mockUserWeakPointRepo.Verify(r => r.AddAsync(It.Is<UserWeakPoint>(uwp =>
                uwp.UserId == request.UserId &&
                uwp.WeakPoint == request.WeakPoint &&
                uwp.ToeicPart == request.ToeicPart &&
                uwp.DifficultyLevel == request.DifficultyLevel &&
                uwp.Advice == request.Advice)), Times.Once);
        }

        [Fact]
        public async Task AddAsync_WithMinimalData_ShouldReturnCreatedUserWeakPoint()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var request = new RequestUserWeakPointDto
            {
                UserId = userId,
                WeakPoint = "Pronunciation Issues",
                ToeicPart = "Part 2"
                // DifficultyLevel and Advice are optional
            };

            var createdUserWeakPoint = new UserWeakPoint
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                WeakPoint = request.WeakPoint,
                ToeicPart = request.ToeicPart,
                DifficultyLevel = null,
                Advice = null,
                CreatedAt = DateTime.UtcNow
            };

            _mockUserWeakPointRepo.Setup(r => r.AddAsync(It.IsAny<UserWeakPoint>()))
                .ReturnsAsync(createdUserWeakPoint);

            // Act
            var result = await _userWeakPointService.AddAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.UserId.Should().Be(request.UserId);
            result.WeakPoint.Should().Be(request.WeakPoint);
            result.ToeicPart.Should().Be(request.ToeicPart);
            result.DifficultyLevel.Should().BeNull();
            result.Advice.Should().BeNull();

            _mockUserWeakPointRepo.Verify(r => r.AddAsync(It.IsAny<UserWeakPoint>()), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_WithValidData_ShouldReturnUpdatedUserWeakPoint()
        {
            // Arrange
            var id = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var request = new RequestUserWeakPointDto
            {
                UserId = userId,
                WeakPoint = "Updated - Time Management",
                ToeicPart = "Part 4",
                DifficultyLevel = "Easy",
                Advice = "Practice with timer exercises"
            };

            var updatedUserWeakPoint = new UserWeakPoint
            {
                Id = id,
                UserId = request.UserId,
                WeakPoint = request.WeakPoint,
                ToeicPart = request.ToeicPart,
                DifficultyLevel = request.DifficultyLevel,
                Advice = request.Advice,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow
            };

            _mockUserWeakPointRepo.Setup(r => r.UpdateAsync(id, It.IsAny<UserWeakPoint>()))
                .ReturnsAsync(updatedUserWeakPoint);

            // Act
            var result = await _userWeakPointService.UpdateAsync(id, request);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(id);
            result.UserId.Should().Be(request.UserId);
            result.WeakPoint.Should().Be(request.WeakPoint);
            result.ToeicPart.Should().Be(request.ToeicPart);
            result.DifficultyLevel.Should().Be(request.DifficultyLevel);
            result.Advice.Should().Be(request.Advice);
            result.UpdatedAt.Should().NotBeNull();

            _mockUserWeakPointRepo.Verify(r => r.UpdateAsync(id, It.Is<UserWeakPoint>(uwp =>
                uwp.UserId == request.UserId &&
                uwp.WeakPoint == request.WeakPoint &&
                uwp.ToeicPart == request.ToeicPart &&
                uwp.DifficultyLevel == request.DifficultyLevel &&
                uwp.Advice == request.Advice)), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_WithPartialData_ShouldReturnUpdatedUserWeakPoint()
        {
            // Arrange
            var id = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var request = new RequestUserWeakPointDto
            {
                UserId = userId,
                WeakPoint = "Partial Update",
                ToeicPart = "Part 1"
                // DifficultyLevel and Advice left null
            };

            var updatedUserWeakPoint = new UserWeakPoint
            {
                Id = id,
                UserId = request.UserId,
                WeakPoint = request.WeakPoint,
                ToeicPart = request.ToeicPart,
                DifficultyLevel = null,
                Advice = null,
                CreatedAt = DateTime.UtcNow.AddHours(-2),
                UpdatedAt = DateTime.UtcNow
            };

            _mockUserWeakPointRepo.Setup(r => r.UpdateAsync(id, It.IsAny<UserWeakPoint>()))
                .ReturnsAsync(updatedUserWeakPoint);

            // Act
            var result = await _userWeakPointService.UpdateAsync(id, request);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(id);
            result.UserId.Should().Be(request.UserId);
            result.WeakPoint.Should().Be(request.WeakPoint);
            result.ToeicPart.Should().Be(request.ToeicPart);
            result.DifficultyLevel.Should().BeNull();
            result.Advice.Should().BeNull();

            _mockUserWeakPointRepo.Verify(r => r.UpdateAsync(id, It.IsAny<UserWeakPoint>()), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_WithValidId_ShouldReturnTrue()
        {
            // Arrange
            var userWeakPointId = Guid.NewGuid();

            _mockUserWeakPointRepo.Setup(r => r.DeleteAsync(userWeakPointId))
                .ReturnsAsync(true);

            // Act
            var result = await _userWeakPointService.DeleteAsync(userWeakPointId);

            // Assert
            result.Should().BeTrue();

            _mockUserWeakPointRepo.Verify(r => r.DeleteAsync(userWeakPointId), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_WithNonExistentId_ShouldReturnFalse()
        {
            // Arrange
            var userWeakPointId = Guid.NewGuid();

            _mockUserWeakPointRepo.Setup(r => r.DeleteAsync(userWeakPointId))
                .ReturnsAsync(false);

            // Act
            var result = await _userWeakPointService.DeleteAsync(userWeakPointId);

            // Assert
            result.Should().BeFalse();

            _mockUserWeakPointRepo.Verify(r => r.DeleteAsync(userWeakPointId), Times.Once);
        }

        [Fact]
        public async Task IsWeakPointExistedAsync_WithExistingWeakPoint_ShouldReturnTrue()
        {
            // Arrange
            var weakPoint = "Grammar - Articles";
            var userId = Guid.NewGuid();

            _mockUserWeakPointRepo.Setup(r => r.IsWeakPointExisted(weakPoint, userId))
                .ReturnsAsync(true);

            // Act
            var result = await _userWeakPointService.IsWeakPointExistedAsync(weakPoint, userId);

            // Assert
            result.Should().BeTrue();

            _mockUserWeakPointRepo.Verify(r => r.IsWeakPointExisted(weakPoint, userId), Times.Once);
        }

        [Fact]
        public async Task IsWeakPointExistedAsync_WithNonExistentWeakPoint_ShouldReturnFalse()
        {
            // Arrange
            var weakPoint = "Non-existent weak point";
            var userId = Guid.NewGuid();

            _mockUserWeakPointRepo.Setup(r => r.IsWeakPointExisted(weakPoint, userId))
                .ReturnsAsync(false);

            // Act
            var result = await _userWeakPointService.IsWeakPointExistedAsync(weakPoint, userId);

            // Assert
            result.Should().BeFalse();

            _mockUserWeakPointRepo.Verify(r => r.IsWeakPointExisted(weakPoint, userId), Times.Once);
        }

        [Fact]
        public async Task IsWeakPointExistedAsync_WithSimilarWeakPoint_ShouldReturnTrue()
        {
            // Arrange
            var weakPoint = "grammar articles";
            var userId = Guid.NewGuid();

            _mockUserWeakPointRepo.Setup(r => r.IsWeakPointExisted(weakPoint, userId))
                .ReturnsAsync(true);

            // Act
            var result = await _userWeakPointService.IsWeakPointExistedAsync(weakPoint, userId);

            // Assert
            result.Should().BeTrue();

            _mockUserWeakPointRepo.Verify(r => r.IsWeakPointExisted(weakPoint, userId), Times.Once);
        }
    }
}