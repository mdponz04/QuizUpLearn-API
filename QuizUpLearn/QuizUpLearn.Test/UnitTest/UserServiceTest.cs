using AutoMapper;
using BusinessLogic.DTOs;
using BusinessLogic.DTOs.BadgeDtos;
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
    public class UserServiceTest : BaseServiceTest
    {
        private readonly Mock<IUserRepo> _mockUserRepo;
        private readonly Mock<IBadgeService> _mockBadgeService;
        private readonly IMapper _mapper;
        private readonly UserService _userService;

        public UserServiceTest()
        {
            _mockUserRepo = new Mock<IUserRepo>();
            _mockBadgeService = new Mock<IBadgeService>();

            // Setup real AutoMapper with the actual mapping profile
            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<MappingProfile>();
            }, new NullLoggerFactory());
            _mapper = mapperConfig.CreateMapper();

            _userService = new UserService(
                _mockUserRepo.Object,
                _mapper,
                _mockBadgeService.Object);
        }

        [Fact]
        public async Task CreateAsync_WithValidData_ShouldReturnResponseUserDto()
        {
            // Arrange
            var requestDto = new RequestUserDto
            {
                Username = "testuser",
                FullName = "Test User",
                AvatarUrl = "https://example.com/avatar.jpg",
                Bio = "Test bio",
                PreferredLanguage = "en",
                Timezone = "UTC"
            };

            var accountId = Guid.NewGuid();
            var createdUser = new User
            {
                Id = Guid.NewGuid(),
                AccountId = accountId,
                Username = requestDto.Username,
                FullName = requestDto.FullName,
                AvatarUrl = requestDto.AvatarUrl,
                Bio = requestDto.Bio,
                PreferredLanguage = requestDto.PreferredLanguage,
                Timezone = requestDto.Timezone,
                LoginStreak = 0,
                TotalPoints = 0,
                CreatedAt = DateTime.UtcNow
            };

            _mockUserRepo.Setup(r => r.CreateAsync(It.IsAny<User>()))
                .ReturnsAsync(createdUser);

            // Act
            var result = await _userService.CreateAsync(requestDto);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(createdUser.Id);
            result.Username.Should().Be(requestDto.Username);
            result.FullName.Should().Be(requestDto.FullName);
            result.AvatarUrl.Should().Be(requestDto.AvatarUrl);
            result.Bio.Should().Be(requestDto.Bio);
            result.PreferredLanguage.Should().Be(requestDto.PreferredLanguage);
            result.Timezone.Should().Be(requestDto.Timezone);

            _mockUserRepo.Verify(r => r.CreateAsync(It.Is<User>(u =>
                u.Username == requestDto.Username &&
                u.FullName == requestDto.FullName &&
                u.AvatarUrl == requestDto.AvatarUrl)), Times.Once);
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllUsers()
        {
            // Arrange
            var users = new List<User>
            {
                new User
                {
                    Id = Guid.NewGuid(),
                    AccountId = Guid.NewGuid(),
                    Username = "user1",
                    FullName = "User 1",
                    AvatarUrl = "https://example.com/avatar1.jpg",
                    LoginStreak = 5,
                    TotalPoints = 100,
                    CreatedAt = DateTime.UtcNow
                },
                new User
                {
                    Id = Guid.NewGuid(),
                    AccountId = Guid.NewGuid(),
                    Username = "user2",
                    FullName = "User 2",
                    AvatarUrl = "https://example.com/avatar2.jpg",
                    LoginStreak = 3,
                    TotalPoints = 50,
                    CreatedAt = DateTime.UtcNow
                }
            };

            _mockUserRepo.Setup(r => r.GetAllAsync(false))
                .ReturnsAsync(users);

            // Act
            var result = await _userService.GetAllAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            _mockUserRepo.Verify(r => r.GetAllAsync(false), Times.Once);
        }

        [Fact]
        public async Task GetAllAsync_WithIncludeDeleted_ShouldReturnAllUsersIncludingDeleted()
        {
            // Arrange
            var users = new List<User>
            {
                new User
                {
                    Id = Guid.NewGuid(),
                    AccountId = Guid.NewGuid(),
                    Username = "user1",
                    AvatarUrl = "https://example.com/avatar1.jpg",
                    CreatedAt = DateTime.UtcNow
                }
            };

            _mockUserRepo.Setup(r => r.GetAllAsync(true))
                .ReturnsAsync(users);

            // Act
            var result = await _userService.GetAllAsync(true);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            _mockUserRepo.Verify(r => r.GetAllAsync(true), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_WithValidId_ShouldReturnResponseUserDtoWithBadges()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new User
            {
                Id = userId,
                AccountId = Guid.NewGuid(),
                Username = "testuser",
                FullName = "Test User",
                AvatarUrl = "https://example.com/avatar.jpg",
                LoginStreak = 10,
                TotalPoints = 500,
                LastLoginDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            var badges = new List<ResponseBadgeDto>
            {
                new ResponseBadgeDto
                {
                    Id = Guid.NewGuid(),
                    Name = "First Quiz",
                    Description = "Complete your first quiz"
                }
            };

            _mockUserRepo.Setup(r => r.GetByIdAsync(userId))
                .ReturnsAsync(user);
            _mockBadgeService.Setup(s => s.GetUserBadgesAsync(userId))
                .ReturnsAsync(badges);

            // Act
            var result = await _userService.GetByIdAsync(userId);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(userId);
            result.Username.Should().Be(user.Username);
            result.LoginStreak.Should().Be(user.LoginStreak);
            result.TotalPoints.Should().Be(user.TotalPoints);
            result.EarnedBadges.Should().NotBeNull();
            result.EarnedBadges.Should().HaveCount(1);

            _mockUserRepo.Verify(r => r.GetByIdAsync(userId), Times.Once);
            _mockBadgeService.Verify(s => s.GetUserBadgesAsync(userId), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_WithNonExistentId_ShouldReturnNull()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _mockUserRepo.Setup(r => r.GetByIdAsync(userId))
                .ReturnsAsync((User?)null);

            // Act
            var result = await _userService.GetByIdAsync(userId);

            // Assert
            result.Should().BeNull();
            _mockUserRepo.Verify(r => r.GetByIdAsync(userId), Times.Once);
            _mockBadgeService.Verify(s => s.GetUserBadgesAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task GetByUsernameAsync_WithValidUsername_ShouldReturnResponseUserDto()
        {
            // Arrange
            var username = "testuser";
            var user = new User
            {
                Id = Guid.NewGuid(),
                AccountId = Guid.NewGuid(),
                Username = username,
                FullName = "Test User",
                AvatarUrl = "https://example.com/avatar.jpg",
                CreatedAt = DateTime.UtcNow
            };

            _mockUserRepo.Setup(r => r.GetByUsernameAsync(username))
                .ReturnsAsync(user);

            // Act
            var result = await _userService.GetByUsernameAsync(username);

            // Assert
            result.Should().NotBeNull();
            result!.Username.Should().Be(username);
            _mockUserRepo.Verify(r => r.GetByUsernameAsync(username), Times.Once);
        }

        [Fact]
        public async Task GetByUsernameAsync_WithNonExistentUsername_ShouldReturnNull()
        {
            // Arrange
            var username = "nonexistent";
            _mockUserRepo.Setup(r => r.GetByUsernameAsync(username))
                .ReturnsAsync((User?)null);

            // Act
            var result = await _userService.GetByUsernameAsync(username);

            // Assert
            result.Should().BeNull();
            _mockUserRepo.Verify(r => r.GetByUsernameAsync(username), Times.Once);
        }

        [Fact]
        public async Task GetByAccountIdAsync_WithValidAccountId_ShouldReturnResponseUserDto()
        {
            // Arrange
            var accountId = Guid.NewGuid();
            var user = new User
            {
                Id = Guid.NewGuid(),
                AccountId = accountId,
                Username = "testuser",
                AvatarUrl = "https://example.com/avatar.jpg",
                CreatedAt = DateTime.UtcNow
            };

            _mockUserRepo.Setup(r => r.GetByAccountIdAsync(accountId))
                .ReturnsAsync(user);

            // Act
            var result = await _userService.GetByAccountIdAsync(accountId);

            // Assert
            result.Should().NotBeNull();
            result!.AccountId.Should().Be(accountId);
            _mockUserRepo.Verify(r => r.GetByAccountIdAsync(accountId), Times.Once);
        }

        [Fact]
        public async Task GetByAccountIdAsync_WithNonExistentAccountId_ShouldReturnNull()
        {
            // Arrange
            var accountId = Guid.NewGuid();
            _mockUserRepo.Setup(r => r.GetByAccountIdAsync(accountId))
                .ReturnsAsync((User?)null);

            // Act
            var result = await _userService.GetByAccountIdAsync(accountId);

            // Assert
            result.Should().BeNull();
            _mockUserRepo.Verify(r => r.GetByAccountIdAsync(accountId), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_WithValidData_ShouldReturnUpdatedResponseUserDto()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var requestDto = new RequestUserDto
            {
                Username = "updateduser",
                FullName = "Updated User",
                AvatarUrl = "https://example.com/new-avatar.jpg",
                Bio = "Updated bio",
                PreferredLanguage = "vi",
                Timezone = "Asia/Ho_Chi_Minh"
            };

            var updatedUser = new User
            {
                Id = userId,
                AccountId = Guid.NewGuid(),
                Username = requestDto.Username,
                FullName = requestDto.FullName,
                AvatarUrl = requestDto.AvatarUrl,
                Bio = requestDto.Bio,
                PreferredLanguage = requestDto.PreferredLanguage,
                Timezone = requestDto.Timezone,
                LoginStreak = 5,
                TotalPoints = 200,
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                UpdatedAt = DateTime.UtcNow
            };

            _mockUserRepo.Setup(r => r.UpdateAsync(userId, It.IsAny<User>()))
                .ReturnsAsync(updatedUser);

            // Act
            var result = await _userService.UpdateAsync(userId, requestDto);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(userId);
            result.Username.Should().Be(requestDto.Username);
            result.FullName.Should().Be(requestDto.FullName);
            result.AvatarUrl.Should().Be(requestDto.AvatarUrl);
            result.Bio.Should().Be(requestDto.Bio);
            result.PreferredLanguage.Should().Be(requestDto.PreferredLanguage);
            result.Timezone.Should().Be(requestDto.Timezone);

            _mockUserRepo.Verify(r => r.UpdateAsync(userId, It.IsAny<User>()), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_WithNonExistentId_ShouldReturnNull()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var requestDto = new RequestUserDto
            {
                Username = "testuser",
                AvatarUrl = "https://example.com/avatar.jpg"
            };

            _mockUserRepo.Setup(r => r.UpdateAsync(userId, It.IsAny<User>()))
                .ReturnsAsync((User?)null);

            // Act
            var result = await _userService.UpdateAsync(userId, requestDto);

            // Assert
            result.Should().BeNull();
            _mockUserRepo.Verify(r => r.UpdateAsync(userId, It.IsAny<User>()), Times.Once);
        }

        [Fact]
        public async Task SoftDeleteAsync_WithValidId_ShouldReturnTrue()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _mockUserRepo.Setup(r => r.SoftDeleteAsync(userId))
                .ReturnsAsync(true);

            // Act
            var result = await _userService.SoftDeleteAsync(userId);

            // Assert
            result.Should().BeTrue();
            _mockUserRepo.Verify(r => r.SoftDeleteAsync(userId), Times.Once);
        }

        [Fact]
        public async Task RestoreAsync_WithValidId_ShouldReturnTrue()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _mockUserRepo.Setup(r => r.RestoreAsync(userId))
                .ReturnsAsync(true);

            // Act
            var result = await _userService.RestoreAsync(userId);

            // Assert
            result.Should().BeTrue();
            _mockUserRepo.Verify(r => r.RestoreAsync(userId), Times.Once);
        }
    }
}

