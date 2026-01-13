using AutoMapper;
using BusinessLogic.DTOs.NotificationDtos;
using Repository.Enums;
using BusinessLogic.MappingProfile;
using BusinessLogic.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Repository.Entities;
using Repository.Interfaces;

namespace QuizUpLearn.Test.UnitTest
{
    public class NotificationServiceTest : BaseControllerTest
    {
        private readonly Mock<INotificationRepo> _mockNotificationRepo;
        private readonly IMapper _mapper;
        private readonly NotificationService _notificationService;

        public NotificationServiceTest()
        {
            _mockNotificationRepo = new Mock<INotificationRepo>();

            // Setup real AutoMapper with the actual mapping profile
            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<MappingProfile>();
            }, new NullLoggerFactory());
            _mapper = mapperConfig.CreateMapper();

            _notificationService = new NotificationService(
                _mockNotificationRepo.Object,
                _mapper);
        }

        [Fact]
        public async Task GetAllAsync_WithExistingNotifications_ShouldReturnAllNotifications()
        {
            // Arrange
            var notifications = new List<Notification>
            {
                new Notification
                {
                    Id = Guid.NewGuid(),
                    Title = "Test Notification 1",
                    Message = "Test message 1",
                    Type = NotificationType.System,
                    CreatedAt = DateTime.UtcNow.AddDays(-1)
                },
                new Notification
                {
                    Id = Guid.NewGuid(),
                    Title = "Test Notification 2",
                    Message = "Test message 2",
                    Type = NotificationType.Quiz,
                    ActionUrl = "https://example.com/quiz",
                    CreatedAt = DateTime.UtcNow
                }
            };

            _mockNotificationRepo.Setup(r => r.GetAllAsync())
                .ReturnsAsync(notifications);

            // Act
            var result = await _notificationService.GetAllAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            
            var resultList = result.ToList();
            resultList[0].Title.Should().Be("Test Notification 1");
            resultList[0].Message.Should().Be("Test message 1");
            resultList[0].Type.Should().Be(NotificationType.System);
            
            resultList[1].Title.Should().Be("Test Notification 2");
            resultList[1].Message.Should().Be("Test message 2");
            resultList[1].Type.Should().Be(NotificationType.Quiz);
            resultList[1].ActionUrl.Should().Be("https://example.com/quiz");

            _mockNotificationRepo.Verify(r => r.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task GetAllAsync_WithNoNotifications_ShouldReturnEmptyCollection()
        {
            // Arrange
            var emptyNotifications = new List<Notification>();

            _mockNotificationRepo.Setup(r => r.GetAllAsync())
                .ReturnsAsync(emptyNotifications);

            // Act
            var result = await _notificationService.GetAllAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();

            _mockNotificationRepo.Verify(r => r.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_WithExistingId_ShouldReturnNotification()
        {
            // Arrange
            var notificationId = Guid.NewGuid();
            var notification = new Notification
            {
                Id = notificationId,
                Title = "Test Notification",
                Message = "Test message",
                Type = NotificationType.Event,
                ActionUrl = "https://example.com/event",
                ImageUrl = "https://example.com/image.jpg",
                Metadata = "{'key': 'value'}",
                ScheduledAt = DateTime.UtcNow.AddHours(1),
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _mockNotificationRepo.Setup(r => r.GetByIdAsync(notificationId))
                .ReturnsAsync(notification);

            // Act
            var result = await _notificationService.GetByIdAsync(notificationId);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(notificationId);
            result.Title.Should().Be("Test Notification");
            result.Message.Should().Be("Test message");
            result.Type.Should().Be(NotificationType.Event);
            result.ActionUrl.Should().Be("https://example.com/event");
            result.ImageUrl.Should().Be("https://example.com/image.jpg");
            result.Metadata.Should().Be("{'key': 'value'}");
            result.ScheduledAt.Should().Be(notification.ScheduledAt);
            result.ExpiresAt.Should().Be(notification.ExpiresAt);
            result.CreatedAt.Should().Be(notification.CreatedAt);
            result.UpdatedAt.Should().Be(notification.UpdatedAt);

            _mockNotificationRepo.Verify(r => r.GetByIdAsync(notificationId), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_WithNonExistingId_ShouldReturnNull()
        {
            // Arrange
            var notificationId = Guid.NewGuid();

            _mockNotificationRepo.Setup(r => r.GetByIdAsync(notificationId))
                .ReturnsAsync((Notification?)null);

            // Act
            var result = await _notificationService.GetByIdAsync(notificationId);

            // Assert
            result.Should().BeNull();

            _mockNotificationRepo.Verify(r => r.GetByIdAsync(notificationId), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_WithValidRequest_ShouldReturnCreatedNotification()
        {
            // Arrange
            var requestDto = new NotificationRequestDto
            {
                Title = "New Notification",
                Message = "New notification message",
                Type = NotificationType.Tournament
            };

            var createdNotification = new Notification
            {
                Id = Guid.NewGuid(),
                Title = "New Notification",
                Message = "New notification message",
                Type = NotificationType.Tournament,
                CreatedAt = DateTime.UtcNow
            };

            _mockNotificationRepo.Setup(r => r.CreateAsync(It.IsAny<Notification>()))
                .ReturnsAsync(createdNotification);

            // Act
            var result = await _notificationService.CreateAsync(requestDto);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(createdNotification.Id);
            result.Title.Should().Be("New Notification");
            result.Message.Should().Be("New notification message");
            result.Type.Should().Be(NotificationType.Tournament);
            result.CreatedAt.Should().Be(createdNotification.CreatedAt);

            _mockNotificationRepo.Verify(r => r.CreateAsync(It.Is<Notification>(n => 
                n.Title == requestDto.Title && 
                n.Message == requestDto.Message && 
                n.Type == requestDto.Type)), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_WithComplexRequest_ShouldReturnCreatedNotification()
        {
            // Arrange
            var requestDto = new NotificationRequestDto
            {
                Title = "Achievement Unlocked",
                Message = "Congratulations! You've completed 10 quizzes!",
                Type = NotificationType.Achievement
            };

            var createdNotification = new Notification
            {
                Id = Guid.NewGuid(),
                Title = "Achievement Unlocked",
                Message = "Congratulations! You've completed 10 quizzes!",
                Type = NotificationType.Achievement,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _mockNotificationRepo.Setup(r => r.CreateAsync(It.IsAny<Notification>()))
                .ReturnsAsync(createdNotification);

            // Act
            var result = await _notificationService.CreateAsync(requestDto);

            // Assert
            result.Should().NotBeNull();
            result.Title.Should().Be("Achievement Unlocked");
            result.Message.Should().Be("Congratulations! You've completed 10 quizzes!");
            result.Type.Should().Be(NotificationType.Achievement);

            _mockNotificationRepo.Verify(r => r.CreateAsync(It.IsAny<Notification>()), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_WithValidRequest_ShouldReturnUpdatedNotification()
        {
            // Arrange
            var notificationId = Guid.NewGuid();
            var requestDto = new NotificationRequestDto
            {
                Title = "Updated Notification",
                Message = "Updated notification message",
                Type = NotificationType.Event
            };

            var updatedNotification = new Notification
            {
                Id = notificationId,
                Title = "Updated Notification",
                Message = "Updated notification message",
                Type = NotificationType.Event,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow
            };

            _mockNotificationRepo.Setup(r => r.UpdateAsync(It.IsAny<Notification>()))
                .ReturnsAsync(updatedNotification);

            // Act
            var result = await _notificationService.UpdateAsync(notificationId, requestDto);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(notificationId);
            result.Title.Should().Be("Updated Notification");
            result.Message.Should().Be("Updated notification message");
            result.Type.Should().Be(NotificationType.Event);
            result.UpdatedAt.Should().Be(updatedNotification.UpdatedAt);

            _mockNotificationRepo.Verify(r => r.UpdateAsync(It.Is<Notification>(n => 
                n.Id == notificationId && 
                n.Title == requestDto.Title && 
                n.Message == requestDto.Message && 
                n.Type == requestDto.Type)), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_WithEmptyId_ShouldThrowArgumentException()
        {
            // Arrange
            var emptyId = Guid.Empty;
            var requestDto = new NotificationRequestDto
            {
                Title = "Updated Notification",
                Message = "Updated notification message",
                Type = NotificationType.Event
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _notificationService.UpdateAsync(emptyId, requestDto));

            exception.Message.Should().Be("Id cannot be empty");

            // Verify that the repository method was never called
            _mockNotificationRepo.Verify(r => r.UpdateAsync(It.IsAny<Notification>()), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_WithDifferentNotificationType_ShouldReturnUpdatedNotification()
        {
            // Arrange
            var notificationId = Guid.NewGuid();
            var requestDto = new NotificationRequestDto
            {
                Title = "Security Alert",
                Message = "Your password has been changed",
                Type = NotificationType.Security
            };

            var updatedNotification = new Notification
            {
                Id = notificationId,
                Title = "Security Alert",
                Message = "Your password has been changed",
                Type = NotificationType.Security,
                CreatedAt = DateTime.UtcNow.AddHours(-2),
                UpdatedAt = DateTime.UtcNow
            };

            _mockNotificationRepo.Setup(r => r.UpdateAsync(It.IsAny<Notification>()))
                .ReturnsAsync(updatedNotification);

            // Act
            var result = await _notificationService.UpdateAsync(notificationId, requestDto);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(notificationId);
            result.Title.Should().Be("Security Alert");
            result.Message.Should().Be("Your password has been changed");
            result.Type.Should().Be(NotificationType.Security);

            _mockNotificationRepo.Verify(r => r.UpdateAsync(It.IsAny<Notification>()), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_WithExistingId_ShouldReturnTrue()
        {
            // Arrange
            var notificationId = Guid.NewGuid();

            _mockNotificationRepo.Setup(r => r.DeleteAsync(notificationId))
                .ReturnsAsync(true);

            // Act
            var result = await _notificationService.DeleteAsync(notificationId);

            // Assert
            result.Should().BeTrue();

            _mockNotificationRepo.Verify(r => r.DeleteAsync(notificationId), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_WithValidId_ShouldReturnTrue()
        {
            // Arrange
            var notificationId = Guid.NewGuid();

            _mockNotificationRepo.Setup(r => r.DeleteAsync(notificationId))
                .ReturnsAsync(true);

            // Act
            var result = await _notificationService.DeleteAsync(notificationId);

            // Assert
            result.Should().BeTrue();

            _mockNotificationRepo.Verify(r => r.DeleteAsync(notificationId), Times.Once);
        }

        [Theory]
        [InlineData(NotificationType.System)]
        [InlineData(NotificationType.Quiz)]
        [InlineData(NotificationType.Event)]
        [InlineData(NotificationType.Tournament)]
        [InlineData(NotificationType.Achievement)]
        [InlineData(NotificationType.Subscription)]
        [InlineData(NotificationType.Social)]
        [InlineData(NotificationType.Reminder)]
        [InlineData(NotificationType.Security)]
        [InlineData(NotificationType.Marketing)]
        public async Task CreateAsync_WithAllNotificationTypes_ShouldReturnCreatedNotification(NotificationType notificationType)
        {
            // Arrange
            var requestDto = new NotificationRequestDto
            {
                Title = "New Notification",
                Message = "This is a notification message",
                Type = notificationType
            };

            var createdNotification = new Notification
            {
                Id = Guid.NewGuid(),
                Title = requestDto.Title,
                Message = requestDto.Message,
                Type = notificationType,
                CreatedAt = DateTime.UtcNow
            };

            _mockNotificationRepo.Setup(r => r.CreateAsync(It.IsAny<Notification>()))
                .ReturnsAsync(createdNotification);

            // Act
            var result = await _notificationService.CreateAsync(requestDto);

            // Assert
            result.Should().NotBeNull();
            result.Title.Should().Be("New Notification");
            result.Message.Should().Be("This is a notification message");
            result.Type.Should().Be(notificationType);

            _mockNotificationRepo.Verify(r => r.CreateAsync(It.IsAny<Notification>()), Times.Once);
        }
    }
}