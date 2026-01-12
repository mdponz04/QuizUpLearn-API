using AutoMapper;
using BusinessLogic.DTOs;
using BusinessLogic.DTOs.QuizReportDtos;
using BusinessLogic.MappingProfile;
using BusinessLogic.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Repository.Entities;
using Repository.Interfaces;

namespace QuizUpLearn.Test.UnitTest
{
    public class QuizReportServiceTest
    {
        private readonly Mock<IQuizReportRepo> _mockQuizReportRepo;
        private readonly IMapper _mapper;
        private readonly QuizReportService _quizReportService;

        public QuizReportServiceTest()
        {
            _mockQuizReportRepo = new Mock<IQuizReportRepo>();

            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<MappingProfile>();
            }, new NullLoggerFactory());
            _mapper = mapperConfig.CreateMapper();

            _quizReportService = new QuizReportService(_mockQuizReportRepo.Object, _mapper);
        }

        [Fact]
        public async Task CreateAsync_WithValidRequest_ShouldReturnQuizReportResponse()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var quizId = Guid.NewGuid();
            var request = new RequestQuizReportDto
            {
                UserId = userId,
                QuizId = quizId,
                Description = "Sample quiz report description"
            };

            var createdQuizReport = new QuizReport
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                QuizId = quizId,
                Description = request.Description,
                CreatedAt = DateTime.UtcNow
            };

            _mockQuizReportRepo.Setup(r => r.CreateAsync(It.IsAny<QuizReport>()))
                .ReturnsAsync(createdQuizReport);

            // Act
            var result = await _quizReportService.CreateAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.UserId.Should().Be(userId);
            result.QuizId.Should().Be(quizId);
            result.Description.Should().Be(request.Description);
            result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

            _mockQuizReportRepo.Verify(r => r.CreateAsync(It.IsAny<QuizReport>()), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_WithNullRequest_ShouldThrowArgumentNullException()
        {
            // Act
            Func<Task> act = async () => await _quizReportService.CreateAsync(null!);

            // Assert
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task GetByIdAsync_WithNonExistentId_ShouldReturnNull()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            _mockQuizReportRepo.Setup(r => r.GetByIdAsync(nonExistentId))
                .ReturnsAsync((QuizReport?)null);

            // Act
            var result = await _quizReportService.GetByIdAsync(nonExistentId);

            // Assert
            result.Should().BeNull();

            _mockQuizReportRepo.Verify(r => r.GetByIdAsync(nonExistentId), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_WithInvalidId_ShouldReturnNull()
        {
            // Arrange
            var invalidId = Guid.NewGuid();

            _mockQuizReportRepo.Setup(r => r.GetByIdAsync(invalidId))
                .ReturnsAsync((QuizReport?)null);

            // Act
            var result = await _quizReportService.GetByIdAsync(invalidId);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetAllAsync_WithValidPagination_ShouldReturnPaginatedQuizReports()
        {
            // Arrange
            var pagination = new PaginationRequestDto { Page = 1, PageSize = 10 };
            var quizReports = new List<QuizReport>
            {
                new QuizReport
                {
                    Id = Guid.NewGuid(),
                    UserId = Guid.NewGuid(),
                    QuizId = Guid.NewGuid(),
                    Description = "First quiz report",
                    CreatedAt = DateTime.UtcNow
                },
                new QuizReport
                {
                    Id = Guid.NewGuid(),
                    UserId = Guid.NewGuid(),
                    QuizId = Guid.NewGuid(),
                    Description = "Second quiz report",
                    CreatedAt = DateTime.UtcNow.AddHours(-1)
                }
            };

            _mockQuizReportRepo.Setup(r => r.GetAllAsync(false))
                .ReturnsAsync(quizReports);

            // Act
            var result = await _quizReportService.GetAllAsync(pagination, false);

            // Assert
            result.Should().NotBeNull();
            result.Data.Should().HaveCount(2);
            result.Data[0].Description.Should().Be("First quiz report");
            result.Data[1].Description.Should().Be("Second quiz report");
            result.Pagination.TotalCount.Should().Be(2);

            _mockQuizReportRepo.Verify(r => r.GetAllAsync(false), Times.Once);
        }

        [Fact]
        public async Task GetAllAsync_WithIncludeDeleted_ShouldReturnAllQuizReports()
        {
            // Arrange
            var pagination = new PaginationRequestDto { Page = 1, PageSize = 10 };
            var quizReports = new List<QuizReport>
            {
                new QuizReport
                {
                    Id = Guid.NewGuid(),
                    UserId = Guid.NewGuid(),
                    QuizId = Guid.NewGuid(),
                    Description = "Active quiz report",
                    CreatedAt = DateTime.UtcNow
                },
                new QuizReport
                {
                    Id = Guid.NewGuid(),
                    UserId = Guid.NewGuid(),
                    QuizId = Guid.NewGuid(),
                    Description = "Deleted quiz report",
                    CreatedAt = DateTime.UtcNow.AddDays(-1),
                    DeletedAt = DateTime.UtcNow.AddHours(-1)
                }
            };

            _mockQuizReportRepo.Setup(r => r.GetAllAsync(true))
                .ReturnsAsync(quizReports);

            // Act
            var result = await _quizReportService.GetAllAsync(pagination, true);

            // Assert
            result.Should().NotBeNull();
            result.Data.Should().HaveCount(2);
            result.Data[0].Description.Should().Be("Active quiz report");
            result.Data[1].Description.Should().Be("Deleted quiz report");

            _mockQuizReportRepo.Verify(r => r.GetAllAsync(true), Times.Once);
        }

        [Fact]
        public async Task GetAllAsync_WithRepositoryException_ShouldThrowException()
        {
            // Arrange
            var pagination = new PaginationRequestDto { Page = 1, PageSize = 10 };

            _mockQuizReportRepo.Setup(r => r.GetAllAsync(It.IsAny<bool>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            Func<Task> act = async () => await _quizReportService.GetAllAsync(pagination);

            // Assert
            await act.Should().ThrowAsync<Exception>().WithMessage("Database error");
        }

        [Fact]
        public async Task GetByUserIdAsync_WithValidUserId_ShouldReturnUserQuizReports()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var pagination = new PaginationRequestDto { Page = 1, PageSize = 10 };
            var userQuizReports = new List<QuizReport>
            {
                new QuizReport
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    QuizId = Guid.NewGuid(),
                    Description = "User's first report",
                    CreatedAt = DateTime.UtcNow
                },
                new QuizReport
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    QuizId = Guid.NewGuid(),
                    Description = "User's second report",
                    CreatedAt = DateTime.UtcNow.AddDays(-1)
                }
            };

            _mockQuizReportRepo.Setup(r => r.GetByUserIdAsync(userId, false))
                .ReturnsAsync(userQuizReports);

            // Act
            var result = await _quizReportService.GetByUserIdAsync(userId, pagination, false);

            // Assert
            result.Should().NotBeNull();
            result.Data.Should().HaveCount(2);
            result.Data.Should().OnlyContain(x => x.UserId == userId);
            result.Data[0].Description.Should().Be("User's first report");
            result.Data[1].Description.Should().Be("User's second report");

            _mockQuizReportRepo.Verify(r => r.GetByUserIdAsync(userId, false), Times.Once);
        }

        [Fact]
        public async Task GetByUserIdAsync_WithInvalidUserId_ShouldReturnEmptyList()
        {
            // Arrange
            var invalidUserId = Guid.NewGuid();
            var pagination = new PaginationRequestDto { Page = 1, PageSize = 10 };

            _mockQuizReportRepo.Setup(r => r.GetByUserIdAsync(invalidUserId, false))
                .ReturnsAsync(new List<QuizReport>());

            // Act
            var result = await _quizReportService.GetByUserIdAsync(invalidUserId, pagination);

            // Assert
            result.Data.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByQuizIdAsync_WithValidQuizId_ShouldReturnQuizReports()
        {
            // Arrange
            var quizId = Guid.NewGuid();
            var pagination = new PaginationRequestDto { Page = 1, PageSize = 10 };
            var quizReports = new List<QuizReport>
            {
                new QuizReport
                {
                    Id = Guid.NewGuid(),
                    UserId = Guid.NewGuid(),
                    QuizId = quizId,
                    Description = "Report for quiz",
                    CreatedAt = DateTime.UtcNow
                },
                new QuizReport
                {
                    Id = Guid.NewGuid(),
                    UserId = Guid.NewGuid(),
                    QuizId = quizId,
                    Description = "Another report for same quiz",
                    CreatedAt = DateTime.UtcNow.AddHours(-2)
                }
            };

            _mockQuizReportRepo.Setup(r => r.GetByQuizIdAsync(quizId, false))
                .ReturnsAsync(quizReports);

            // Act
            var result = await _quizReportService.GetByQuizIdAsync(quizId, pagination, false);

            // Assert
            result.Should().NotBeNull();
            result.Data.Should().HaveCount(2);
            result.Data.Should().OnlyContain(x => x.QuizId == quizId);
            result.Data[0].Description.Should().Be("Report for quiz");
            result.Data[1].Description.Should().Be("Another report for same quiz");

            _mockQuizReportRepo.Verify(r => r.GetByQuizIdAsync(quizId, false), Times.Once);
        }

        [Fact]
        public async Task GetByQuizIdAsync_WithInvalidQuizId_ShouldReturnEmptyList()
        {
            // Arrange
            var invalidQuizId = Guid.NewGuid();
            var pagination = new PaginationRequestDto { Page = 1, PageSize = 10 };

            _mockQuizReportRepo.Setup(r => r.GetByQuizIdAsync(invalidQuizId, false))
                .ReturnsAsync(new List<QuizReport>());

            // Act
            var result = await _quizReportService.GetByQuizIdAsync(invalidQuizId, pagination);

            // Assert
            result.Data.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByUserAndQuizAsync_WithValidUserAndQuizIds_ShouldReturnQuizReport()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var quizId = Guid.NewGuid();
            var quizReport = new QuizReport
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                QuizId = quizId,
                Description = "Specific user-quiz report",
                CreatedAt = DateTime.UtcNow
            };

            _mockQuizReportRepo.Setup(r => r.GetByUserAndQuizAsync(userId, quizId, false))
                .ReturnsAsync(quizReport);

            // Act
            var result = await _quizReportService.GetByUserAndQuizAsync(userId, quizId, false);

            // Assert
            result.Should().NotBeNull();
            result!.UserId.Should().Be(userId);
            result.QuizId.Should().Be(quizId);
            result.Description.Should().Be("Specific user-quiz report");

            _mockQuizReportRepo.Verify(r => r.GetByUserAndQuizAsync(userId, quizId, false), Times.Once);
        }

        [Fact]
        public async Task GetByUserAndQuizAsync_WithNonExistentUserQuizCombo_ShouldReturnNull()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var quizId = Guid.NewGuid();

            _mockQuizReportRepo.Setup(r => r.GetByUserAndQuizAsync(userId, quizId, false))
                .ReturnsAsync((QuizReport?)null);

            // Act
            var result = await _quizReportService.GetByUserAndQuizAsync(userId, quizId, false);

            // Assert
            result.Should().BeNull();

            _mockQuizReportRepo.Verify(r => r.GetByUserAndQuizAsync(userId, quizId, false), Times.Once);
        }

        [Fact]
        public async Task GetByUserAndQuizAsync_WithNonExistentCombination_ShouldReturnNull()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var quizId = Guid.NewGuid();

            _mockQuizReportRepo.Setup(r => r.GetByUserAndQuizAsync(userId, quizId, false))
                .ReturnsAsync((QuizReport?)null);

            // Act
            var result = await _quizReportService.GetByUserAndQuizAsync(userId, quizId);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task HardDeleteAsync_WithValidId_ShouldReturnTrue()
        {
            // Arrange
            var quizReportId = Guid.NewGuid();

            _mockQuizReportRepo.Setup(r => r.HardDeleteAsync(quizReportId))
                .ReturnsAsync(true);

            // Act
            var result = await _quizReportService.HardDeleteAsync(quizReportId);

            // Assert
            result.Should().BeTrue();

            _mockQuizReportRepo.Verify(r => r.HardDeleteAsync(quizReportId), Times.Once);
        }

        [Fact]
        public async Task HardDeleteAsync_WithNonExistentId_ShouldReturnFalse()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            _mockQuizReportRepo.Setup(r => r.HardDeleteAsync(nonExistentId))
                .ReturnsAsync(false);

            // Act
            var result = await _quizReportService.HardDeleteAsync(nonExistentId);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task IsExistAsync_WithExistingUserQuizCombo_ShouldReturnTrue()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var quizId = Guid.NewGuid();

            _mockQuizReportRepo.Setup(r => r.IsExistAsync(userId, quizId))
                .ReturnsAsync(true);

            // Act
            var result = await _quizReportService.IsExistAsync(userId, quizId);

            // Assert
            result.Should().BeTrue();

            _mockQuizReportRepo.Verify(r => r.IsExistAsync(userId, quizId), Times.Once);
        }

        [Fact]
        public async Task IsExistAsync_WithNonExistentUserQuizCombo_ShouldReturnFalse()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var quizId = Guid.NewGuid();

            _mockQuizReportRepo.Setup(r => r.IsExistAsync(userId, quizId))
                .ReturnsAsync(false);

            // Act
            var result = await _quizReportService.IsExistAsync(userId, quizId);

            // Assert
            result.Should().BeFalse();

            _mockQuizReportRepo.Verify(r => r.IsExistAsync(userId, quizId), Times.Once);
        }

        [Fact]
        public async Task IsExistAsync_WithNonExistentCombination_ShouldReturnFalse()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var quizId = Guid.NewGuid();

            _mockQuizReportRepo.Setup(r => r.IsExistAsync(userId, quizId))
                .ReturnsAsync(false);

            // Act
            var result = await _quizReportService.IsExistAsync(userId, quizId);

            // Assert
            result.Should().BeFalse();
        }
    }
}