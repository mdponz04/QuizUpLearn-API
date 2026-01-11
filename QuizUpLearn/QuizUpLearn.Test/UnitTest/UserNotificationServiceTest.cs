using AutoMapper;
using BusinessLogic.DTOs.UserNotificationDtos;
using BusinessLogic.MappingProfile;
using BusinessLogic.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Repository.Entities;
using Repository.Interfaces;

namespace QuizUpLearn.Test.UnitTest
{
    public class UserNotificationServiceTest
    {
        private readonly Mock<IUserNotificationRepo> _mockUserNotificationRepo;
        private readonly IMapper _mapper;
        private readonly UserNotificationService _userNotificationService;

        public UserNotificationServiceTest()
        {
            _mockUserNotificationRepo = new Mock<IUserNotificationRepo>();

            // Setup real AutoMapper with the actual mapping profile
            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<MappingProfile>();
            }, new NullLoggerFactory());
            _mapper = mapperConfig.CreateMapper();

            _userNotificationService = new UserNotificationService(
                _mockUserNotificationRepo.Object,
                _mapper);
        }

        [Fact]
        public async Task GetAllAsync_WhenUserNotificationsExist_ShouldReturnMappedUserNotifications()
        {
            // Arrange
            var userId1 = Guid.NewGuid();
            var userId2 = Guid.NewGuid();
            var notificationId1 = Guid.NewGuid();
            var notificationId2 = Guid.NewGuid();

            var userNotifications = new List<UserNotification>
            {
                new UserNotification
                {
                    Id = Guid.NewGuid(),
                    UserId = userId1,
                    NotificationId = notificationId1,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow.AddDays(-1)
                },
                new UserNotification
                {
                    Id = Guid.NewGuid(),
                    UserId = userId2,
                    NotificationId = notificationId2,
                    IsRead = true,
                    ReadAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow.AddDays(-2)
                }
            };

            _mockUserNotificationRepo.Setup(r => r.GetAllAsync())
                .ReturnsAsync(userNotifications);

            // Act
            var result = await _userNotificationService.GetAllAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);

            var resultList = result.ToList();
            resultList[0].UserId.Should().Be(userId1);
            resultList[0].NotificationId.Should().Be(notificationId1);
            resultList[0].IsRead.Should().BeFalse();

            resultList[1].UserId.Should().Be(userId2);
            resultList[1].NotificationId.Should().Be(notificationId2);
            resultList[1].IsRead.Should().BeTrue();

            _mockUserNotificationRepo.Verify(r => r.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task GetAllAsync_WhenNoUserNotificationsExist_ShouldReturnEmptyCollection()
        {
            // Arrange
            _mockUserNotificationRepo.Setup(r => r.GetAllAsync())
                .ReturnsAsync(new List<UserNotification>());

            // Act
            var result = await _userNotificationService.GetAllAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();

            _mockUserNotificationRepo.Verify(r => r.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_WhenUserNotificationExists_ShouldReturnMappedUserNotification()
        {
            // Arrange
            var userNotificationId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var notificationId = Guid.NewGuid();

            var userNotification = new UserNotification
            {
                Id = userNotificationId,
                UserId = userId,
                NotificationId = notificationId,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            _mockUserNotificationRepo.Setup(r => r.GetByIdAsync(userNotificationId))
                .ReturnsAsync(userNotification);

            // Act
            var result = await _userNotificationService.GetByIdAsync(userNotificationId);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(userNotificationId);
            result.UserId.Should().Be(userId);
            result.NotificationId.Should().Be(notificationId);
            result.IsRead.Should().BeFalse();

            _mockUserNotificationRepo.Verify(r => r.GetByIdAsync(userNotificationId), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_WhenUserNotificationDoesNotExist_ShouldReturnNull()
        {
            // Arrange
            var userNotificationId = Guid.NewGuid();

            _mockUserNotificationRepo.Setup(r => r.GetByIdAsync(userNotificationId))
                .ReturnsAsync((UserNotification?)null);

            // Act
            var result = await _userNotificationService.GetByIdAsync(userNotificationId);

            // Assert
            result.Should().BeNull();

            _mockUserNotificationRepo.Verify(r => r.GetByIdAsync(userNotificationId), Times.Once);
        }

        [Fact]
        public async Task GetByUserIdAsync_WhenUserNotificationsExist_ShouldReturnMappedUserNotifications()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var notificationId1 = Guid.NewGuid();
            var notificationId2 = Guid.NewGuid();

            var userNotifications = new List<UserNotification>
            {
                new UserNotification
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    NotificationId = notificationId1,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow.AddDays(-1)
                },
                new UserNotification
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    NotificationId = notificationId2,
                    IsRead = true,
                    ReadAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow.AddDays(-2)
                }
            };

            _mockUserNotificationRepo.Setup(r => r.GetByUserIdAsync(userId))
                .ReturnsAsync(userNotifications);

            // Act
            var result = await _userNotificationService.GetByUserIdAsync(userId);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);

            var resultList = result.ToList();
            resultList.Should().AllSatisfy(un => un.UserId.Should().Be(userId));

            _mockUserNotificationRepo.Verify(r => r.GetByUserIdAsync(userId), Times.Once);
        }

        [Fact]
        public async Task GetByUserIdAsync_WhenNoUserNotificationsExist_ShouldReturnEmptyCollection()
        {
            // Arrange
            var userId = Guid.NewGuid();

            _mockUserNotificationRepo.Setup(r => r.GetByUserIdAsync(userId))
                .ReturnsAsync(new List<UserNotification>());

            // Act
            var result = await _userNotificationService.GetByUserIdAsync(userId);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();

            _mockUserNotificationRepo.Verify(r => r.GetByUserIdAsync(userId), Times.Once);
        }

        [Fact]
        public async Task GetUnreadByUserIdAsync_WhenUnreadUserNotificationsExist_ShouldReturnMappedUnreadUserNotifications()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var notificationId1 = Guid.NewGuid();
            var notificationId2 = Guid.NewGuid();

            var unreadUserNotifications = new List<UserNotification>
            {
                new UserNotification
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    NotificationId = notificationId1,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow.AddDays(-1)
                },
                new UserNotification
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    NotificationId = notificationId2,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow.AddDays(-2)
                }
            };

            _mockUserNotificationRepo.Setup(r => r.GetUnreadByUserIdAsync(userId))
                .ReturnsAsync(unreadUserNotifications);

            // Act
            var result = await _userNotificationService.GetUnreadByUserIdAsync(userId);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);

            var resultList = result.ToList();
            resultList.Should().AllSatisfy(un => 
            {
                un.UserId.Should().Be(userId);
                un.IsRead.Should().BeFalse();
            });

            _mockUserNotificationRepo.Verify(r => r.GetUnreadByUserIdAsync(userId), Times.Once);
        }

        [Fact]
        public async Task GetUnreadByUserIdAsync_WhenNoUnreadUserNotificationsExist_ShouldReturnEmptyCollection()
        {
            // Arrange
            var userId = Guid.NewGuid();

            _mockUserNotificationRepo.Setup(r => r.GetUnreadByUserIdAsync(userId))
                .ReturnsAsync(new List<UserNotification>());

            // Act
            var result = await _userNotificationService.GetUnreadByUserIdAsync(userId);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();

            _mockUserNotificationRepo.Verify(r => r.GetUnreadByUserIdAsync(userId), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_WithValidData_ShouldReturnMappedUserNotification()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var notificationId = Guid.NewGuid();

            var requestDto = new UserNotificationRequestDto
            {
                UserId = userId,
                NotificationId = notificationId,
                IsRead = false
            };

            var createdUserNotification = new UserNotification
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                NotificationId = notificationId,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            _mockUserNotificationRepo.Setup(r => r.CreateAsync(It.IsAny<UserNotification>()))
                .ReturnsAsync(createdUserNotification);

            // Act
            var result = await _userNotificationService.CreateAsync(requestDto);

            // Assert
            result.Should().NotBeNull();
            result.UserId.Should().Be(userId);
            result.NotificationId.Should().Be(notificationId);
            result.IsRead.Should().BeFalse();

            _mockUserNotificationRepo.Verify(r => r.CreateAsync(It.Is<UserNotification>(un =>
                un.UserId == userId &&
                un.NotificationId == notificationId &&
                un.IsRead == false)), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_WithValidData_ShouldReturnUpdatedMappedUserNotification()
        {
            // Arrange
            var userNotificationId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var notificationId = Guid.NewGuid();

            var requestDto = new UserNotificationRequestDto
            {
                UserId = userId,
                NotificationId = notificationId,
                IsRead = true,
                ReadAt = DateTime.UtcNow
            };

            var updatedUserNotification = new UserNotification
            {
                Id = userNotificationId,
                UserId = userId,
                NotificationId = notificationId,
                IsRead = true,
                ReadAt = requestDto.ReadAt,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow
            };

            _mockUserNotificationRepo.Setup(r => r.UpdateAsync(It.IsAny<UserNotification>()))
                .ReturnsAsync(updatedUserNotification);

            // Act
            var result = await _userNotificationService.UpdateAsync(userNotificationId, requestDto);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(userNotificationId);
            result.UserId.Should().Be(userId);
            result.NotificationId.Should().Be(notificationId);
            result.IsRead.Should().BeTrue();
            result.ReadAt.Should().Be(requestDto.ReadAt);

            _mockUserNotificationRepo.Verify(r => r.UpdateAsync(It.Is<UserNotification>(un =>
                un.Id == userNotificationId &&
                un.UserId == userId &&
                un.NotificationId == notificationId &&
                un.IsRead == true)), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_WhenUserNotificationExists_ShouldReturnTrue()
        {
            // Arrange
            var userNotificationId = Guid.NewGuid();

            _mockUserNotificationRepo.Setup(r => r.DeleteAsync(userNotificationId))
                .ReturnsAsync(true);

            // Act
            var result = await _userNotificationService.DeleteAsync(userNotificationId);

            // Assert
            result.Should().BeTrue();

            _mockUserNotificationRepo.Verify(r => r.DeleteAsync(userNotificationId), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_WhenUserNotificationDoesNotExist_ShouldReturnFalse()
        {
            // Arrange
            var userNotificationId = Guid.NewGuid();

            _mockUserNotificationRepo.Setup(r => r.DeleteAsync(userNotificationId))
                .ReturnsAsync(false);

            // Act
            var result = await _userNotificationService.DeleteAsync(userNotificationId);

            // Assert
            result.Should().BeFalse();

            _mockUserNotificationRepo.Verify(r => r.DeleteAsync(userNotificationId), Times.Once);
        }

        [Fact]
        public async Task MarkAsReadAsync_WhenUserNotificationExists_ShouldReturnTrue()
        {
            // Arrange
            var userNotificationId = Guid.NewGuid();

            _mockUserNotificationRepo.Setup(r => r.MarkAsReadAsync(userNotificationId))
                .ReturnsAsync(true);

            // Act
            var result = await _userNotificationService.MarkAsReadAsync(userNotificationId);

            // Assert
            result.Should().BeTrue();

            _mockUserNotificationRepo.Verify(r => r.MarkAsReadAsync(userNotificationId), Times.Once);
        }

        [Fact]
        public async Task MarkAsReadAsync_WhenUserNotificationDoesNotExist_ShouldReturnFalse()
        {
            // Arrange
            var userNotificationId = Guid.NewGuid();

            _mockUserNotificationRepo.Setup(r => r.MarkAsReadAsync(userNotificationId))
                .ReturnsAsync(false);

            // Act
            var result = await _userNotificationService.MarkAsReadAsync(userNotificationId);

            // Assert
            result.Should().BeFalse();

            _mockUserNotificationRepo.Verify(r => r.MarkAsReadAsync(userNotificationId), Times.Once);
        }

        [Fact]
        public async Task MarkAllAsReadByUserIdAsync_WhenUnreadNotificationsExist_ShouldReturnTrue()
        {
            // Arrange
            var userId = Guid.NewGuid();

            _mockUserNotificationRepo.Setup(r => r.MarkAllAsReadByUserIdAsync(userId))
                .ReturnsAsync(true);

            // Act
            var result = await _userNotificationService.MarkAllAsReadByUserIdAsync(userId);

            // Assert
            result.Should().BeTrue();

            _mockUserNotificationRepo.Verify(r => r.MarkAllAsReadByUserIdAsync(userId), Times.Once);
        }

        [Fact]
        public async Task MarkAllAsReadByUserIdAsync_WhenNoUnreadNotificationsExist_ShouldReturnFalse()
        {
            // Arrange
            var userId = Guid.NewGuid();

            _mockUserNotificationRepo.Setup(r => r.MarkAllAsReadByUserIdAsync(userId))
                .ReturnsAsync(false);

            // Act
            var result = await _userNotificationService.MarkAllAsReadByUserIdAsync(userId);

            // Assert
            result.Should().BeFalse();

            _mockUserNotificationRepo.Verify(r => r.MarkAllAsReadByUserIdAsync(userId), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_WithNonExistInvalid_ShouldReturnNull()
        {
            // Arrange
            var invalidId = Guid.NewGuid();
            _mockUserNotificationRepo.Setup(r => r.GetByIdAsync(invalidId))
                .ReturnsAsync((UserNotification?)null);

            // Act
            var result = await _userNotificationService.GetByIdAsync(invalidId);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByIdAsync_WhenIdIsEmpty_ShouldThrowArgumentException()
        {
            // Arrange
            var emptyId = Guid.Empty;

            // Act
            Func<Task> act = async () => await _userNotificationService.GetByIdAsync(emptyId);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task GetByUserIdAsync_WhenUserIdIsEmpty_ShouldReturnEmptyCollection()
        {
            // Arrange
            var emptyUserId = Guid.Empty;
            _mockUserNotificationRepo.Setup(r => r.GetByUserIdAsync(emptyUserId))
                .ReturnsAsync(new List<UserNotification>());

            // Act
            var result = await _userNotificationService.GetByUserIdAsync(emptyUserId);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
            _mockUserNotificationRepo.Verify(r => r.GetByUserIdAsync(emptyUserId), Times.Once);
        }

        [Fact]
        public async Task GetUnreadByUserIdAsync_WhenUserIdIsEmpty_ShouldReturnEmptyCollection()
        {
            // Arrange
            var emptyUserId = Guid.Empty;
            _mockUserNotificationRepo.Setup(r => r.GetUnreadByUserIdAsync(emptyUserId))
                .ReturnsAsync(new List<UserNotification>());

            // Act
            var result = await _userNotificationService.GetUnreadByUserIdAsync(emptyUserId);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
            _mockUserNotificationRepo.Verify(r => r.GetUnreadByUserIdAsync(emptyUserId), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_WithNullRequestDto_ShouldThrowArgumentNullException()
        {
            // Arrange
            UserNotificationRequestDto? nullRequestDto = null;

            // Act
            Func<Task> act = async () => await _userNotificationService.CreateAsync(nullRequestDto!);

            // Assert
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task UpdateAsync_WithEmptyId_ShouldCallRepoWithEmptyId()
        {
            // Arrange
            var emptyId = Guid.Empty;
            var requestDto = new UserNotificationRequestDto
            {
                UserId = Guid.NewGuid(),
                NotificationId = Guid.NewGuid(),
                IsRead = true
            };

            var updatedUserNotification = new UserNotification
            {
                Id = emptyId,
                UserId = requestDto.UserId,
                NotificationId = requestDto.NotificationId,
                IsRead = true
            };

            _mockUserNotificationRepo.Setup(r => r.UpdateAsync(It.IsAny<UserNotification>()))
                .ReturnsAsync(updatedUserNotification);

            // Act
            var result = await _userNotificationService.UpdateAsync(emptyId, requestDto);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(emptyId);
            _mockUserNotificationRepo.Verify(r => r.UpdateAsync(It.Is<UserNotification>(un => un.Id == emptyId)), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_WithEmptyId_ShouldReturnFalse()
        {
            // Arrange
            var emptyId = Guid.Empty;
            _mockUserNotificationRepo.Setup(r => r.DeleteAsync(emptyId))
                .ReturnsAsync(false);

            // Act
            var result = await _userNotificationService.DeleteAsync(emptyId);

            // Assert
            result.Should().BeFalse();
            _mockUserNotificationRepo.Verify(r => r.DeleteAsync(emptyId), Times.Once);
        }

        [Fact]
        public async Task MarkAsReadAsync_WithEmptyId_ShouldReturnFalse()
        {
            // Arrange
            var emptyId = Guid.Empty;
            _mockUserNotificationRepo.Setup(r => r.MarkAsReadAsync(emptyId))
                .ReturnsAsync(false);

            // Act
            var result = await _userNotificationService.MarkAsReadAsync(emptyId);

            // Assert
            result.Should().BeFalse();
            _mockUserNotificationRepo.Verify(r => r.MarkAsReadAsync(emptyId), Times.Once);
        }

        [Fact]
        public async Task MarkAllAsReadByUserIdAsync_WithEmptyUserId_ShouldReturnFalse()
        {
            // Arrange
            var emptyUserId = Guid.Empty;
            _mockUserNotificationRepo.Setup(r => r.MarkAllAsReadByUserIdAsync(emptyUserId))
                .ReturnsAsync(false);

            // Act
            var result = await _userNotificationService.MarkAllAsReadByUserIdAsync(emptyUserId);

            // Assert
            result.Should().BeFalse();
            _mockUserNotificationRepo.Verify(r => r.MarkAllAsReadByUserIdAsync(emptyUserId), Times.Once);
        }
    }
}