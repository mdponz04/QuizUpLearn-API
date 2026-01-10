using AutoMapper;
using BusinessLogic.DTOs;
using BusinessLogic.DTOs.QuizDtos;
using BusinessLogic.DTOs.UserMistakeDtos;
using BusinessLogic.Extensions;
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
    public class UserMistakeServiceTest : BaseServiceTest
    {
        private readonly Mock<IUserMistakeRepo> _mockUserMistakeRepo;
        private readonly Mock<IUserWeakPointRepo> _mockUserWeakPointRepo;
        private readonly Mock<IUserWeakPointService> _mockUserWeakPointService;
        private readonly IMapper _mapper;
        private readonly UserMistakeService _userMistakeService;

        public UserMistakeServiceTest()
        {
            _mockUserMistakeRepo = new Mock<IUserMistakeRepo>();
            _mockUserWeakPointRepo = new Mock<IUserWeakPointRepo>();
            _mockUserWeakPointService = new Mock<IUserWeakPointService>();

            // Setup real AutoMapper with the actual mapping profile
            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<MappingProfile>();
            }, new NullLoggerFactory());
            _mapper = mapperConfig.CreateMapper();

            _userMistakeService = new UserMistakeService(
                _mockUserMistakeRepo.Object,
                _mockUserWeakPointRepo.Object,
                _mockUserWeakPointService.Object,
                _mapper);
        }

        [Fact]
        public async Task GetAllAsync_WithValidPagination_ShouldReturnPagedResponse()
        {
            // Arrange
            var pagination = new PaginationRequestDto
            {
                Page = 1,
                PageSize = 10
            };

            var userMistakes = new List<UserMistake>
            {
                new UserMistake
                {
                    Id = Guid.NewGuid(),
                    UserId = Guid.NewGuid(),
                    QuizId = Guid.NewGuid(),
                    TimesAttempted = 3,
                    TimesWrong = 2,
                    LastAttemptedAt = DateTime.UtcNow.AddDays(-1),
                    IsAnalyzed = true,
                    UserAnswer = "A",
                    CreatedAt = DateTime.UtcNow.AddDays(-2)
                },
                new UserMistake
                {
                    Id = Guid.NewGuid(),
                    UserId = Guid.NewGuid(),
                    QuizId = Guid.NewGuid(),
                    TimesAttempted = 5,
                    TimesWrong = 3,
                    LastAttemptedAt = DateTime.UtcNow.AddDays(-3),
                    IsAnalyzed = false,
                    UserAnswer = "B",
                    CreatedAt = DateTime.UtcNow.AddDays(-4)
                }
            };

            _mockUserMistakeRepo.Setup(r => r.GetAllAsync())
                .ReturnsAsync(userMistakes);

            // Act
            var result = await _userMistakeService.GetAllAsync(pagination);

            // Assert
            result.Should().NotBeNull();
            result.Data.Should().NotBeNull();
            result.Pagination.TotalCount.Should().Be(2);
            result.Pagination.CurrentPage.Should().Be(1);
            result.Pagination.PageSize.Should().Be(10);

            _mockUserMistakeRepo.Verify(r => r.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task GetAllAsync_WithNullPagination_ShouldUseDefaultPagination()
        {
            // Arrange
            var userMistakes = new List<UserMistake>
            {
                new UserMistake
                {
                    Id = Guid.NewGuid(),
                    UserId = Guid.NewGuid(),
                    QuizId = Guid.NewGuid(),
                    TimesAttempted = 1,
                    TimesWrong = 1,
                    IsAnalyzed = true,
                    CreatedAt = DateTime.UtcNow
                }
            };

            _mockUserMistakeRepo.Setup(r => r.GetAllAsync())
                .ReturnsAsync(userMistakes);

            // Act
            var result = await _userMistakeService.GetAllAsync(null);

            // Assert
            result.Should().NotBeNull();
            result.Data.Should().NotBeNull();
            result.Pagination.TotalCount.Should().Be(1);

            _mockUserMistakeRepo.Verify(r => r.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task GetAllByUserIdAsync_WithPagination_ShouldReturnPagedUserMistakes()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var pagination = new PaginationRequestDto
            {
                Page = 1,
                PageSize = 5
            };

            var userMistakes = new List<UserMistake>
            {
                new UserMistake
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    QuizId = Guid.NewGuid(),
                    TimesAttempted = 2,
                    TimesWrong = 1,
                    IsAnalyzed = true,
                    CreatedAt = DateTime.UtcNow
                }
            };

            _mockUserMistakeRepo.Setup(r => r.GetAlByUserIdAsync(userId))
                .ReturnsAsync(userMistakes);

            // Act
            var result = await _userMistakeService.GetAllByUserIdAsync(userId, pagination);

            // Assert
            result.Should().NotBeNull();
            result.Data.Should().NotBeNull();
            result.Data.Should().HaveCount(1);
            result.Data.First().UserId.Should().Be(userId);
            result.Pagination.TotalCount.Should().Be(1);

            _mockUserMistakeRepo.Verify(r => r.GetAlByUserIdAsync(userId), Times.Once);
        }

        [Fact]
        public async Task GetAllByUserIdAsync_WithoutPagination_ShouldReturnAllUserMistakes()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var userMistakes = new List<UserMistake>
            {
                new UserMistake
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    QuizId = Guid.NewGuid(),
                    TimesAttempted = 3,
                    TimesWrong = 2,
                    IsAnalyzed = false,
                    CreatedAt = DateTime.UtcNow
                }
            };

            _mockUserMistakeRepo.Setup(r => r.GetAlByUserIdAsync(userId))
                .ReturnsAsync(userMistakes);

            // Act
            var result = await _userMistakeService.GetAllByUserIdAsync(userId);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result.First().UserId.Should().Be(userId);

            _mockUserMistakeRepo.Verify(r => r.GetAlByUserIdAsync(userId), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_WithValidId_ShouldReturnResponseUserMistakeDto()
        {
            // Arrange
            var userMistakeId = Guid.NewGuid();
            var userMistake = new UserMistake
            {
                Id = userMistakeId,
                UserId = Guid.NewGuid(),
                QuizId = Guid.NewGuid(),
                TimesAttempted = 4,
                TimesWrong = 2,
                LastAttemptedAt = DateTime.UtcNow.AddHours(-2),
                IsAnalyzed = true,
                UserAnswer = "C",
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            };

            _mockUserMistakeRepo.Setup(r => r.GetByIdAsync(userMistakeId))
                .ReturnsAsync(userMistake);

            // Act
            var result = await _userMistakeService.GetByIdAsync(userMistakeId);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(userMistakeId);
            result.UserId.Should().Be(userMistake.UserId);
            result.QuizId.Should().Be(userMistake.QuizId);
            result.TimesAttempted.Should().Be(userMistake.TimesAttempted);
            result.TimesWrong.Should().Be(userMistake.TimesWrong);
            result.IsAnalyzed.Should().Be(userMistake.IsAnalyzed);
            result.UserAnswer.Should().Be(userMistake.UserAnswer);

            _mockUserMistakeRepo.Verify(r => r.GetByIdAsync(userMistakeId), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_WithNonExistentId_ShouldReturnNull()
        {
            // Arrange
            var userMistakeId = Guid.NewGuid();
            _mockUserMistakeRepo.Setup(r => r.GetByIdAsync(userMistakeId))
                .ReturnsAsync((UserMistake?)null);

            // Act
            var result = await _userMistakeService.GetByIdAsync(userMistakeId);

            // Assert
            result.Should().BeNull();
            _mockUserMistakeRepo.Verify(r => r.GetByIdAsync(userMistakeId), Times.Once);
        }

        [Fact]
        public async Task GetMistakeQuizzesByUserId_WithAllAnalyzedMistakes_ShouldReturnQuizzes()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var pagination = new PaginationRequestDto
            {
                Page = 1,
                PageSize = 10
            };

            var quiz1 = new Quiz
            {
                Id = Guid.NewGuid(),
                QuestionText = "Test Question 1",
                CorrectAnswer = "A",
                TOEICPart = "Part1",
                CreatedAt = DateTime.UtcNow
            };

            var quiz2 = new Quiz
            {
                Id = Guid.NewGuid(),
                QuestionText = "Test Question 2",
                CorrectAnswer = "B",
                TOEICPart = "Part1",
                CreatedAt = DateTime.UtcNow
            };

            var userMistakes = new List<UserMistake>
            {
                new UserMistake
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    QuizId = quiz1.Id,
                    Quiz = quiz1,
                    IsAnalyzed = true,
                    CreatedAt = DateTime.UtcNow
                },
                new UserMistake
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    QuizId = quiz2.Id,
                    Quiz = quiz2,
                    IsAnalyzed = true,
                    CreatedAt = DateTime.UtcNow
                }
            };

            var userWeakPoints = new List<UserWeakPoint>();

            _mockUserWeakPointRepo.Setup(r => r.GetByUserIdAsync(userId))
                .ReturnsAsync(userWeakPoints);
            _mockUserMistakeRepo.Setup(r => r.GetAlByUserIdAsync(userId))
                .ReturnsAsync(userMistakes);

            // Act
            var result = await _userMistakeService.GetMistakeQuizzesByUserId(userId, pagination);

            // Assert
            result.Should().NotBeNull();
            result.Data.Should().NotBeNull();
            result.Data.Should().HaveCount(2);
            result.Pagination.TotalCount.Should().Be(2);

            _mockUserMistakeRepo.Verify(r => r.GetAlByUserIdAsync(userId), Times.Once);
        }

        [Fact]
        public async Task AddAsync_WithValidRequestDto_ShouldAddUserMistake()
        {
            // Arrange
            var requestDto = new RequestUserMistakeDto
            {
                UserId = Guid.NewGuid(),
                QuizId = Guid.NewGuid(),
                TimesAttempted = 1,
                TimesWrong = 1,
                LastAttemptedAt = DateTime.UtcNow,
                IsAnalyzed = false,
                UserAnswer = "A"
            };

            _mockUserMistakeRepo.Setup(r => r.AddAsync(It.IsAny<UserMistake>()))
                .Returns(Task.CompletedTask);

            // Act
            await _userMistakeService.AddAsync(requestDto);

            // Assert
            _mockUserMistakeRepo.Verify(r => r.AddAsync(It.Is<UserMistake>(um =>
                um.UserId == requestDto.UserId &&
                um.QuizId == requestDto.QuizId &&
                um.TimesAttempted == requestDto.TimesAttempted &&
                um.TimesWrong == requestDto.TimesWrong &&
                um.IsAnalyzed == requestDto.IsAnalyzed &&
                um.UserAnswer == requestDto.UserAnswer)), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_WithValidData_ShouldUpdateUserMistake()
        {
            // Arrange
            var userMistakeId = Guid.NewGuid();
            var requestDto = new RequestUserMistakeDto
            {
                UserId = Guid.NewGuid(),
                QuizId = Guid.NewGuid(),
                TimesAttempted = 5,
                TimesWrong = 3,
                LastAttemptedAt = DateTime.UtcNow,
                IsAnalyzed = true,
                UserAnswer = "B"
            };

            _mockUserMistakeRepo.Setup(r => r.UpdateAsync(userMistakeId, It.IsAny<UserMistake>()))
                .Returns(Task.CompletedTask);

            // Act
            await _userMistakeService.UpdateAsync(userMistakeId, requestDto);

            // Assert
            _mockUserMistakeRepo.Verify(r => r.UpdateAsync(userMistakeId, It.Is<UserMistake>(um =>
                um.UserId == requestDto.UserId &&
                um.QuizId == requestDto.QuizId &&
                um.TimesAttempted == requestDto.TimesAttempted &&
                um.TimesWrong == requestDto.TimesWrong &&
                um.IsAnalyzed == requestDto.IsAnalyzed &&
                um.UserAnswer == requestDto.UserAnswer)), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_WithValidId_ShouldDeleteUserMistake()
        {
            // Arrange
            var userMistakeId = Guid.NewGuid();

            _mockUserMistakeRepo.Setup(r => r.DeleteAsync(userMistakeId))
                .Returns(Task.CompletedTask);

            // Act
            await _userMistakeService.DeleteAsync(userMistakeId);

            // Assert
            _mockUserMistakeRepo.Verify(r => r.DeleteAsync(userMistakeId), Times.Once);
        }

        [Fact]
        public async Task CleanupOrphanWeakPointsAsync_WithOrphanWeakPoints_ShouldDeleteThem()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var orphanWeakPointId = Guid.NewGuid();
            var usedWeakPointId = Guid.NewGuid();

            var userWeakPoints = new List<UserWeakPoint>
            {
                new UserWeakPoint { Id = orphanWeakPointId, UserId = userId, ToeicPart = "Part1", WeakPoint = "Weakpoint1" },
                new UserWeakPoint { Id = usedWeakPointId, UserId = userId, ToeicPart = "Part1", WeakPoint = "Weakpoint2" }
            };

            var userMistakes = new List<UserMistake>
            {
                new UserMistake
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    UserWeakPointId = usedWeakPointId,
                    CreatedAt = DateTime.UtcNow
                }
            };

            _mockUserWeakPointRepo.Setup(r => r.GetByUserIdAsync(userId))
                .ReturnsAsync(userWeakPoints);
            _mockUserMistakeRepo.Setup(r => r.GetAlByUserIdAsync(userId))
                .ReturnsAsync(userMistakes);
            _mockUserWeakPointService.Setup(s => s.DeleteAsync(orphanWeakPointId))
                .ReturnsAsync(true);

            // Act
            await _userMistakeService.CleanupOrphanWeakPointsAsync(userId);

            // Assert
            _mockUserWeakPointRepo.Verify(r => r.GetByUserIdAsync(userId), Times.Once);
            _mockUserMistakeRepo.Verify(r => r.GetAlByUserIdAsync(userId), Times.Once);
            _mockUserWeakPointService.Verify(s => s.DeleteAsync(orphanWeakPointId), Times.Once);
            _mockUserWeakPointService.Verify(s => s.DeleteAsync(usedWeakPointId), Times.Never);
        }

        [Fact]
        public async Task CleanupOrphanWeakPointsAsync_WithNoWeakPoints_ShouldReturnEarly()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var userWeakPoints = new List<UserWeakPoint>();

            _mockUserWeakPointRepo.Setup(r => r.GetByUserIdAsync(userId))
                .ReturnsAsync(userWeakPoints);

            // Act
            await _userMistakeService.CleanupOrphanWeakPointsAsync(userId);

            // Assert
            _mockUserWeakPointRepo.Verify(r => r.GetByUserIdAsync(userId), Times.Once);
            _mockUserMistakeRepo.Verify(r => r.GetAlByUserIdAsync(It.IsAny<Guid>()), Times.Never);
            _mockUserWeakPointService.Verify(s => s.DeleteAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task CleanupOrphanWeakPointsAsync_WithException_ShouldContinueExecution()
        {
            // Arrange
            var userId = Guid.NewGuid();

            _mockUserWeakPointRepo.Setup(r => r.GetByUserIdAsync(userId))
                .ThrowsAsync(new Exception("Database error"));

            // Act & Assert - Should not throw exception
            await _userMistakeService.CleanupOrphanWeakPointsAsync(userId);

            _mockUserWeakPointRepo.Verify(r => r.GetByUserIdAsync(userId), Times.Once);
        }
    }
}