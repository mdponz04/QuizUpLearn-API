using BusinessLogic.DTOs;
using BusinessLogic.DTOs.QuizGroupItemDtos;
using BusinessLogic.DTOs.QuizSetDtos;
using BusinessLogic.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using QuizUpLearn.API.Controllers;
using QuizUpLearn.API.Models;
using Repository.Enums;
using System.Net;

namespace QuizUpLearn.Test.IntegrationTest
{
    public class QuizSetControllerTest : BaseControllerTest
    {
        private readonly Mock<IQuizSetService> _mockQuizSetService;
        private readonly QuizSetController _controller;
        private readonly Guid _testUserId = Guid.NewGuid();
        private static readonly List<QuizSetResponseDto> StaticQuizSetsData = new()
        {
            new QuizSetResponseDto
            {
                Id = new Guid("11111111-1111-1111-1111-111111111111"),
                Title = "Xenophobic Bacteria Analysis",
                Description = "Obscure biological phenomenon study",
                QuizSetType = QuizSetTypeEnum.Placement,
                TotalQuestions = 15,
                CreatedBy = new Guid("22222222-2222-2222-2222-222222222222"),
                IsPublished = true,
                IsPremiumOnly = false,
                TotalAttempts = 3,
                AverageScore = 67.5m,
                CreatedAt = new DateTime(2024, 3, 15, 14, 30, 0),
                UpdatedAt = new DateTime(2024, 3, 16, 10, 15, 0),
                DeletedAt = null,
                IsRequireValidate = false,
                ValidatedAt = new DateTime(2024, 3, 15, 16, 0, 0),
                QuizGroupItems = new List<ResponseQuizGroupItemDto>()
            },
            new QuizSetResponseDto
            {
                Id = new Guid("33333333-3333-3333-3333-333333333333"),
                Title = "Zymurgy Fermentation Processes",
                Description = "Ancient brewing methodologies examination",
                QuizSetType = QuizSetTypeEnum.Tournament,
                TotalQuestions = 8,
                CreatedBy = new Guid("44444444-4444-4444-4444-444444444444"),
                IsPublished = false,
                IsPremiumOnly = true,
                TotalAttempts = 0,
                AverageScore = 0m,
                CreatedAt = new DateTime(2023, 11, 8, 9, 45, 0),
                UpdatedAt = null,
                DeletedAt = null,
                IsRequireValidate = true,
                ValidatedAt = null,
                QuizGroupItems = new List<ResponseQuizGroupItemDto>()
            },
            new QuizSetResponseDto
            {
                Id = new Guid("55555555-5555-5555-5555-555555555555"),
                Title = "Quixotic Paradigm Shifts",
                Description = "Unorthodox philosophical perspectives",
                QuizSetType = QuizSetTypeEnum.Practice,
                TotalQuestions = 22,
                CreatedBy = new Guid("66666666-6666-6666-6666-666666666666"),
                IsPublished = true,
                IsPremiumOnly = false,
                TotalAttempts = 12,
                AverageScore = 84.2m,
                CreatedAt = new DateTime(2024, 1, 3, 11, 20, 0),
                UpdatedAt = new DateTime(2024, 2, 14, 13, 45, 0),
                DeletedAt = new DateTime(2024, 2, 20, 16, 30, 0),
                IsRequireValidate = false,
                ValidatedAt = new DateTime(2024, 1, 4, 8, 15, 0),
                QuizGroupItems = new List<ResponseQuizGroupItemDto>()
            },
            new QuizSetResponseDto
            {
                Id = new Guid("77777777-7777-7777-7777-777777777777"),
                Title = "Onomatopoeia Linguistic Patterns",
                Description = "Sound symbolism across cultures",
                QuizSetType = QuizSetTypeEnum.Practice,
                TotalQuestions = 6,
                CreatedBy = new Guid("88888888-8888-8888-8888-888888888888"),
                IsPublished = false,
                IsPremiumOnly = false,
                TotalAttempts = 7,
                AverageScore = 52.1m,
                CreatedAt = new DateTime(2024, 5, 12, 16, 10, 0),
                UpdatedAt = null,
                DeletedAt = null,
                IsRequireValidate = true,
                ValidatedAt = null,
                QuizGroupItems = new List<ResponseQuizGroupItemDto>()
            },
            new QuizSetResponseDto
            {
                Id = new Guid("99999999-9999-9999-9999-999999999999"),
                Title = "Ephemeral Crystallography",
                Description = "Transient mineral formations",
                QuizSetType = QuizSetTypeEnum.Placement,
                TotalQuestions = 18,
                CreatedBy = new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                IsPublished = true,
                IsPremiumOnly = true,
                TotalAttempts = 25,
                AverageScore = 91.7m,
                CreatedAt = new DateTime(2023, 9, 27, 7, 30, 0),
                UpdatedAt = new DateTime(2024, 4, 10, 12, 0, 0),
                DeletedAt = null,
                IsRequireValidate = false,
                ValidatedAt = new DateTime(2023, 9, 28, 14, 20, 0),
                QuizGroupItems = new List<ResponseQuizGroupItemDto>()
            }
        };

