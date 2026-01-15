using AutoMapper;
using BusinessLogic.DTOs;
using BusinessLogic.DTOs.QuizGroupItemDtos;
using BusinessLogic.MappingProfile;
using BusinessLogic.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Repository.Entities;
using Repository.Interfaces;

namespace QuizUpLearn.Test.UnitTest
{
    public class QuizGroupItemServiceTest : BaseServiceTest
    {
        private readonly Mock<IQuizGroupItemRepo> _mockQuizGroupItemRepo;
        private readonly IMapper _mapper;
        private readonly QuizGroupItemService _quizGroupItemService;

        public QuizGroupItemServiceTest()
        {
            _mockQuizGroupItemRepo = new Mock<IQuizGroupItemRepo>();

            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<MappingProfile>();
            }, new NullLoggerFactory());
            _mapper = mapperConfig.CreateMapper();

            _quizGroupItemService = new QuizGroupItemService(_mockQuizGroupItemRepo.Object, _mapper);
        }

        [Fact]
        public async Task GetAllAsync_WithValidPagination_ShouldReturnPaginatedQuizGroupItems()
        {
            // Arrange
            var pagination = new PaginationRequestDto { Page = 1, PageSize = 10 };
            var quizGroupItems = new List<QuizGroupItem>
            {
                new QuizGroupItem 
                { 
                    Id = Guid.NewGuid(), 
                    Name = "Reading Passage 1", 
                    PassageText = "Sample passage text",
                    CreatedAt = DateTime.UtcNow,
                    Quizzes = new List<Quiz>()
                },
                new QuizGroupItem 
                { 
                    Id = Guid.NewGuid(), 
                    Name = "Listening Section", 
                    AudioUrl = "https://example.com/audio.mp3",
                    AudioScript = "Sample audio script",
                    CreatedAt = DateTime.UtcNow,
                    Quizzes = new List<Quiz>()
                }
            };

            _mockQuizGroupItemRepo.Setup(r => r.GetAllAsync())
                .ReturnsAsync(quizGroupItems);

            // Act
            var result = await _quizGroupItemService.GetAllGroupItemAsync(pagination);

            // Assert
            result.Should().NotBeNull();
            result.Data.Should().HaveCount(2);
            result.Data[0].Name.Should().Be("Reading Passage 1");
            result.Data[0].PassageText.Should().Be("Sample passage text");
            result.Data[1].Name.Should().Be("Listening Section");
            result.Data[1].AudioUrl.Should().Be("https://example.com/audio.mp3");
            result.Data[1].AudioScript.Should().Be("Sample audio script");
            result.Pagination.TotalCount.Should().Be(2);

            _mockQuizGroupItemRepo.Verify(r => r.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_WithValidId_ShouldReturnQuizGroupItem()
        {
            // Arrange
            var quizGroupItemId = Guid.NewGuid();
            var quizGroupItem = new QuizGroupItem
            {
                Id = quizGroupItemId,
                Name = "Grammar Section",
                PassageText = "Sample grammar passage",
                ImageUrl = "https://example.com/image.jpg",
                ImageDescription = "Sample image description",
                CreatedAt = DateTime.UtcNow,
                Quizzes = new List<Quiz>()
            };

            _mockQuizGroupItemRepo.Setup(r => r.GetByIdAsync(quizGroupItemId))
                .ReturnsAsync(quizGroupItem);

            // Act
            var result = await _quizGroupItemService.GetGroupItemByIdAsync(quizGroupItemId);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(quizGroupItemId);
            result.Name.Should().Be("Grammar Section");
            result.PassageText.Should().Be("Sample grammar passage");
            result.ImageUrl.Should().Be("https://example.com/image.jpg");
            result.ImageDescription.Should().Be("Sample image description");
            result.Quizzes.Should().NotBeNull();

            _mockQuizGroupItemRepo.Verify(r => r.GetByIdAsync(quizGroupItemId), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_WithValidRequest_ShouldReturnCreatedQuizGroupItem()
        {
            // Arrange
            var request = new RequestQuizGroupItemDto
            {
                Name = "New Reading Section",
                PassageText = "This is a new reading passage for testing purposes.",
                AudioUrl = "https://example.com/new-audio.mp3",
                AudioScript = "New audio script content",
                ImageUrl = "https://example.com/new-image.jpg",
                ImageDescription = "New image description"
            };

            var createdQuizGroupItem = new QuizGroupItem
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                PassageText = request.PassageText,
                AudioUrl = request.AudioUrl,
                AudioScript = request.AudioScript,
                ImageUrl = request.ImageUrl,
                ImageDescription = request.ImageDescription,
                CreatedAt = DateTime.UtcNow,
                Quizzes = new List<Quiz>()
            };

            _mockQuizGroupItemRepo.Setup(r => r.CreateAsync(It.IsAny<QuizGroupItem>()))
                .ReturnsAsync(createdQuizGroupItem);

            // Act
            var result = await _quizGroupItemService.CreateGroupItemAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Name.Should().Be(request.Name);
            result.PassageText.Should().Be(request.PassageText);
            result.AudioUrl.Should().Be(request.AudioUrl);
            result.AudioScript.Should().Be(request.AudioScript);
            result.ImageUrl.Should().Be(request.ImageUrl);
            result.ImageDescription.Should().Be(request.ImageDescription);
            result.Quizzes.Should().NotBeNull();

            _mockQuizGroupItemRepo.Verify(r => r.CreateAsync(It.Is<QuizGroupItem>(item =>
                item.Name == request.Name &&
                item.PassageText == request.PassageText &&
                item.AudioUrl == request.AudioUrl &&
                item.AudioScript == request.AudioScript &&
                item.ImageUrl == request.ImageUrl &&
                item.ImageDescription == request.ImageDescription)), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_WithValidData_ShouldReturnUpdatedQuizGroupItem()
        {
            // Arrange
            var quizGroupItemId = Guid.NewGuid();
            var request = new RequestQuizGroupItemDto
            {
                Name = "Updated Vocabulary Section",
                PassageText = "Updated passage text for vocabulary practice",
                AudioUrl = "https://example.com/updated-audio.mp3",
                AudioScript = "Updated audio script",
                ImageUrl = "https://example.com/updated-image.jpg",
                ImageDescription = "Updated image description"
            };

            var updatedQuizGroupItem = new QuizGroupItem
            {
                Id = quizGroupItemId,
                Name = request.Name,
                PassageText = request.PassageText,
                AudioUrl = request.AudioUrl,
                AudioScript = request.AudioScript,
                ImageUrl = request.ImageUrl,
                ImageDescription = request.ImageDescription,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow,
                Quizzes = new List<Quiz>()
            };

            _mockQuizGroupItemRepo.Setup(r => r.UpdateAsync(quizGroupItemId, It.IsAny<QuizGroupItem>()))
                .ReturnsAsync(updatedQuizGroupItem);

            // Act
            var result = await _quizGroupItemService.UpdateGroupItemAsync(quizGroupItemId, request);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(quizGroupItemId);
            result.Name.Should().Be(request.Name);
            result.PassageText.Should().Be(request.PassageText);
            result.AudioUrl.Should().Be(request.AudioUrl);
            result.AudioScript.Should().Be(request.AudioScript);
            result.ImageUrl.Should().Be(request.ImageUrl);
            result.ImageDescription.Should().Be(request.ImageDescription);
            result.UpdatedAt.Should().NotBeNull();

            _mockQuizGroupItemRepo.Verify(r => r.UpdateAsync(quizGroupItemId, It.Is<QuizGroupItem>(item =>
                item.Name == request.Name &&
                item.PassageText == request.PassageText &&
                item.AudioUrl == request.AudioUrl &&
                item.AudioScript == request.AudioScript &&
                item.ImageUrl == request.ImageUrl &&
                item.ImageDescription == request.ImageDescription)), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_WithValidId_ShouldReturnTrue()
        {
            // Arrange
            var quizGroupItemId = Guid.NewGuid();
            _mockQuizGroupItemRepo.Setup(r => r.DeleteAsync(quizGroupItemId))
                .ReturnsAsync(true);

            // Act
            var result = await _quizGroupItemService.DeleteGroupItemAsync(quizGroupItemId);

            // Assert
            result.Should().BeTrue();
            _mockQuizGroupItemRepo.Verify(r => r.DeleteAsync(quizGroupItemId), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_WithMinimalData_ShouldReturnCreatedQuizGroupItem()
        {
            // Arrange
            var request = new RequestQuizGroupItemDto
            {
                Name = "Minimal Section"
                // All other properties are optional and left null
            };

            var createdQuizGroupItem = new QuizGroupItem
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                PassageText = null,
                AudioUrl = null,
                AudioScript = null,
                ImageUrl = null,
                ImageDescription = null,
                CreatedAt = DateTime.UtcNow,
                Quizzes = new List<Quiz>()
            };

            _mockQuizGroupItemRepo.Setup(r => r.CreateAsync(It.IsAny<QuizGroupItem>()))
                .ReturnsAsync(createdQuizGroupItem);

            // Act
            var result = await _quizGroupItemService.CreateGroupItemAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Name.Should().Be(request.Name);
            result.PassageText.Should().BeNull();
            result.AudioUrl.Should().BeNull();
            result.AudioScript.Should().BeNull();
            result.ImageUrl.Should().BeNull();
            result.ImageDescription.Should().BeNull();

            _mockQuizGroupItemRepo.Verify(r => r.CreateAsync(It.Is<QuizGroupItem>(item =>
                item.Name == request.Name &&
                item.PassageText == null &&
                item.AudioUrl == null &&
                item.AudioScript == null &&
                item.ImageUrl == null &&
                item.ImageDescription == null)), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_WithPartialData_ShouldReturnUpdatedQuizGroupItem()
        {
            // Arrange
            var quizGroupItemId = Guid.NewGuid();
            var request = new RequestQuizGroupItemDto
            {
                Name = "Partially Updated Section",
                PassageText = "Only updating name and passage text"
                // Other properties left null
            };

            var updatedQuizGroupItem = new QuizGroupItem
            {
                Id = quizGroupItemId,
                Name = request.Name,
                PassageText = request.PassageText,
                AudioUrl = null,
                AudioScript = null,
                ImageUrl = null,
                ImageDescription = null,
                CreatedAt = DateTime.UtcNow.AddHours(-2),
                UpdatedAt = DateTime.UtcNow,
                Quizzes = new List<Quiz>()
            };

            _mockQuizGroupItemRepo.Setup(r => r.UpdateAsync(quizGroupItemId, It.IsAny<QuizGroupItem>()))
                .ReturnsAsync(updatedQuizGroupItem);

            // Act
            var result = await _quizGroupItemService.UpdateGroupItemAsync(quizGroupItemId, request);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(quizGroupItemId);
            result.Name.Should().Be(request.Name);
            result.PassageText.Should().Be(request.PassageText);
            result.AudioUrl.Should().BeNull();
            result.AudioScript.Should().BeNull();
            result.ImageUrl.Should().BeNull();
            result.ImageDescription.Should().BeNull();

            _mockQuizGroupItemRepo.Verify(r => r.UpdateAsync(quizGroupItemId, It.IsAny<QuizGroupItem>()), Times.Once);
        }

        [Fact]
        public async Task GetAllAsync_WithEmptyResult_ShouldReturnEmptyPaginatedList()
        {
            // Arrange
            var pagination = new PaginationRequestDto { Page = 1, PageSize = 10 };
            var emptyQuizGroupItems = new List<QuizGroupItem>();

            _mockQuizGroupItemRepo.Setup(r => r.GetAllAsync())
                .ReturnsAsync(emptyQuizGroupItems);

            // Act
            var result = await _quizGroupItemService.GetAllGroupItemAsync(pagination);

            // Assert
            result.Should().NotBeNull();
            result.Data.Should().BeEmpty();
            result.Pagination.TotalCount.Should().Be(0);

            _mockQuizGroupItemRepo.Verify(r => r.GetAllAsync(), Times.Once);
        }


        [Fact]
        public async Task GetGroupItemByIdAsync_WithEmptyId_ShouldThrowArgumentException()
        {
            // Arrange
            var emptyId = Guid.Empty;

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _quizGroupItemService.GetGroupItemByIdAsync(emptyId));

            exception.Message.Should().Be("Invalid Group Item ID");
            _mockQuizGroupItemRepo.Verify(r => r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task CreateGroupItemAsync_WithNullRequest_ShouldThrowArgumentNullException()
        {
            // Arrange
            RequestQuizGroupItemDto? request = null;

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(
                () => _quizGroupItemService.CreateGroupItemAsync(request!));

            exception.Message.Should().Contain("Request DTO cannot be null");
            _mockQuizGroupItemRepo.Verify(r => r.CreateAsync(It.IsAny<QuizGroupItem>()), Times.Never);
        }

        [Fact]
        public async Task UpdateGroupItemAsync_WithEmptyId_ShouldThrowArgumentException()
        {
            // Arrange
            var emptyId = Guid.Empty;
            var request = new RequestQuizGroupItemDto { Name = "Updated Vocabulary Section" };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _quizGroupItemService.UpdateGroupItemAsync(emptyId, request));

            exception.Message.Should().Be("Invalid Group Item ID");
            _mockQuizGroupItemRepo.Verify(r => r.UpdateAsync(It.IsAny<Guid>(), It.IsAny<QuizGroupItem>()), Times.Never);
        }

        [Fact]
        public async Task UpdateGroupItemAsync_WithNullRequest_ShouldThrowArgumentNullException()
        {
            // Arrange
            var id = Guid.NewGuid();
            RequestQuizGroupItemDto? request = null;

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(
                () => _quizGroupItemService.UpdateGroupItemAsync(id, request!));

            exception.Message.Should().Contain("Request DTO cannot be null");
            _mockQuizGroupItemRepo.Verify(r => r.UpdateAsync(It.IsAny<Guid>(), It.IsAny<QuizGroupItem>()), Times.Never);
        }

        [Fact]
        public async Task DeleteGroupItemAsync_WithEmptyId_ShouldThrowArgumentException()
        {
            // Arrange
            var emptyId = Guid.Empty;

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _quizGroupItemService.DeleteGroupItemAsync(emptyId));

            exception.Message.Should().Be("Invalid Group Item ID");
            _mockQuizGroupItemRepo.Verify(r => r.DeleteAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task GetAllGroupItemAsync_WithNegativePage_ShouldThrowValidationException()
        {
            // Arrange
            var pagination = new PaginationRequestDto { Page = -1, PageSize = 10 };

            // Act & Assert
            await Assert.ThrowsAsync<System.ComponentModel.DataAnnotations.ValidationException>(async () =>
                await _quizGroupItemService.GetAllGroupItemAsync(pagination));

            _mockQuizGroupItemRepo.Verify(r => r.GetAllAsync(), Times.Never);
        }

        [Fact]
        public async Task GetAllGroupItemAsync_WithNegativePageSize_ShouldThrowValidationException()
        {
            // Arrange
            var pagination = new PaginationRequestDto { Page = 1, PageSize = -5 };

            // Act & Assert
            await Assert.ThrowsAsync<System.ComponentModel.DataAnnotations.ValidationException>(async () =>
                await _quizGroupItemService.GetAllGroupItemAsync(pagination));

            _mockQuizGroupItemRepo.Verify(r => r.GetAllAsync(), Times.Never);
        }
    }
}