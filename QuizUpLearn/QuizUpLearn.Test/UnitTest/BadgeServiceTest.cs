using AutoMapper;
using BusinessLogic.DTOs.BadgeDtos;
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
    public class BadgeServiceTest : BaseControllerTest
    {
        private readonly Mock<IBadgeRepo> _mockBadgeRepo;
        private readonly Mock<IQuizAttemptRepo> _mockQuizAttemptRepo;
        private readonly Mock<IQuizRepo> _mockQuizRepo;
        private readonly Mock<IQuizSetRepo> _mockQuizSetRepo;
        private readonly Mock<IQuizAttemptDetailRepo> _mockQuizAttemptDetailRepo;
        private readonly Mock<IUserRepo> _mockUserRepo;
        private readonly Mock<IUserBadgeRepo> _mockUserBadgeRepo;
        private readonly IMapper _mapper;
        private readonly BadgeService _badgeService;

        public BadgeServiceTest()
        {
            _mockBadgeRepo = new Mock<IBadgeRepo>();
            _mockQuizAttemptRepo = new Mock<IQuizAttemptRepo>();
            _mockQuizRepo = new Mock<IQuizRepo>();
            _mockQuizSetRepo = new Mock<IQuizSetRepo>();
            _mockQuizAttemptDetailRepo = new Mock<IQuizAttemptDetailRepo>();
            _mockUserRepo = new Mock<IUserRepo>();
            _mockUserBadgeRepo = new Mock<IUserBadgeRepo>();

            // Setup real AutoMapper with the actual mapping profile
            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<MappingProfile>();
            }, new NullLoggerFactory());
            _mapper = mapperConfig.CreateMapper();

            _badgeService = new BadgeService(
                _mockBadgeRepo.Object,
                _mockQuizAttemptRepo.Object,
                _mockQuizRepo.Object,
                _mockQuizSetRepo.Object,
                _mockQuizAttemptDetailRepo.Object,
                _mockUserRepo.Object,
                _mockUserBadgeRepo.Object,
                _mapper);
        }

        [Fact]
        public async Task GetUserBadgesAsync_WithValidUserId_ShouldReturnUserBadges()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var badge1 = new Badge
            {
                Id = Guid.NewGuid(),
                Code = "FIRST_STEP",
                Name = "First Step",
                Description = "Complete your first quiz",
                Type = BadgeTypeEnum.Progress,
                CreatedAt = DateTime.UtcNow
            };

            var badge2 = new Badge
            {
                Id = Guid.NewGuid(),
                Code = "QUIZ_SET_10",
                Name = "Quiz Set 10",
                Description = "Complete 10 quiz sets",
                Type = BadgeTypeEnum.Progress,
                CreatedAt = DateTime.UtcNow
            };

            var userBadges = new List<UserBadge>
            {
                new UserBadge
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    BadgeId = badge1.Id,
                    Badge = badge1,
                    CreatedAt = DateTime.UtcNow
                },
                new UserBadge
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    BadgeId = badge2.Id,
                    Badge = badge2,
                    CreatedAt = DateTime.UtcNow
                }
            };

            _mockUserBadgeRepo.Setup(r => r.GetByUserIdAsync(userId, false))
                .ReturnsAsync(userBadges);

            // Act
            var result = await _badgeService.GetUserBadgesAsync(userId);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.All(b => b.Id == badge1.Id || b.Id == badge2.Id).Should().BeTrue();

            _mockUserBadgeRepo.Verify(r => r.GetByUserIdAsync(userId, false), Times.Once);
        }

        [Fact]
        public async Task GetUserBadgesAsync_WithNoBadges_ShouldReturnEmptyList()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _mockUserBadgeRepo.Setup(r => r.GetByUserIdAsync(userId, false))
                .ReturnsAsync(new List<UserBadge>());

            // Act
            var result = await _badgeService.GetUserBadgesAsync(userId);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetUserBadgesAsync_WithNullBadge_ShouldFilterOutNullBadges()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var badge = new Badge
            {
                Id = Guid.NewGuid(),
                Name = "Test Badge",
                CreatedAt = DateTime.UtcNow
            };

            var userBadges = new List<UserBadge>
            {
                new UserBadge
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    BadgeId = badge.Id,
                    Badge = badge,
                    CreatedAt = DateTime.UtcNow
                },
                new UserBadge
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    BadgeId = Guid.NewGuid(),
                    Badge = null, // Null badge
                    CreatedAt = DateTime.UtcNow
                }
            };

            _mockUserBadgeRepo.Setup(r => r.GetByUserIdAsync(userId, false))
                .ReturnsAsync(userBadges);

            // Act
            var result = await _badgeService.GetUserBadgesAsync(userId);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result.First().Id.Should().Be(badge.Id);
        }

        [Fact]
        public async Task CheckAndAssignBadgesAsync_WithNonExistentUser_ShouldReturnEarly()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _mockUserRepo.Setup(r => r.GetByIdAsync(userId))
                .ReturnsAsync((User?)null);

            // Act
            await _badgeService.CheckAndAssignBadgesAsync(userId);

            // Assert
            _mockUserBadgeRepo.Verify(r => r.CreateAsync(It.IsAny<UserBadge>()), Times.Never);
        }

        [Fact]
        public async Task CheckAndAssignBadgesAsync_WithFirstStepBadge_ShouldAssignBadge()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new User
            {
                Id = userId,
                Username = "testuser",
                AvatarUrl = "https://example.com/avatar.jpg",
                CreatedAt = DateTime.UtcNow
            };

            var badge = new Badge
            {
                Id = Guid.NewGuid(),
                Code = "FIRST_STEP",
                Name = "First Step",
                Description = "Complete your first quiz",
                Type = BadgeTypeEnum.Progress,
                CreatedAt = DateTime.UtcNow
            };

            var completedAttempt = new QuizAttempt
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                QuizSetId = Guid.NewGuid(),
                Status = "completed",
                CreatedAt = DateTime.UtcNow
            };

            _mockBadgeRepo.Setup(r => r.GetAllAsync(false))
                .ReturnsAsync(new List<Badge> { badge });
            _mockUserRepo.Setup(r => r.GetByIdAsync(userId))
                .ReturnsAsync(user);
            _mockQuizAttemptRepo.Setup(r => r.GetByUserIdAsync(userId, false))
                .ReturnsAsync(new List<QuizAttempt> { completedAttempt });
            _mockUserBadgeRepo.Setup(r => r.GetByUserIdAsync(userId, false))
                .ReturnsAsync(new List<UserBadge>());
            _mockUserBadgeRepo.Setup(r => r.CreateAsync(It.IsAny<UserBadge>()))
                .ReturnsAsync((UserBadge ub) => ub);

            // Act
            await _badgeService.CheckAndAssignBadgesAsync(userId);

            // Assert
            _mockUserBadgeRepo.Verify(r => r.CreateAsync(It.Is<UserBadge>(ub =>
                ub.UserId == userId &&
                ub.BadgeId == badge.Id)), Times.Once);
        }

        [Fact]
        public async Task CheckAndAssignBadgesAsync_WithExistingBadge_ShouldNotAssignAgain()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new User
            {
                Id = userId,
                Username = "testuser",
                AvatarUrl = "https://example.com/avatar.jpg",
                CreatedAt = DateTime.UtcNow
            };

            var badge = new Badge
            {
                Id = Guid.NewGuid(),
                Code = "FIRST_STEP",
                Name = "First Step",
                Type = BadgeTypeEnum.Progress,
                CreatedAt = DateTime.UtcNow
            };

            var existingUserBadge = new UserBadge
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                BadgeId = badge.Id,
                CreatedAt = DateTime.UtcNow
            };

            _mockBadgeRepo.Setup(r => r.GetAllAsync(false))
                .ReturnsAsync(new List<Badge> { badge });
            _mockUserRepo.Setup(r => r.GetByIdAsync(userId))
                .ReturnsAsync(user);
            _mockQuizAttemptRepo.Setup(r => r.GetByUserIdAsync(userId, false))
                .ReturnsAsync(new List<QuizAttempt>());
            _mockUserBadgeRepo.Setup(r => r.GetByUserIdAsync(userId, false))
                .ReturnsAsync(new List<UserBadge> { existingUserBadge });

            // Act
            await _badgeService.CheckAndAssignBadgesAsync(userId);

            // Assert
            _mockUserBadgeRepo.Verify(r => r.CreateAsync(It.IsAny<UserBadge>()), Times.Never);
        }

        [Fact]
        public async Task CheckAndAssignBadgesAsync_WithQuizSet10Badge_ShouldAssignBadge()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new User
            {
                Id = userId,
                Username = "testuser",
                AvatarUrl = "https://example.com/avatar.jpg",
                CreatedAt = DateTime.UtcNow
            };

            var badge = new Badge
            {
                Id = Guid.NewGuid(),
                Code = "QUIZ_SET_10",
                Name = "Quiz Set 10",
                Type = BadgeTypeEnum.Progress,
                CreatedAt = DateTime.UtcNow
            };

            // Create 10 completed attempts with different QuizSetIds
            var completedAttempts = new List<QuizAttempt>();
            for (int i = 0; i < 10; i++)
            {
                completedAttempts.Add(new QuizAttempt
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    QuizSetId = Guid.NewGuid(), // Different QuizSetId for each
                    Status = "completed",
                    CreatedAt = DateTime.UtcNow
                });
            }

            _mockBadgeRepo.Setup(r => r.GetAllAsync(false))
                .ReturnsAsync(new List<Badge> { badge });
            _mockUserRepo.Setup(r => r.GetByIdAsync(userId))
                .ReturnsAsync(user);
            _mockQuizAttemptRepo.Setup(r => r.GetByUserIdAsync(userId, false))
                .ReturnsAsync(completedAttempts);
            _mockUserBadgeRepo.Setup(r => r.GetByUserIdAsync(userId, false))
                .ReturnsAsync(new List<UserBadge>());
            _mockUserBadgeRepo.Setup(r => r.CreateAsync(It.IsAny<UserBadge>()))
                .ReturnsAsync((UserBadge ub) => ub);

            // Act
            await _badgeService.CheckAndAssignBadgesAsync(userId);

            // Assert
            _mockUserBadgeRepo.Verify(r => r.CreateAsync(It.Is<UserBadge>(ub =>
                ub.UserId == userId &&
                ub.BadgeId == badge.Id)), Times.Once);
        }

        [Fact]
        public async Task CheckAndAssignBadgesAsync_WithStreak7Days_ShouldAssignBadge()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new User
            {
                Id = userId,
                Username = "testuser",
                AvatarUrl = "https://example.com/avatar.jpg",
                LoginStreak = 7,
                CreatedAt = DateTime.UtcNow
            };

            var badge = new Badge
            {
                Id = Guid.NewGuid(),
                Code = "STREAK_7_DAYS",
                Name = "7 Day Streak",
                Type = BadgeTypeEnum.Consistency,
                CreatedAt = DateTime.UtcNow
            };

            _mockBadgeRepo.Setup(r => r.GetAllAsync(false))
                .ReturnsAsync(new List<Badge> { badge });
            _mockUserRepo.Setup(r => r.GetByIdAsync(userId))
                .ReturnsAsync(user);
            _mockQuizAttemptRepo.Setup(r => r.GetByUserIdAsync(userId, false))
                .ReturnsAsync(new List<QuizAttempt>());
            _mockUserBadgeRepo.Setup(r => r.GetByUserIdAsync(userId, false))
                .ReturnsAsync(new List<UserBadge>());
            _mockUserBadgeRepo.Setup(r => r.CreateAsync(It.IsAny<UserBadge>()))
                .ReturnsAsync((UserBadge ub) => ub);

            // Act
            await _badgeService.CheckAndAssignBadgesAsync(userId);

            // Assert
            _mockUserBadgeRepo.Verify(r => r.CreateAsync(It.Is<UserBadge>(ub =>
                ub.UserId == userId &&
                ub.BadgeId == badge.Id)), Times.Once);
        }

        [Fact]
        public async Task CheckAndAssignBadgesAsync_WithAccuracyPro_ShouldAssignBadge()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new User
            {
                Id = userId,
                Username = "testuser",
                AvatarUrl = "https://example.com/avatar.jpg",
                CreatedAt = DateTime.UtcNow
            };

            var badge = new Badge
            {
                Id = Guid.NewGuid(),
                Code = "ACCURACY_PRO",
                Name = "Accuracy Pro",
                Type = BadgeTypeEnum.Skill,
                CreatedAt = DateTime.UtcNow
            };

            // Create attempts with high accuracy (>= 90%)
            var completedAttempts = new List<QuizAttempt>
            {
                new QuizAttempt
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    QuizSetId = Guid.NewGuid(),
                    Status = "completed",
                    Accuracy = 0.95m,
                    CreatedAt = DateTime.UtcNow
                },
                new QuizAttempt
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    QuizSetId = Guid.NewGuid(),
                    Status = "completed",
                    Accuracy = 0.92m,
                    CreatedAt = DateTime.UtcNow
                }
            };

            _mockBadgeRepo.Setup(r => r.GetAllAsync(false))
                .ReturnsAsync(new List<Badge> { badge });
            _mockUserRepo.Setup(r => r.GetByIdAsync(userId))
                .ReturnsAsync(user);
            _mockQuizAttemptRepo.Setup(r => r.GetByUserIdAsync(userId, false))
                .ReturnsAsync(completedAttempts);
            _mockUserBadgeRepo.Setup(r => r.GetByUserIdAsync(userId, false))
                .ReturnsAsync(new List<UserBadge>());
            _mockUserBadgeRepo.Setup(r => r.CreateAsync(It.IsAny<UserBadge>()))
                .ReturnsAsync((UserBadge ub) => ub);

            // Act
            await _badgeService.CheckAndAssignBadgesAsync(userId);

            // Assert
            _mockUserBadgeRepo.Verify(r => r.CreateAsync(It.Is<UserBadge>(ub =>
                ub.UserId == userId &&
                ub.BadgeId == badge.Id)), Times.Once);
        }
    }
}