        public QuizSetControllerTest()
        {
            _mockQuizSetService = new Mock<IQuizSetService>();
            _controller = new QuizSetController(_mockQuizSetService.Object);

            // Setup HttpContext for authorization tests
            var httpContext = new DefaultHttpContext();
            httpContext.Items["UserId"] = _testUserId;
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
        }

        #region CreateQuizSet Tests

        [Fact]
        public async Task CreateQuizSet_WithValidRequest_ShouldReturnCreatedAtAction()
        {
            // Arrange
            var requestDto = new QuizSetRequestDto
            {
                Title = "Test Quiz Set",
                Description = "Test Description",
                QuizSetType = QuizSetTypeEnum.Practice,
                CreatedBy = _testUserId,
                IsPublished = false,
                IsPremiumOnly = false,
                QuizGroupItems = new List<RequestQuizGroupItemDto>()
            };

            var expectedResponse = new QuizSetResponseDto
            {
                Id = Guid.NewGuid(),
                Title = requestDto.Title,
                Description = requestDto.Description,
                QuizSetType = requestDto.QuizSetType.Value,
                CreatedBy = _testUserId,
                IsPublished = false,
                IsPremiumOnly = false,
                CreatedAt = DateTime.UtcNow,
                QuizGroupItems = new List<ResponseQuizGroupItemDto>()
            };

            _mockQuizSetService.Setup(s => s.CreateQuizSetAsync(It.IsAny<QuizSetRequestDto>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.CreateQuizSet(requestDto);

            // Assert
            result.Should().NotBeNull();
            var createdAtActionResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
            createdAtActionResult.StatusCode.Should().Be(201);
            createdAtActionResult.Value.Should().BeEquivalentTo(expectedResponse);
            createdAtActionResult.ActionName.Should().Be(nameof(QuizSetController.GetQuizSetById));
            createdAtActionResult.RouteValues.Should().ContainKey("id").WhoseValue.Should().Be(expectedResponse.Id);

            _mockQuizSetService.Verify(s => s.CreateQuizSetAsync(It.Is<QuizSetRequestDto>(dto =>
                dto.Title == requestDto.Title &&
                dto.Description == requestDto.Description &&
                dto.QuizSetType == requestDto.QuizSetType &&
                dto.CreatedBy == requestDto.CreatedBy &&
                dto.IsPublished == requestDto.IsPublished &&
                dto.IsPremiumOnly == requestDto.IsPremiumOnly)), Times.Once);
        }

        [Fact]
        public async Task CreateQuizSet_WithNullDto_ShouldReturnException()
        {
            // Arrange
            QuizSetRequestDto requestDto = null!;

            // Act & Assert
            var exception = await Assert.ThrowsAsync<HttpException>(() => _controller.CreateQuizSet(requestDto));
            exception.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            exception.Message.Should().Be("Quiz set data is required");

            _mockQuizSetService.Verify(s => s.CreateQuizSetAsync(It.IsAny<QuizSetRequestDto>()), Times.Never);
        }

        [Fact]
        public async Task CreateQuizSet_WithEmptyQuizSetType_ShouldReturnException()
        {
            // Arrange
            var requestDto = new QuizSetRequestDto
            {
                Title = "Test Quiz Set",
                Description = "Test Description",
                QuizSetType = null,
                CreatedBy = _testUserId,
                IsPublished = false,
                IsPremiumOnly = false,
                QuizGroupItems = new List<RequestQuizGroupItemDto>()
            };

            // Simulate ModelState validation failure for missing QuizSetType
            _controller.ModelState.AddModelError("QuizSetType", "The QuizSetType field is required.");

            // Act & Assert
            var exception = await Assert.ThrowsAsync<HttpException>(() => _controller.CreateQuizSet(requestDto));
            exception.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            exception.Message.Should().Be("Invalid model state");

            _mockQuizSetService.Verify(s => s.CreateQuizSetAsync(It.IsAny<QuizSetRequestDto>()), Times.Never);
        }
        #endregion

        #region GetQuizSetById Tests

        [Fact]
        public async Task GetQuizSetById_WithExistingId_ShouldReturnOk()
        {
            // Arrange
            var quizSetId = Guid.NewGuid();
            var expectedResponse = new QuizSetResponseDto
            {
                Id = quizSetId,
                Title = "Test Quiz Set",
                Description = "Test Description",
                QuizSetType = QuizSetTypeEnum.Practice,
                CreatedBy = _testUserId,
                IsPublished = true,
                IsPremiumOnly = false,
                CreatedAt = DateTime.UtcNow,
                QuizGroupItems = new List<ResponseQuizGroupItemDto>()
            };

            _mockQuizSetService.Setup(s => s.GetQuizSetByIdAsync(quizSetId))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.GetQuizSetById(quizSetId);

            // Assert
            result.Should().NotBeNull();
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.StatusCode.Should().Be(200);
            okResult.Value.Should().BeEquivalentTo(expectedResponse);

            _mockQuizSetService.Verify(s => s.GetQuizSetByIdAsync(quizSetId), Times.Once);
        }

        [Fact]
        public async Task GetQuizSetById_WithNonExistingId_ShouldThrowHttpException()
        {
            // Arrange
            var quizSetId = Guid.NewGuid();
            _mockQuizSetService.Setup(s => s.GetQuizSetByIdAsync(quizSetId))
                .ReturnsAsync((QuizSetResponseDto)null!);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<HttpException>(() => _controller.GetQuizSetById(quizSetId));
            exception.StatusCode.Should().Be(HttpStatusCode.NotFound);
            exception.Message.Should().Be("Quiz set not found");

            _mockQuizSetService.Verify(s => s.GetQuizSetByIdAsync(quizSetId), Times.Once);
        }

        [Fact]
        public async Task GetQuizSetById_WithNullId_ShouldThrowHttpException()
        {
            // Arrange
            var emptyId = Guid.Empty;
            _mockQuizSetService.Setup(s => s.GetQuizSetByIdAsync(emptyId))
                .ReturnsAsync((QuizSetResponseDto)null!);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<HttpException>(() => _controller.GetQuizSetById(emptyId));
            exception.StatusCode.Should().Be(HttpStatusCode.NotFound);
            exception.Message.Should().Be("Quiz set not found");

            _mockQuizSetService.Verify(s => s.GetQuizSetByIdAsync(emptyId), Times.Once);
        }

        #endregion

        #region GetAllQuizSets Tests

        [Fact]
        public async Task GetAllQuizSets_WithValidRequest_ShouldReturnOk()
        {
            // Arrange - Using static test data
            var request = new PaginationRequestDto
            {
                Page = 1,
                PageSize = 10,
                SearchTerm = "test",
                SortBy = "Title",
                SortDirection = "asc"
            };

            var expectedResponse = CreatePaginationResponse(StaticQuizSetsData, request);

            _mockQuizSetService.Setup(s => s.GetAllQuizSetsAsync(It.IsAny<PaginationRequestDto>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.GetAllQuizSets(request);

            // Assert
            result.Should().NotBeNull();
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.StatusCode.Should().Be(200);
            okResult.Value.Should().BeEquivalentTo(expectedResponse);

            _mockQuizSetService.Verify(s => s.GetAllQuizSetsAsync(It.Is<PaginationRequestDto>(r =>
                r.Page == request.Page &&
                r.PageSize == request.PageSize &&
                r.SearchTerm == request.SearchTerm &&
                r.SortBy == request.SortBy &&
                r.SortDirection == request.SortDirection)), Times.Once);
        }

        [Fact]
        public async Task GetAllQuizSets_WithNullPagination_ShouldReturnOk()
        {
            // Arrange - Using static test data
            PaginationRequestDto? request = null!;

            var expectedResponse = CreatePaginationResponse(StaticQuizSetsData, new PaginationRequestDto { Page = 1, PageSize = 10 });

            _mockQuizSetService.Setup(s => s.GetAllQuizSetsAsync(request))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.GetAllQuizSets(request);

            // Assert
            result.Should().NotBeNull();
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeEquivalentTo(expectedResponse);

            _mockQuizSetService.Verify(s => s.GetAllQuizSetsAsync(request), Times.Once);
        }

        [Fact]
        public async Task GetAllQuizSets_WithFiltersAndSorting_ShouldReturnOk()
        {
            var request = new PaginationRequestDto
            {
                Page = 2,
                PageSize = 20,
                SearchTerm = "obscure",
                SortBy = "CreatedAt",
                SortDirection = "desc",
                Filters = new Dictionary<string, object>
                {
                    { "QuizSetType", QuizSetTypeEnum.Practice },
                    { "IsPublished", true }
                }
            };

            var filteredData = new List<QuizSetResponseDto>();
            var expectedResponse = CreatePaginationResponse(filteredData, request);

            _mockQuizSetService.Setup(s => s.GetAllQuizSetsAsync(request))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.GetAllQuizSets(request);

            // Assert
            result.Should().NotBeNull();
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeEquivalentTo(expectedResponse);

            _mockQuizSetService.Verify(s => s.GetAllQuizSetsAsync(request), Times.Once);
        }

        #endregion

        #region GetQuizSetsByCreator Tests

        [Fact]
        public async Task GetQuizSetsByCreator_WithCreatorId_ShouldReturnOk()
        {
            var creatorId = new Guid("22222222-2222-2222-2222-222222222222");
            var request = new PaginationRequestDto
            {
                Page = 1,
                PageSize = 10
            };

            var filteredData = StaticQuizSetsData.Where(q => q.CreatedBy == creatorId).ToList();
            var expectedResponse = CreatePaginationResponse(filteredData, request);

            _mockQuizSetService.Setup(s => s.GetQuizSetsByCreatorAsync(creatorId, request))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.GetQuizSetsByCreator(creatorId, request);

            // Assert
            result.Should().NotBeNull();
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeEquivalentTo(expectedResponse);

            _mockQuizSetService.Verify(s => s.GetQuizSetsByCreatorAsync(creatorId, request), Times.Once);
        }

        [Fact]
        public async Task GetQuizSetsByCreator_WithoutCreatorId_ShouldUseCurrentUser()
        {
            var request = new PaginationRequestDto
            {
                Page = 1,
                PageSize = 10
            };

            var filteredData = StaticQuizSetsData.Where(q => q.CreatedBy == _testUserId).ToList();
            var expectedResponse = CreatePaginationResponse(filteredData, request);

            _mockQuizSetService.Setup(s => s.GetQuizSetsByCreatorAsync(_testUserId, request))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.GetQuizSetsByCreator(null, request);

            // Assert
            result.Should().NotBeNull();
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeEquivalentTo(expectedResponse);

            _mockQuizSetService.Verify(s => s.GetQuizSetsByCreatorAsync(_testUserId, request), Times.Once);
        }

        #endregion

        #region UpdateQuizSet Tests

        [Fact]
        public async Task UpdateQuizSet_WithValidRequest_ShouldReturnOk()
        {
            // Arrange
            var quizSetId = Guid.NewGuid();
            var requestDto = new QuizSetRequestDto
            {
                Title = "Updated Quiz Set",
                Description = "Updated Description",
                QuizSetType = QuizSetTypeEnum.Practice,
                IsPublished = true,
                QuizGroupItems = new List<RequestQuizGroupItemDto>()
            };

            var expectedResponse = new QuizSetResponseDto
            {
                Id = quizSetId,
                Title = requestDto.Title,
                Description = requestDto.Description,
                QuizSetType = requestDto.QuizSetType.Value,
                IsPublished = requestDto.IsPublished.Value,
                UpdatedAt = DateTime.UtcNow,
                QuizGroupItems = new List<ResponseQuizGroupItemDto>()
            };

            _mockQuizSetService.Setup(s => s.UpdateQuizSetAsync(quizSetId, requestDto))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.UpdateQuizSet(quizSetId, requestDto);

            // Assert
            result.Should().NotBeNull();
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeEquivalentTo(expectedResponse);

            _mockQuizSetService.Verify(s => s.UpdateQuizSetAsync(quizSetId, requestDto), Times.Once);
        }

        [Fact]
        public async Task UpdateQuizSet_WithNonExistingId_ShouldThrowHttpException()
        {
            // Arrange
            var quizSetId = Guid.NewGuid();
            var requestDto = new QuizSetRequestDto
            {
                Title = "Updated Quiz Set",
                QuizGroupItems = new List<RequestQuizGroupItemDto>()
            };

            _mockQuizSetService.Setup(s => s.UpdateQuizSetAsync(quizSetId, requestDto))
                .ReturnsAsync((QuizSetResponseDto)null!);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<HttpException>(() => _controller.UpdateQuizSet(quizSetId, requestDto));
            exception.StatusCode.Should().Be(HttpStatusCode.NotFound);
            exception.Message.Should().Be("Quiz set not found");

            _mockQuizSetService.Verify(s => s.UpdateQuizSetAsync(quizSetId, requestDto), Times.Once);
        }

        [Fact]
        public async Task UpdateQuizSet_WithNullId_ShouldThrowHttpException()
        {
            // Arrange
            var emptyId = Guid.Empty;
            var requestDto = new QuizSetRequestDto
            {
                Title = "Updated Quiz Set",
                Description = "Updated Description",
                QuizGroupItems = new List<RequestQuizGroupItemDto>()
            };

            _mockQuizSetService.Setup(s => s.UpdateQuizSetAsync(emptyId, requestDto))
                .ReturnsAsync((QuizSetResponseDto)null!);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<HttpException>(() => _controller.UpdateQuizSet(emptyId, requestDto));
            exception.StatusCode.Should().Be(HttpStatusCode.NotFound);
            exception.Message.Should().Be("Quiz set not found");

            _mockQuizSetService.Verify(s => s.UpdateQuizSetAsync(emptyId, requestDto), Times.Once);
        }

        #endregion

        #region SoftDeleteQuizSet Tests

        [Fact]
        public async Task SoftDeleteQuizSet_WithExistingId_ShouldReturnNoContent()
        {
            // Arrange
            var quizSetId = Guid.NewGuid();
            _mockQuizSetService.Setup(s => s.SoftDeleteQuizSetAsync(quizSetId))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.SoftDeleteQuizSet(quizSetId);

            // Assert
            result.Should().BeOfType<NoContentResult>();
            var noContentResult = result as NoContentResult;
            noContentResult.StatusCode.Should().Be(204);

            _mockQuizSetService.Verify(s => s.SoftDeleteQuizSetAsync(quizSetId), Times.Once);
        }

        [Fact]
        public async Task SoftDeleteQuizSet_WithNonExistingId_ShouldThrowHttpException()
        {
            // Arrange
            var quizSetId = Guid.NewGuid();
            _mockQuizSetService.Setup(s => s.SoftDeleteQuizSetAsync(quizSetId))
                .ReturnsAsync(false);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<HttpException>(() => _controller.SoftDeleteQuizSet(quizSetId));
            exception.StatusCode.Should().Be(HttpStatusCode.NotFound);
            exception.Message.Should().Be("Quiz set not found");

            _mockQuizSetService.Verify(s => s.SoftDeleteQuizSetAsync(quizSetId), Times.Once);
        }

        [Fact]
        public async Task SoftDeleteQuizSet_WithEmptyId_ShouldThrowHttpException()
        {
            // Arrange
            var emptyId = Guid.Empty;
            _mockQuizSetService.Setup(s => s.SoftDeleteQuizSetAsync(emptyId))
                .ReturnsAsync(false);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<HttpException>(() => _controller.SoftDeleteQuizSet(emptyId));
            exception.StatusCode.Should().Be(HttpStatusCode.NotFound);
            exception.Message.Should().Be("Quiz set not found");

            _mockQuizSetService.Verify(s => s.SoftDeleteQuizSetAsync(emptyId), Times.Once);
        }
        #endregion

        #region HardDeleteQuizSet Tests

        [Fact]
        public async Task HardDeleteQuizSet_WithExistingId_ShouldReturnNoContent()
        {
            // Arrange
            var quizSetId = Guid.NewGuid();
            _mockQuizSetService.Setup(s => s.HardDeleteQuizSetAsync(quizSetId))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.HardDeleteQuizSet(quizSetId);

            // Assert
            result.Should().BeOfType<NoContentResult>();
            var noContentResult = result as NoContentResult;
            noContentResult.StatusCode.Should().Be(204);

            _mockQuizSetService.Verify(s => s.HardDeleteQuizSetAsync(quizSetId), Times.Once);
        }

        [Fact]
        public async Task HardDeleteQuizSet_WithNonExistingId_ShouldThrowHttpException()
        {
            // Arrange
            var quizSetId = Guid.NewGuid();
            _mockQuizSetService.Setup(s => s.HardDeleteQuizSetAsync(quizSetId))
                .ReturnsAsync(false);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<HttpException>(() => _controller.HardDeleteQuizSet(quizSetId));
            exception.StatusCode.Should().Be(HttpStatusCode.NotFound);
            exception.Message.Should().Be("Quiz set not found");

            _mockQuizSetService.Verify(s => s.HardDeleteQuizSetAsync(quizSetId), Times.Once);
        }

        [Fact]
        public async Task HardDeleteQuizSet_WithNullId_ShouldThrowHttpException()
        {
            // Arrange
            var emptyId = Guid.Empty;
            _mockQuizSetService.Setup(s => s.HardDeleteQuizSetAsync(emptyId))
                .ReturnsAsync(false);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<HttpException>(() => _controller.HardDeleteQuizSet(emptyId));
            exception.StatusCode.Should().Be(HttpStatusCode.NotFound);
            exception.Message.Should().Be("Quiz set not found");

            _mockQuizSetService.Verify(s => s.HardDeleteQuizSetAsync(emptyId), Times.Once);
        }
        #endregion

        #region RestoreQuizSet Tests

        [Fact]
        public async Task RestoreQuizSet_WithExistingId_ShouldReturnOk()
        {
            // Arrange
            var quizSetId = Guid.NewGuid();
            var expectedResponse = new QuizSetResponseDto
            {
                Id = quizSetId,
                Title = "Restored Quiz Set",
                DeletedAt = null,
                QuizGroupItems = new List<ResponseQuizGroupItemDto>()
            };

            _mockQuizSetService.Setup(s => s.RestoreQuizSetAsync(quizSetId))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.RestoreQuizSet(quizSetId);

            // Assert
            result.Should().NotBeNull();
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeEquivalentTo(expectedResponse);

            _mockQuizSetService.Verify(s => s.RestoreQuizSetAsync(quizSetId), Times.Once);
        }

        [Fact]
        public async Task RestoreQuizSet_WithNonExistingId_ShouldThrowHttpException()
        {
            // Arrange
            var quizSetId = Guid.NewGuid();
            _mockQuizSetService.Setup(s => s.RestoreQuizSetAsync(quizSetId))
                .ReturnsAsync((QuizSetResponseDto)null!);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<HttpException>(() => _controller.RestoreQuizSet(quizSetId));
            exception.StatusCode.Should().Be(HttpStatusCode.NotFound);
            exception.Message.Should().Be("Quiz set not found");

            _mockQuizSetService.Verify(s => s.RestoreQuizSetAsync(quizSetId), Times.Once);
        }

        [Fact]
        public async Task RestoreQuizSet_WithNullId_ShouldThrowHttpException()
        {
            // Arrange
            var emptyId = Guid.Empty;
            _mockQuizSetService.Setup(s => s.RestoreQuizSetAsync(emptyId))
                .ReturnsAsync((QuizSetResponseDto)null!);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<HttpException>(() => _controller.RestoreQuizSet(emptyId));
            exception.StatusCode.Should().Be(HttpStatusCode.NotFound);
            exception.Message.Should().Be("Quiz set not found");

            _mockQuizSetService.Verify(s => s.RestoreQuizSetAsync(emptyId), Times.Once);
        }
        #endregion

        #region Helper Methods

        private static PaginationResponseDto<QuizSetResponseDto> CreatePaginationResponse(
            List<QuizSetResponseDto> data, 
            PaginationRequestDto request)
        {
            var totalCount = data.Count;
            var totalPages = totalCount > 0 ? (int)Math.Ceiling((double)totalCount / request.PageSize) : 0;

            return new PaginationResponseDto<QuizSetResponseDto>
            {
                Data = data,
                Pagination = new PaginationMetadata
                {
                    CurrentPage = request.Page,
                    PageSize = request.PageSize,
                    TotalCount = totalCount,
                    TotalPages = totalPages,
                    HasPreviousPage = request.Page > 1,
                    HasNextPage = request.Page < totalPages,
                    SearchTerm = request.SearchTerm,
                    SortBy = request.SortBy,
                    SortDirection = request.SortDirection
                }
            };
        }

        #endregion
    }
}
