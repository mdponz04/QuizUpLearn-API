using AutoMapper;
using BusinessLogic.DTOs.EventDtos;
using BusinessLogic.Interfaces;
using BusinessLogic.MappingProfile;
using BusinessLogic.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Repository.Entities;
using Repository.Enums;
using Repository.Interfaces;

namespace QuizUpLearn.Test.UnitTest
{
    public class EventServiceTest : BaseControllerTest
    {
        private readonly Mock<IEventRepo> _mockEventRepo;
        private readonly Mock<IEventParticipantRepo> _mockEventParticipantRepo;
        private readonly Mock<IQuizSetRepo> _mockQuizSetRepo;
        private readonly Mock<IUserRepo> _mockUserRepo;
        private readonly Mock<IAccountRepo> _mockAccountRepo;
        private readonly Mock<IRealtimeGameService> _mockRealtimeGameService;
        private readonly Mock<IMailerSendService> _mockMailerSendService;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<IQuizAttemptRepo> _mockQuizAttemptRepo;
        private readonly IMapper _mapper;
        private readonly EventService _eventService;

        public EventServiceTest()
        {
            _mockEventRepo = new Mock<IEventRepo>();
            _mockEventParticipantRepo = new Mock<IEventParticipantRepo>();
            _mockQuizSetRepo = new Mock<IQuizSetRepo>();
            _mockUserRepo = new Mock<IUserRepo>();
            _mockAccountRepo = new Mock<IAccountRepo>();
            _mockRealtimeGameService = new Mock<IRealtimeGameService>();
            _mockMailerSendService = new Mock<IMailerSendService>();
            _mockConfiguration = new Mock<IConfiguration>();
            _mockQuizAttemptRepo = new Mock<IQuizAttemptRepo>();

            // Setup real AutoMapper with the actual mapping profile
            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<MappingProfile>();
            }, new NullLoggerFactory());
            _mapper = mapperConfig.CreateMapper();

            // Setup default logger
            var logger = new NullLogger<EventService>();

