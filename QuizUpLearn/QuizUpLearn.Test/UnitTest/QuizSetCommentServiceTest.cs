using AutoMapper;
using BusinessLogic.DTOs;
using BusinessLogic.DTOs.QuizSetCommentDtos;
using BusinessLogic.MappingProfile;
using BusinessLogic.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Repository.Entities;
using Repository.Interfaces;

namespace QuizUpLearn.Test.UnitTest
{
    public class QuizSetCommentServiceTest : BaseServiceTest
    {
        private readonly Mock<IQuizSetCommentRepo> _mockQuizSetCommentRepo;
        private readonly IMapper _mapper;
        private readonly QuizSetCommentService _quizSetCommentService;

        public QuizSetCommentServiceTest()
        {
            _mockQuizSetCommentRepo = new Mock<IQuizSetCommentRepo>();

            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<MappingProfile>();
            }, new NullLoggerFactory());
            _mapper = mapperConfig.CreateMapper();

            _quizSetCommentService = new QuizSetCommentService(_mockQuizSetCommentRepo.Object, _mapper);
        }

        [Fact]
        public async Task CreateAsync_WithValidRequest_ShouldReturnResponseQuizSetCommentDto()
        {
            // Arrange
            var request = new RequestQuizSetCommentDto
            {
                UserId = Guid.NewGuid(),
                QuizSetId = Guid.NewGuid(),
                Content = "This is a great quiz set!"
            };

            var createdComment = new QuizSetComment
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                QuizSetId = request.QuizSetId,
                Content = request.Content,
                CreatedAt = DateTime.UtcNow
            };

            _mockQuizSetCommentRepo.Setup(r => r.CreateAsync(It.IsAny<QuizSetComment>()))
                .ReturnsAsync(createdComment);

            // Act
            var result = await _quizSetCommentService.CreateAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.UserId.Should().Be(request.UserId);
            result.QuizSetId.Should().Be(request.QuizSetId);
            result.Content.Should().Be(request.Content);
            result.Id.Should().Be(createdComment.Id);
            result.CreatedAt.Should().Be(createdComment.CreatedAt);

            _mockQuizSetCommentRepo.Verify(r => r.CreateAsync(It.Is<QuizSetComment>(c => 
                c.UserId == request.UserId && 
                c.QuizSetId == request.QuizSetId && 
                c.Content == request.Content)), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_WithInvalidUserId_ShouldThrowArgumentException()
        {
            // Arrange
            var request = new RequestQuizSetCommentDto
            {
                UserId = Guid.Empty,
                QuizSetId = Guid.NewGuid(),
                Content = "This is a great quiz set!"
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await _quizSetCommentService.CreateAsync(request));
        }
        [Fact]
        public async Task CreateAsync_WithInvalidQuizSetId_ShouldThrowArgumentException()
        {
            // Arrange
            var request = new RequestQuizSetCommentDto
            {
                UserId = Guid.NewGuid(),
                QuizSetId = Guid.Empty,
                Content = "This is a great quiz set!"
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await _quizSetCommentService.CreateAsync(request));
        }
        [Fact]
        public async Task CreateAsync_WithNullDto_ShouldThrowArgumentException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await _quizSetCommentService.CreateAsync(null!));
        }
        [Fact]
        public async Task GetByIdAsync_WithValidId_ShouldReturnResponseQuizSetCommentDto()
        {
            // Arrange
            var commentId = Guid.NewGuid();
            var comment = new QuizSetComment
            {
                Id = commentId,
                UserId = Guid.NewGuid(),
                QuizSetId = Guid.NewGuid(),
                Content = "Great quiz set!",
                CreatedAt = DateTime.UtcNow
            };

            _mockQuizSetCommentRepo.Setup(r => r.GetByIdAsync(commentId))
                .ReturnsAsync(comment);

            // Act
            var result = await _quizSetCommentService.GetByIdAsync(commentId);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(commentId);
            result.UserId.Should().Be(comment.UserId);
            result.QuizSetId.Should().Be(comment.QuizSetId);
            result.Content.Should().Be(comment.Content);
            result.CreatedAt.Should().Be(comment.CreatedAt);

            _mockQuizSetCommentRepo.Verify(r => r.GetByIdAsync(commentId), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_WithInvalidId_ShouldThrowArgumentException()
        {
            // Arrange
            var invalidId = Guid.Empty;

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await _quizSetCommentService.GetByIdAsync(invalidId));
        }

        [Fact]
        public async Task GetByIdAsync_WithNonExistentId_ShouldReturnNull()
        {
            // Arrange
            var commentId = Guid.NewGuid();

            _mockQuizSetCommentRepo.Setup(r => r.GetByIdAsync(commentId))
                .ReturnsAsync((QuizSetComment?)null);

            // Act
            var result = await _quizSetCommentService.GetByIdAsync(commentId);

            // Assert
            result.Should().BeNull();

            _mockQuizSetCommentRepo.Verify(r => r.GetByIdAsync(commentId), Times.Once);
        }

        [Fact]
        public async Task GetAllAsync_WithValidPagination_ShouldReturnPaginatedComments()
        {
            // Arrange
            var pagination = new PaginationRequestDto { Page = 1, PageSize = 10 };
            var comments = new List<QuizSetComment>
            {
                new QuizSetComment 
                { 
                    Id = Guid.NewGuid(), 
                    UserId = Guid.NewGuid(), 
                    QuizSetId = Guid.NewGuid(),
                    Content = "First comment",
                    CreatedAt = DateTime.UtcNow
                },
                new QuizSetComment 
                { 
                    Id = Guid.NewGuid(), 
                    UserId = Guid.NewGuid(), 
                    QuizSetId = Guid.NewGuid(),
                    Content = "Second comment",
                    CreatedAt = DateTime.UtcNow.AddMinutes(-5)
                }
            };

            _mockQuizSetCommentRepo.Setup(r => r.GetAllAsync(false))
                .ReturnsAsync(comments);

            // Act
            var result = await _quizSetCommentService.GetAllAsync(pagination);

            // Assert
            result.Should().NotBeNull();
            result.Data.Should().HaveCount(2);
            result.Data[0].Content.Should().Be("First comment");
            result.Data[1].Content.Should().Be("Second comment");
            result.Pagination.TotalCount.Should().Be(2);

            _mockQuizSetCommentRepo.Verify(r => r.GetAllAsync(false), Times.Once);
        }

        [Fact]
        public async Task GetAllAsync_WithInvalidPagination_ShouldThrowArgumentException()
        {
            // Arrange
            var invalidPagination = new PaginationRequestDto { Page = 0, PageSize = 10 }; // Invalid page number

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await _quizSetCommentService.GetAllAsync(invalidPagination));
        }

        [Fact]
        public async Task GetAllAsync_WithIncludeDeleted_ShouldReturnPaginatedCommentsIncludingDeleted()
        {
            // Arrange
            var pagination = new PaginationRequestDto { Page = 1, PageSize = 10 };
            var comments = new List<QuizSetComment>
            {
                new QuizSetComment 
                { 
                    Id = Guid.NewGuid(), 
                    UserId = Guid.NewGuid(), 
                    QuizSetId = Guid.NewGuid(),
                    Content = "Active comment",
                    CreatedAt = DateTime.UtcNow
                },
                new QuizSetComment 
                { 
                    Id = Guid.NewGuid(), 
                    UserId = Guid.NewGuid(), 
                    QuizSetId = Guid.NewGuid(),
                    Content = "Deleted comment",
                    CreatedAt = DateTime.UtcNow.AddMinutes(-5),
                    DeletedAt = DateTime.UtcNow.AddMinutes(-1)
                }
            };

            _mockQuizSetCommentRepo.Setup(r => r.GetAllAsync(true))
                .ReturnsAsync(comments);

            // Act
            var result = await _quizSetCommentService.GetAllAsync(pagination, includeDeleted: true);

            // Assert
            result.Should().NotBeNull();
            result.Data.Should().HaveCount(2);
            result.Data[0].Content.Should().Be("Active comment");
            result.Data[1].Content.Should().Be("Deleted comment");
            result.Data[1].DeletedAt.Should().NotBeNull();

            _mockQuizSetCommentRepo.Verify(r => r.GetAllAsync(true), Times.Once);
        }

        [Fact]
        public async Task GetByUserIdAsync_WithValidUserId_ShouldReturnUserComments()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var pagination = new PaginationRequestDto { Page = 1, PageSize = 10 };
            var userComments = new List<QuizSetComment>
            {
                new QuizSetComment 
                { 
                    Id = Guid.NewGuid(), 
                    UserId = userId, 
                    QuizSetId = Guid.NewGuid(),
                    Content = "User's first comment",
                    CreatedAt = DateTime.UtcNow
                },
                new QuizSetComment 
                { 
                    Id = Guid.NewGuid(), 
                    UserId = userId, 
                    QuizSetId = Guid.NewGuid(),
                    Content = "User's second comment",
                    CreatedAt = DateTime.UtcNow.AddMinutes(-10)
                }
            };

            _mockQuizSetCommentRepo.Setup(r => r.GetByUserIdAsync(userId, false))
                .ReturnsAsync(userComments);

            // Act
            var result = await _quizSetCommentService.GetByUserIdAsync(userId, pagination);

            // Assert
            result.Should().NotBeNull();
            result.Data.Should().HaveCount(2);
            result.Data.Should().AllSatisfy(c => c.UserId.Should().Be(userId));
            result.Data[0].Content.Should().Be("User's first comment");
            result.Data[1].Content.Should().Be("User's second comment");

            _mockQuizSetCommentRepo.Verify(r => r.GetByUserIdAsync(userId, false), Times.Once);
        }

        [Fact]
        public async Task GetByUserIdAsync_WithInvalidUserId_ShouldThrowArgumentException()
        {
            // Arrange
            var invalidUserId = Guid.Empty;
            var pagination = new PaginationRequestDto { Page = 1, PageSize = 10 };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await _quizSetCommentService.GetByUserIdAsync(invalidUserId, pagination));
        }

        [Fact]
        public async Task GetByQuizSetIdAsync_WithValidQuizSetId_ShouldReturnQuizSetComments()
        {
            // Arrange
            var quizSetId = Guid.NewGuid();
            var pagination = new PaginationRequestDto { Page = 1, PageSize = 10 };
            var quizSetComments = new List<QuizSetComment>
            {
                new QuizSetComment 
                { 
                    Id = Guid.NewGuid(), 
                    UserId = Guid.NewGuid(), 
                    QuizSetId = quizSetId,
                    Content = "First comment on quiz set",
                    CreatedAt = DateTime.UtcNow
                },
                new QuizSetComment 
                { 
                    Id = Guid.NewGuid(), 
                    UserId = Guid.NewGuid(), 
                    QuizSetId = quizSetId,
                    Content = "Second comment on quiz set",
                    CreatedAt = DateTime.UtcNow.AddHours(-1)
                }
            };

            _mockQuizSetCommentRepo.Setup(r => r.GetByQuizSetIdAsync(quizSetId, false))
                .ReturnsAsync(quizSetComments);

            // Act
            var result = await _quizSetCommentService.GetByQuizSetIdAsync(quizSetId, pagination);

            // Assert
            result.Should().NotBeNull();
            result.Data.Should().HaveCount(2);
            result.Data.Should().AllSatisfy(c => c.QuizSetId.Should().Be(quizSetId));
            result.Data[0].Content.Should().Be("First comment on quiz set");
            result.Data[1].Content.Should().Be("Second comment on quiz set");

            _mockQuizSetCommentRepo.Verify(r => r.GetByQuizSetIdAsync(quizSetId, false), Times.Once);
        }

        [Fact]
        public async Task GetByQuizSetIdAsync_WithInvalidQuizSetId_ShouldThrowArgumentException()
        {
            // Arrange
            var invalidQuizSetId = Guid.Empty;
            var pagination = new PaginationRequestDto { Page = 1, PageSize = 10 };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await _quizSetCommentService.GetByQuizSetIdAsync(invalidQuizSetId, pagination));
        }

        [Fact]
        public async Task UpdateAsync_WithValidIdAndRequest_ShouldReturnUpdatedComment()
        {
            // Arrange
            var commentId = Guid.NewGuid();
            var updateRequest = new RequestQuizSetCommentDto
            {
                UserId = Guid.NewGuid(),
                QuizSetId = Guid.NewGuid(),
                Content = "Updated comment content"
            };

            var updatedComment = new QuizSetComment
            {
                Id = commentId,
                UserId = updateRequest.UserId,
                QuizSetId = updateRequest.QuizSetId,
                Content = updateRequest.Content,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow
            };

            _mockQuizSetCommentRepo.Setup(r => r.UpdateAsync(commentId, It.IsAny<QuizSetComment>()))
                .ReturnsAsync(updatedComment);

            // Act
            var result = await _quizSetCommentService.UpdateAsync(commentId, updateRequest);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(commentId);
            result.Content.Should().Be(updateRequest.Content);
            result.UserId.Should().Be(updateRequest.UserId);
            result.QuizSetId.Should().Be(updateRequest.QuizSetId);
            result.UpdatedAt.Should().NotBeNull();

            _mockQuizSetCommentRepo.Verify(r => r.UpdateAsync(commentId, It.Is<QuizSetComment>(c => 
                c.UserId == updateRequest.UserId && 
                c.QuizSetId == updateRequest.QuizSetId && 
                c.Content == updateRequest.Content)), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_WithInvalidId_ShouldThrowArgumentException()
        {
            // Arrange
            var invalidId = Guid.Empty;
            var updateRequest = new RequestQuizSetCommentDto
            {
                UserId = Guid.NewGuid(),
                QuizSetId = Guid.NewGuid(),
                Content = "Updated comment content"
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await _quizSetCommentService.UpdateAsync(invalidId, updateRequest));
        }

        [Fact]
        public async Task UpdateAsync_WithNonExistentId_ShouldReturnNull()
        {
            // Arrange
            var commentId = Guid.NewGuid();
            var updateRequest = new RequestQuizSetCommentDto
            {
                UserId = Guid.NewGuid(),
                QuizSetId = Guid.NewGuid(),
                Content = "Updated content"
            };

            _mockQuizSetCommentRepo.Setup(r => r.UpdateAsync(commentId, It.IsAny<QuizSetComment>()))
                .ReturnsAsync((QuizSetComment?)null);

            // Act
            var result = await _quizSetCommentService.UpdateAsync(commentId, updateRequest);

            // Assert
            result.Should().BeNull();

            _mockQuizSetCommentRepo.Verify(r => r.UpdateAsync(commentId, It.IsAny<QuizSetComment>()), Times.Once);
        }

        [Fact]
        public async Task HardDeleteAsync_WithValidId_ShouldReturnTrue()
        {
            // Arrange
            var commentId = Guid.NewGuid();

            _mockQuizSetCommentRepo.Setup(r => r.HardDeleteAsync(commentId))
                .ReturnsAsync(true);

            // Act
            var result = await _quizSetCommentService.HardDeleteAsync(commentId);

            // Assert
            result.Should().BeTrue();

            _mockQuizSetCommentRepo.Verify(r => r.HardDeleteAsync(commentId), Times.Once);
        }

        [Fact]
        public async Task HardDeleteAsync_WithInvalidId_ShouldThrowArgumentException()
        {
            // Arrange
            var invalidId = Guid.Empty;

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await _quizSetCommentService.HardDeleteAsync(invalidId));
        }

        [Fact]
        public async Task HardDeleteAsync_WithNonExistentId_ShouldReturnFalse()
        {
            // Arrange
            var commentId = Guid.NewGuid();

            _mockQuizSetCommentRepo.Setup(r => r.HardDeleteAsync(commentId))
                .ReturnsAsync(false);

            // Act
            var result = await _quizSetCommentService.HardDeleteAsync(commentId);

            // Assert
            result.Should().BeFalse();

            _mockQuizSetCommentRepo.Verify(r => r.HardDeleteAsync(commentId), Times.Once);
        }

        [Fact]
        public async Task GetCommentCountByQuizSetAsync_WithValidQuizSetId_ShouldReturnCommentCount()
        {
            // Arrange
            var quizSetId = Guid.NewGuid();
            var expectedCount = 5;

            _mockQuizSetCommentRepo.Setup(r => r.GetCommentCountByQuizSetAsync(quizSetId))
                .ReturnsAsync(expectedCount);

            // Act
            var result = await _quizSetCommentService.GetCommentCountByQuizSetAsync(quizSetId);

            // Assert
            result.Should().Be(expectedCount);

            _mockQuizSetCommentRepo.Verify(r => r.GetCommentCountByQuizSetAsync(quizSetId), Times.Once);
        }

        [Fact]
        public async Task GetCommentCountByQuizSetAsync_WithInvalidQuizSetId_ShouldThrowArgumentException()
        {
            // Arrange
            var invalidQuizSetId = Guid.Empty;

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await _quizSetCommentService.GetCommentCountByQuizSetAsync(invalidQuizSetId));
        }

        [Fact]
        public async Task GetCommentCountByQuizSetAsync_WithQuizSetIdHavingNoComments_ShouldReturnZero()
        {
            // Arrange
            var quizSetId = Guid.NewGuid();

            _mockQuizSetCommentRepo.Setup(r => r.GetCommentCountByQuizSetAsync(quizSetId))
                .ReturnsAsync(0);

            // Act
            var result = await _quizSetCommentService.GetCommentCountByQuizSetAsync(quizSetId);

            // Assert
            result.Should().Be(0);

            _mockQuizSetCommentRepo.Verify(r => r.GetCommentCountByQuizSetAsync(quizSetId), Times.Once);
        }
    }
}