            _eventService = new EventService(
                _mockEventRepo.Object,
                _mockEventParticipantRepo.Object,
                _mockQuizSetRepo.Object,
                _mockUserRepo.Object,
                _mockAccountRepo.Object,
                _mockRealtimeGameService.Object,
                _mockMailerSendService.Object,
                _mockConfiguration.Object,
                logger,
                _mockQuizAttemptRepo.Object);
        }

        [Fact]
        public async Task CreateEventAsync_WithValidData_ShouldReturnEventResponse()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var quizSetId = Guid.NewGuid();
            var createEventDto = new CreateEventRequestDto
            {
                QuizSetId = quizSetId,
                Name = "Test Event",
                Description = "Test Description",
                StartDate = DateTime.UtcNow.AddDays(1),
                EndDate = DateTime.UtcNow.AddDays(2),
                MaxParticipants = 100
            };

            var quizSet = new QuizSet
            {
                Id = quizSetId,
                Title = "Test Quiz Set",
                QuizSetType = QuizSetTypeEnum.Event,
                CreatedAt = DateTime.UtcNow
            };

            var createdEvent = new Event
            {
                Id = Guid.NewGuid(),
                QuizSetId = createEventDto.QuizSetId,
                Name = createEventDto.Name,
                Description = createEventDto.Description,
                StartDate = createEventDto.StartDate.ToUniversalTime(),
                EndDate = createEventDto.EndDate.ToUniversalTime(),
                MaxParticipants = createEventDto.MaxParticipants,
                Status = "Upcoming",
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow,
                QuizSet = quizSet
            };

            _mockQuizSetRepo.Setup(r => r.GetQuizSetByIdAsync(quizSetId))
                .ReturnsAsync(quizSet);
            _mockEventRepo.Setup(r => r.CreateAsync(It.IsAny<Event>()))
                .ReturnsAsync(createdEvent);
            _mockEventParticipantRepo.Setup(r => r.CountParticipantsByEventIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(0);

            // Act
            var result = await _eventService.CreateEventAsync(userId, createEventDto);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(createdEvent.Id);
            result.Name.Should().Be(createEventDto.Name);
            result.Description.Should().Be(createEventDto.Description);
            result.QuizSetId.Should().Be(createEventDto.QuizSetId);
            result.MaxParticipants.Should().Be(createEventDto.MaxParticipants);
            result.Status.Should().Be("Upcoming");
            result.CreatedBy.Should().Be(userId);

            _mockQuizSetRepo.Verify(r => r.GetQuizSetByIdAsync(quizSetId), Times.Once);
            _mockEventRepo.Verify(r => r.CreateAsync(It.Is<Event>(e =>
                e.Name == createEventDto.Name &&
                e.QuizSetId == createEventDto.QuizSetId &&
                e.Status == "Upcoming")), Times.Once);
        }

        [Fact]
        public async Task CreateEventAsync_WithInvalidQuizSet_ShouldThrowException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var quizSetId = Guid.NewGuid();
            var createEventDto = new CreateEventRequestDto
            {
                QuizSetId = quizSetId,
                Name = "Test Event",
                StartDate = DateTime.UtcNow.AddDays(1),
                EndDate = DateTime.UtcNow.AddDays(2),
                MaxParticipants = 100
            };

            _mockQuizSetRepo.Setup(r => r.GetQuizSetByIdAsync(quizSetId))
                .ReturnsAsync((QuizSet?)null);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await _eventService.CreateEventAsync(userId, createEventDto));

            _mockQuizSetRepo.Verify(r => r.GetQuizSetByIdAsync(quizSetId), Times.Once);
            _mockEventRepo.Verify(r => r.CreateAsync(It.IsAny<Event>()), Times.Never);
        }

        [Fact]
        public async Task CreateEventAsync_WithNonEventQuizSetType_ShouldThrowException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var quizSetId = Guid.NewGuid();
            var createEventDto = new CreateEventRequestDto
            {
                QuizSetId = quizSetId,
                Name = "Test Event",
                StartDate = DateTime.UtcNow.AddDays(1),
                EndDate = DateTime.UtcNow.AddDays(2),
                MaxParticipants = 100
            };

            var quizSet = new QuizSet
            {
                Id = quizSetId,
                Title = "Test Quiz Set",
                QuizSetType = QuizSetTypeEnum.Practice, // Wrong type
                CreatedAt = DateTime.UtcNow
            };

            _mockQuizSetRepo.Setup(r => r.GetQuizSetByIdAsync(quizSetId))
                .ReturnsAsync(quizSet);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await _eventService.CreateEventAsync(userId, createEventDto));

            _mockEventRepo.Verify(r => r.CreateAsync(It.IsAny<Event>()), Times.Never);
        }

        [Fact]
        public async Task CreateEventAsync_WithInvalidDates_ShouldThrowException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var quizSetId = Guid.NewGuid();
            var createEventDto = new CreateEventRequestDto
            {
                QuizSetId = quizSetId,
                Name = "Test Event",
                StartDate = DateTime.UtcNow.AddDays(2),
                EndDate = DateTime.UtcNow.AddDays(1), // EndDate before StartDate
                MaxParticipants = 100
            };

            var quizSet = new QuizSet
            {
                Id = quizSetId,
                Title = "Test Quiz Set",
                QuizSetType = QuizSetTypeEnum.Event,
                CreatedAt = DateTime.UtcNow
            };

            _mockQuizSetRepo.Setup(r => r.GetQuizSetByIdAsync(quizSetId))
                .ReturnsAsync(quizSet);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await _eventService.CreateEventAsync(userId, createEventDto));

            _mockEventRepo.Verify(r => r.CreateAsync(It.IsAny<Event>()), Times.Never);
        }

        [Fact]
        public async Task GetEventByIdAsync_WithValidId_ShouldReturnEventResponse()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var eventEntity = new Event
            {
                Id = eventId,
                Name = "Test Event",
                Description = "Test Description",
                Status = "Upcoming",
                StartDate = DateTime.UtcNow.AddDays(1),
                EndDate = DateTime.UtcNow.AddDays(2),
                MaxParticipants = 100,
                CreatedAt = DateTime.UtcNow
            };

            _mockEventRepo.Setup(r => r.GetByIdWithDetailsAsync(eventId))
                .ReturnsAsync(eventEntity);
            _mockEventParticipantRepo.Setup(r => r.CountParticipantsByEventIdAsync(eventId))
                .ReturnsAsync(0);

            // Act
            var result = await _eventService.GetEventByIdAsync(eventId);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(eventId);
            result.Name.Should().Be(eventEntity.Name);
            result.Status.Should().Be(eventEntity.Status);

            _mockEventRepo.Verify(r => r.GetByIdWithDetailsAsync(eventId), Times.Once);
        }

        [Fact]
        public async Task GetEventByIdAsync_WithNonExistentId_ShouldReturnNull()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            _mockEventRepo.Setup(r => r.GetByIdWithDetailsAsync(eventId))
                .ReturnsAsync((Event?)null);

            // Act
            var result = await _eventService.GetEventByIdAsync(eventId);

            // Assert
            result.Should().BeNull();
            _mockEventRepo.Verify(r => r.GetByIdWithDetailsAsync(eventId), Times.Once);
        }

        [Fact]
        public async Task GetAllEventsAsync_ShouldReturnAllEvents()
        {
            // Arrange
            var events = new List<Event>
            {
                new Event
                {
                    Id = Guid.NewGuid(),
                    Name = "Event 1",
                    Status = "Upcoming",
                    CreatedAt = DateTime.UtcNow
                },
                new Event
                {
                    Id = Guid.NewGuid(),
                    Name = "Event 2",
                    Status = "Active",
                    CreatedAt = DateTime.UtcNow
                }
            };

            _mockEventRepo.Setup(r => r.GetAllAsync())
                .ReturnsAsync(events);
            _mockEventParticipantRepo.Setup(r => r.CountParticipantsByEventIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(0);

            // Act
            var result = await _eventService.GetAllEventsAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            _mockEventRepo.Verify(r => r.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task GetActiveEventsAsync_ShouldReturnActiveEvents()
        {
            // Arrange
            var events = new List<Event>
            {
                new Event
                {
                    Id = Guid.NewGuid(),
                    Name = "Active Event",
                    Status = "Active",
                    CreatedAt = DateTime.UtcNow
                }
            };

            _mockEventRepo.Setup(r => r.GetActiveEventsAsync())
                .ReturnsAsync(events);
            _mockEventParticipantRepo.Setup(r => r.CountParticipantsByEventIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(0);

            // Act
            var result = await _eventService.GetActiveEventsAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result.All(e => e.Status == "Active").Should().BeTrue();
            _mockEventRepo.Verify(r => r.GetActiveEventsAsync(), Times.Once);
        }

        [Fact]
        public async Task GetUpcomingEventsAsync_ShouldReturnUpcomingEvents()
        {
            // Arrange
            var events = new List<Event>
            {
                new Event
                {
                    Id = Guid.NewGuid(),
                    Name = "Upcoming Event",
                    Status = "Upcoming",
                    CreatedAt = DateTime.UtcNow
                }
            };

            _mockEventRepo.Setup(r => r.GetUpcomingEventsAsync())
                .ReturnsAsync(events);
            _mockEventParticipantRepo.Setup(r => r.CountParticipantsByEventIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(0);

            // Act
            var result = await _eventService.GetUpcomingEventsAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            _mockEventRepo.Verify(r => r.GetUpcomingEventsAsync(), Times.Once);
        }

        [Fact]
        public async Task GetMyEventsAsync_WithValidUserId_ShouldReturnUserEvents()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var events = new List<Event>
            {
                new Event
                {
                    Id = Guid.NewGuid(),
                    Name = "My Event",
                    CreatedBy = userId,
                    Status = "Upcoming",
                    CreatedAt = DateTime.UtcNow
                }
            };

            _mockEventRepo.Setup(r => r.GetEventsByCreatorAsync(userId))
                .ReturnsAsync(events);
            _mockEventParticipantRepo.Setup(r => r.CountParticipantsByEventIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(0);

            // Act
            var result = await _eventService.GetMyEventsAsync(userId);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result.All(e => e.CreatedBy == userId).Should().BeTrue();
            _mockEventRepo.Verify(r => r.GetEventsByCreatorAsync(userId), Times.Once);
        }

        [Fact]
        public async Task UpdateEventAsync_WithValidData_ShouldReturnUpdatedEventResponse()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var updateDto = new UpdateEventRequestDto
            {
                Name = "Updated Event Name",
                Description = "Updated Description",
                MaxParticipants = 200
            };

            var existingEvent = new Event
            {
                Id = eventId,
                Name = "Original Name",
                Description = "Original Description",
                Status = "Upcoming",
                StartDate = DateTime.UtcNow.AddDays(1),
                EndDate = DateTime.UtcNow.AddDays(2),
                MaxParticipants = 100,
                CreatedAt = DateTime.UtcNow
            };

            var updatedEvent = new Event
            {
                Id = eventId,
                Name = updateDto.Name!,
                Description = updateDto.Description!,
                Status = "Upcoming",
                StartDate = existingEvent.StartDate,
                EndDate = existingEvent.EndDate,
                MaxParticipants = updateDto.MaxParticipants!.Value,
                CreatedAt = existingEvent.CreatedAt,
                UpdatedAt = DateTime.UtcNow
            };

            _mockEventRepo.Setup(r => r.GetByIdAsync(eventId))
                .ReturnsAsync(existingEvent);
            _mockEventRepo.Setup(r => r.UpdateAsync(It.IsAny<Event>()))
                .ReturnsAsync(updatedEvent);
            _mockEventParticipantRepo.Setup(r => r.CountParticipantsByEventIdAsync(eventId))
                .ReturnsAsync(0);

            // Act
            var result = await _eventService.UpdateEventAsync(eventId, updateDto);

            // Assert
            result.Should().NotBeNull();
            result!.Name.Should().Be(updateDto.Name);
            result.Description.Should().Be(updateDto.Description);
            result.MaxParticipants.Should().Be(updateDto.MaxParticipants.Value);

            _mockEventRepo.Verify(r => r.GetByIdAsync(eventId), Times.Once);
            _mockEventRepo.Verify(r => r.UpdateAsync(It.IsAny<Event>()), Times.Once);
        }

        [Fact]
        public async Task UpdateEventAsync_WithNonExistentId_ShouldReturnNull()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var updateDto = new UpdateEventRequestDto
            {
                Name = "Updated Name"
            };

            _mockEventRepo.Setup(r => r.GetByIdAsync(eventId))
                .ReturnsAsync((Event?)null);

            // Act
            var result = await _eventService.UpdateEventAsync(eventId, updateDto);

            // Assert
            result.Should().BeNull();
            _mockEventRepo.Verify(r => r.UpdateAsync(It.IsAny<Event>()), Times.Never);
        }

        [Fact]
        public async Task UpdateEventAsync_WithActiveStatus_ShouldThrowException()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var updateDto = new UpdateEventRequestDto
            {
                Name = "Updated Name"
            };

            var existingEvent = new Event
            {
                Id = eventId,
                Name = "Original Name",
                Status = "Active", // Cannot update Active event
                CreatedAt = DateTime.UtcNow
            };

            _mockEventRepo.Setup(r => r.GetByIdAsync(eventId))
                .ReturnsAsync(existingEvent);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await _eventService.UpdateEventAsync(eventId, updateDto));

            _mockEventRepo.Verify(r => r.UpdateAsync(It.IsAny<Event>()), Times.Never);
        }

        [Fact]
        public async Task DeleteEventAsync_WithValidId_ShouldReturnTrue()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var eventEntity = new Event
            {
                Id = eventId,
                Name = "Test Event",
                Status = "Upcoming", // Can delete Upcoming event
                CreatedAt = DateTime.UtcNow
            };

            _mockEventRepo.Setup(r => r.GetByIdAsync(eventId))
                .ReturnsAsync(eventEntity);
            _mockEventRepo.Setup(r => r.DeleteAsync(eventId))
                .ReturnsAsync(true);

            // Act
            var result = await _eventService.DeleteEventAsync(eventId);

            // Assert
            result.Should().BeTrue();
            _mockEventRepo.Verify(r => r.GetByIdAsync(eventId), Times.Once);
            _mockEventRepo.Verify(r => r.DeleteAsync(eventId), Times.Once);
        }

        [Fact]
        public async Task DeleteEventAsync_WithNonExistentId_ShouldReturnFalse()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            _mockEventRepo.Setup(r => r.GetByIdAsync(eventId))
                .ReturnsAsync((Event?)null);

            // Act
            var result = await _eventService.DeleteEventAsync(eventId);

            // Assert
            result.Should().BeFalse();
            _mockEventRepo.Verify(r => r.DeleteAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task DeleteEventAsync_WithActiveStatus_ShouldThrowException()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var eventEntity = new Event
            {
                Id = eventId,
                Name = "Test Event",
                Status = "Active" // Cannot delete Active event
            };

            _mockEventRepo.Setup(r => r.GetByIdAsync(eventId))
                .ReturnsAsync(eventEntity);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await _eventService.DeleteEventAsync(eventId));

            _mockEventRepo.Verify(r => r.DeleteAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task GetEventParticipantsAsync_WithValidEventId_ShouldReturnParticipants()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var participants = new List<EventParticipant>
            {
                new EventParticipant
                {
                    Id = Guid.NewGuid(),
                    EventId = eventId,
                    ParticipantId = userId,
                    Score = 100,
                    Accuracy = 85.5,
                    Rank = 1,
                    JoinAt = DateTime.UtcNow,
                    Participant = new User
                    {
                        Id = userId,
                        Username = "testuser",
                        AvatarUrl = "https://example.com/avatar.jpg",
                        FullName = "Test User"
                    }
                }
            };

            _mockEventParticipantRepo.Setup(r => r.GetByEventIdAsync(eventId))
                .ReturnsAsync(participants);

            // Act
            var result = await _eventService.GetEventParticipantsAsync(eventId);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result.First().EventId.Should().Be(eventId);
            result.First().ParticipantId.Should().Be(userId);
            result.First().Score.Should().Be(100);

            _mockEventParticipantRepo.Verify(r => r.GetByEventIdAsync(eventId), Times.Once);
        }

        [Fact]
        public async Task JoinEventAsync_WithValidData_ShouldReturnTrue()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var eventEntity = new Event
            {
                Id = eventId,
                Name = "Test Event",
                Status = "Upcoming",
                MaxParticipants = 100,
                CreatedAt = DateTime.UtcNow
            };

            _mockEventRepo.Setup(r => r.GetByIdAsync(eventId))
                .ReturnsAsync(eventEntity);
            _mockEventParticipantRepo.Setup(r => r.IsParticipantInEventAsync(eventId, userId))
                .ReturnsAsync(false);
            _mockEventParticipantRepo.Setup(r => r.CountParticipantsByEventIdAsync(eventId))
                .ReturnsAsync(50); // Less than MaxParticipants
            _mockEventParticipantRepo.Setup(r => r.CreateAsync(It.IsAny<EventParticipant>()))
                .ReturnsAsync((EventParticipant p) => p);

            // Act
            var result = await _eventService.JoinEventAsync(eventId, userId);

            // Assert
            result.Should().BeTrue();
            _mockEventParticipantRepo.Verify(r => r.IsParticipantInEventAsync(eventId, userId), Times.Once);
            _mockEventParticipantRepo.Verify(r => r.CreateAsync(It.Is<EventParticipant>(p =>
                p.EventId == eventId && p.ParticipantId == userId)), Times.Once);
        }

        [Fact]
        public async Task JoinEventAsync_WhenAlreadyJoined_ShouldThrowException()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var eventEntity = new Event
            {
                Id = eventId,
                Status = "Upcoming",
                MaxParticipants = 100
            };

            _mockEventRepo.Setup(r => r.GetByIdAsync(eventId))
                .ReturnsAsync(eventEntity);
            _mockEventParticipantRepo.Setup(r => r.IsParticipantInEventAsync(eventId, userId))
                .ReturnsAsync(true); // Already joined

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await _eventService.JoinEventAsync(eventId, userId));

            _mockEventParticipantRepo.Verify(r => r.CreateAsync(It.IsAny<EventParticipant>()), Times.Never);
        }

        [Fact]
        public async Task JoinEventAsync_WhenEventFull_ShouldThrowException()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var eventEntity = new Event
            {
                Id = eventId,
                Status = "Upcoming",
                MaxParticipants = 100
            };

            _mockEventRepo.Setup(r => r.GetByIdAsync(eventId))
                .ReturnsAsync(eventEntity);
            _mockEventParticipantRepo.Setup(r => r.IsParticipantInEventAsync(eventId, userId))
                .ReturnsAsync(false);
            _mockEventParticipantRepo.Setup(r => r.CountParticipantsByEventIdAsync(eventId))
                .ReturnsAsync(100); // Full

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await _eventService.JoinEventAsync(eventId, userId));

            _mockEventParticipantRepo.Verify(r => r.CreateAsync(It.IsAny<EventParticipant>()), Times.Never);
        }

        [Fact]
        public async Task IsUserJoinedAsync_WhenJoined_ShouldReturnTrue()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            _mockEventParticipantRepo.Setup(r => r.IsParticipantInEventAsync(eventId, userId))
                .ReturnsAsync(true);

            // Act
            var result = await _eventService.IsUserJoinedAsync(eventId, userId);

            // Assert
            result.Should().BeTrue();
            _mockEventParticipantRepo.Verify(r => r.IsParticipantInEventAsync(eventId, userId), Times.Once);
        }

        [Fact]
        public async Task IsUserJoinedAsync_WhenNotJoined_ShouldReturnFalse()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            _mockEventParticipantRepo.Setup(r => r.IsParticipantInEventAsync(eventId, userId))
                .ReturnsAsync(false);

            // Act
            var result = await _eventService.IsUserJoinedAsync(eventId, userId);

            // Assert
            result.Should().BeFalse();
            _mockEventParticipantRepo.Verify(r => r.IsParticipantInEventAsync(eventId, userId), Times.Once);
        }

        [Fact]
        public async Task SyncPlayerScoreAsync_WithNewParticipant_ShouldCreateParticipant()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var score = 1000L;
            var accuracy = 85.5;

            _mockEventParticipantRepo.Setup(r => r.GetByEventAndParticipantAsync(eventId, userId))
                .ReturnsAsync((EventParticipant?)null);
            _mockEventParticipantRepo.Setup(r => r.CreateAsync(It.IsAny<EventParticipant>()))
                .ReturnsAsync((EventParticipant p) => p);

            // Act
            await _eventService.SyncPlayerScoreAsync(eventId, userId, score, accuracy);

            // Assert
            _mockEventParticipantRepo.Verify(r => r.GetByEventAndParticipantAsync(eventId, userId), Times.Once);
            _mockEventParticipantRepo.Verify(r => r.CreateAsync(It.Is<EventParticipant>(p =>
                p.EventId == eventId &&
                p.ParticipantId == userId &&
                p.Score == score &&
                p.Accuracy == accuracy)), Times.Once);
        }

        [Fact]
        public async Task SyncPlayerScoreAsync_WithExistingParticipant_ShouldUpdateParticipant()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var score = 1500L;
            var accuracy = 90.0;

            var existingParticipant = new EventParticipant
            {
                Id = Guid.NewGuid(),
                EventId = eventId,
                ParticipantId = userId,
                Score = 1000,
                Accuracy = 85.0,
                JoinAt = DateTime.UtcNow.AddHours(-1)
            };

            _mockEventParticipantRepo.Setup(r => r.GetByEventAndParticipantAsync(eventId, userId))
                .ReturnsAsync(existingParticipant);
            _mockEventParticipantRepo.Setup(r => r.UpdateAsync(It.IsAny<EventParticipant>()))
                .ReturnsAsync((EventParticipant p) => p);

            // Act
            await _eventService.SyncPlayerScoreAsync(eventId, userId, score, accuracy);

            // Assert
            _mockEventParticipantRepo.Verify(r => r.GetByEventAndParticipantAsync(eventId, userId), Times.Once);
            _mockEventParticipantRepo.Verify(r => r.UpdateAsync(It.Is<EventParticipant>(p =>
                p.Score == score &&
                p.Accuracy == accuracy)), Times.Once);
        }

        [Fact]
        public async Task UpdateEventStatusAsync_WithValidStatus_ShouldReturnTrue()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var eventEntity = new Event
            {
                Id = eventId,
                Name = "Test Event",
                Status = "Active",
                CreatedAt = DateTime.UtcNow
            };

            _mockEventRepo.Setup(r => r.GetByIdAsync(eventId))
                .ReturnsAsync(eventEntity);
            _mockEventRepo.Setup(r => r.UpdateAsync(It.IsAny<Event>()))
                .ReturnsAsync((Event e) => e);

            // Act
            var result = await _eventService.UpdateEventStatusAsync(eventId, "Ended");

            // Assert
            result.Should().BeTrue();
            _mockEventRepo.Verify(r => r.GetByIdAsync(eventId), Times.Once);
            _mockEventRepo.Verify(r => r.UpdateAsync(It.Is<Event>(e =>
                e.Status == "Ended")), Times.Once);
        }

        [Fact]
        public async Task UpdateEventStatusAsync_WithNonExistentId_ShouldReturnFalse()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            _mockEventRepo.Setup(r => r.GetByIdAsync(eventId))
                .ReturnsAsync((Event?)null);

            // Act
            var result = await _eventService.UpdateEventStatusAsync(eventId, "Ended");

            // Assert
            result.Should().BeFalse();
            _mockEventRepo.Verify(r => r.UpdateAsync(It.IsAny<Event>()), Times.Never);
        }

        [Fact]
        public async Task UpdateEventStatusAsync_WithInvalidStatus_ShouldThrowException()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var eventEntity = new Event
            {
                Id = eventId,
                Status = "Active"
            };

            _mockEventRepo.Setup(r => r.GetByIdAsync(eventId))
                .ReturnsAsync(eventEntity);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await _eventService.UpdateEventStatusAsync(eventId, "InvalidStatus"));

            _mockEventRepo.Verify(r => r.UpdateAsync(It.IsAny<Event>()), Times.Never);
        }

        [Fact]
        public async Task GetEventLeaderboardAsync_WithNoParticipants_ShouldReturnEmptyLeaderboard()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var eventEntity = new Event
            {
                Id = eventId,
                Name = "Test Event",
                Status = "Active",
                StartDate = DateTime.UtcNow.AddDays(-1),
                EndDate = DateTime.UtcNow.AddDays(1),
                QuizSetId = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow
            };

            _mockEventRepo.Setup(r => r.GetByIdWithDetailsAsync(eventId))
                .ReturnsAsync(eventEntity);
            _mockEventParticipantRepo.Setup(r => r.GetByEventIdAsync(eventId))
                .ReturnsAsync(new List<EventParticipant>());

            // Act
            var result = await _eventService.GetEventLeaderboardAsync(eventId);

            // Assert
            result.Should().NotBeNull();
            result.EventId.Should().Be(eventId);
            result.TotalParticipants.Should().Be(0);
            result.Rankings.Should().BeEmpty();
            result.TopPlayer.Should().BeNull();
        }

        [Fact]
        public async Task GetEventLeaderboardAsync_WithNonExistentEvent_ShouldThrowException()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            _mockEventRepo.Setup(r => r.GetByIdWithDetailsAsync(eventId))
                .ReturnsAsync((Event?)null);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await _eventService.GetEventLeaderboardAsync(eventId));
        }
    }
}

