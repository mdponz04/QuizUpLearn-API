using BusinessLogic.DTOs;
using BusinessLogic.DTOs.QuizQuizSetDtos;
using BusinessLogic.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using QuizUpLearn.API.Controllers;
using QuizUpLearn.API.Models;
using System.Net;

namespace QuizUpLearn.Test.IntegrationTest
{
    public class QuizQuizSetControllerTest : BaseControllerTest
    {
        private readonly Mock<IQuizQuizSetService> _mockQuizQuizSetService;
        private readonly QuizQuizSetController _controller;

        public QuizQuizSetControllerTest()
        {
            _mockQuizQuizSetService = new Mock<IQuizQuizSetService>();
            _controller = new QuizQuizSetController(_mockQuizQuizSetService.Object);
            
            // Setup HttpContext for authorization attributes
            var httpContext = new DefaultHttpContext();
            httpContext.Items["UserId"] = Guid.NewGuid();
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
        }

        #region Add Quiz to QuizSet Tests
        [Fact]
        public async Task AddQuizzesToQuizSet_WithValidRequest_ShouldReturnOkResult()
        {
            // Arrange
            var quizSetId = Guid.NewGuid();
            var quizIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
            var request = new BulkAddQuizzesRequest { QuizIds = quizIds };

            _mockQuizQuizSetService.Setup(s => s.AddQuizzesToQuizSetAsync(quizIds, quizSetId))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.AddQuizzesToQuizSet(quizSetId, request);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value;
            response.Should().NotBeNull();
            
            _mockQuizQuizSetService.Verify(s => s.AddQuizzesToQuizSetAsync(quizIds, quizSetId), Times.Once);
        }

        [Fact]
        public async Task AddQuizzesToQuizSet_WithEmptyQuizIds_ShouldThrowHttpException()
        {
            // Arrange
            var quizSetId = Guid.NewGuid();
            var request = new BulkAddQuizzesRequest { QuizIds = new List<Guid>() };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<HttpException>(
                () => _controller.AddQuizzesToQuizSet(quizSetId, request));
            
            exception.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            exception.Message.Should().Be("Quiz IDs list cannot be empty");
        }

        [Fact]
        public async Task AddQuizzesToQuizSet_WithNullRequest_ShouldThrowHttpException()
        {
            // Arrange
            var quizSetId = Guid.NewGuid();
            BulkAddQuizzesRequest? request = null;

            // Act & Assert
            var exception = await Assert.ThrowsAsync<HttpException>(
                () => _controller.AddQuizzesToQuizSet(quizSetId, request!));
            
            exception.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            exception.Message.Should().Be("Quiz IDs list cannot be empty");
        }

        [Fact]
        public async Task AddQuizzesToQuizSet_WithServiceFailure_ShouldThrowHttpException()
        {
            // Arrange
            var quizSetId = Guid.NewGuid();
            var quizIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
            var request = new BulkAddQuizzesRequest { QuizIds = quizIds };

            _mockQuizQuizSetService.Setup(s => s.AddQuizzesToQuizSetAsync(quizIds, quizSetId))
                .ReturnsAsync(false);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<HttpException>(
                () => _controller.AddQuizzesToQuizSet(quizSetId, request));
            
            exception.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            exception.Message.Should().Be("Failed to add quizzes to quiz set");
        }

        #endregion

        #region Get by QuizSet ID Tests

        [Fact]
        public async Task GetByQuizSetId_WithValidId_ShouldReturnOkResult()
        {
            // Arrange
            var quizSetId = Guid.NewGuid();
            var pagination = new PaginationRequestDto { Page = 1, PageSize = 10 };
            var expectedResponse = new PaginationResponseDto<ResponseQuizQuizSetDto>
            {
                Data = new List<ResponseQuizQuizSetDto>
                {
                    new ResponseQuizQuizSetDto
                    {
                        Id = Guid.NewGuid(),
                        QuizId = Guid.NewGuid(),
                        QuizSetId = quizSetId,
                        CreatedAt = DateTime.UtcNow
                    }
                },
                Pagination = new PaginationMetadata()
            };

            _mockQuizQuizSetService.Setup(s => s.GetQuizQuizSetByQuizSetIdAsync(quizSetId, pagination, false))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.GetByQuizSetId(quizSetId, pagination, false);

            // Assert
            var actionResult = result.Should().BeOfType<ActionResult<PaginationResponseDto<ResponseQuizQuizSetDto>>>().Subject;
            var okResult = actionResult.Result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeOfType<PaginationResponseDto<ResponseQuizQuizSetDto>>().Subject;
            
            response.Data.Should().HaveCount(1);
            response.Data.First().QuizSetId.Should().Be(quizSetId);
            _mockQuizQuizSetService.Verify(s => s.GetQuizQuizSetByQuizSetIdAsync(quizSetId, pagination, false), Times.Once);
        }

        [Fact]
        public async Task GetByQuizSetId_WithIncludeDeleted_ShouldPassCorrectParameter()
        {
            // Arrange
            var quizSetId = Guid.NewGuid();
            var pagination = new PaginationRequestDto { Page = 1, PageSize = 10 };
            var expectedResponse = new PaginationResponseDto<ResponseQuizQuizSetDto>
            {
                Data = new List<ResponseQuizQuizSetDto>(),
                Pagination = new PaginationMetadata()
            };

            _mockQuizQuizSetService.Setup(s => s.GetQuizQuizSetByQuizSetIdAsync(quizSetId, pagination, true))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.GetByQuizSetId(quizSetId, pagination, true);

            // Assert
            var actionResult = result.Should().BeOfType<ActionResult<PaginationResponseDto<ResponseQuizQuizSetDto>>>().Subject;
            actionResult.Result.Should().BeOfType<OkObjectResult>();
            _mockQuizQuizSetService.Verify(s => s.GetQuizQuizSetByQuizSetIdAsync(quizSetId, pagination, true), Times.Once);
        }

        [Fact]
        public async Task GetByQuizSetId_WithEmptyResult_ShouldReturnEmptyList()
        {
            // Arrange
            var quizSetId = Guid.NewGuid();
            var pagination = new PaginationRequestDto { Page = 1, PageSize = 10 };
            var expectedResponse = new PaginationResponseDto<ResponseQuizQuizSetDto>
            {
                Data = new List<ResponseQuizQuizSetDto>(),
                Pagination = new PaginationMetadata()
            };

            _mockQuizQuizSetService.Setup(s => s.GetQuizQuizSetByQuizSetIdAsync(quizSetId, pagination, false))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.GetByQuizSetId(quizSetId, pagination, false);

            // Assert
            var actionResult = result.Should().BeOfType<ActionResult<PaginationResponseDto<ResponseQuizQuizSetDto>>>().Subject;
            var okResult = actionResult.Result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeOfType<PaginationResponseDto<ResponseQuizQuizSetDto>>().Subject;
            response.Data.Should().BeEmpty();
        }

        #endregion

        #region Delete Associations Tests

        [Fact]
        public async Task DeleteByQuizSetId_WithValidId_ShouldReturnOkResult()
        {
            // Arrange
            var quizSetId = Guid.NewGuid();
            _mockQuizQuizSetService.Setup(s => s.DeleteQuizQuizSetByQuizSetIdAsync(quizSetId))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.DeleteByQuizSetId(quizSetId);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value;
            response.Should().NotBeNull();
            
            _mockQuizQuizSetService.Verify(s => s.DeleteQuizQuizSetByQuizSetIdAsync(quizSetId), Times.Once);
        }

        [Fact]
        public async Task DeleteByQuizSetId_WithNonExistentId_ShouldThrowHttpException()
        {
            // Arrange
            var quizSetId = Guid.NewGuid();
            _mockQuizQuizSetService.Setup(s => s.DeleteQuizQuizSetByQuizSetIdAsync(quizSetId))
                .ReturnsAsync(false);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<HttpException>(
                () => _controller.DeleteByQuizSetId(quizSetId));
            
            exception.StatusCode.Should().Be(HttpStatusCode.NotFound);
            exception.Message.Should().Be("No associations found for the specified quiz set");
            _mockQuizQuizSetService.Verify(s => s.DeleteQuizQuizSetByQuizSetIdAsync(quizSetId), Times.Once);
        }

        [Fact]
        public async Task DeleteByQuizId_WithValidId_ShouldReturnOkResult()
        {
            // Arrange
            var quizId = Guid.NewGuid();
            _mockQuizQuizSetService.Setup(s => s.DeleteQuizQuizSetByQuizIdAsync(quizId))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.DeleteByQuizId(quizId);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value;
            response.Should().NotBeNull();
            
            _mockQuizQuizSetService.Verify(s => s.DeleteQuizQuizSetByQuizIdAsync(quizId), Times.Once);
        }

        [Fact]
        public async Task DeleteByQuizId_WithNonExistentId_ShouldThrowHttpException()
        {
            // Arrange
            var quizId = Guid.NewGuid();
            _mockQuizQuizSetService.Setup(s => s.DeleteQuizQuizSetByQuizIdAsync(quizId))
                .ReturnsAsync(false);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<HttpException>(
                () => _controller.DeleteByQuizId(quizId));
            
            exception.StatusCode.Should().Be(HttpStatusCode.NotFound);
            exception.Message.Should().Be("No associations found for the specified quiz");
            _mockQuizQuizSetService.Verify(s => s.DeleteQuizQuizSetByQuizIdAsync(quizId), Times.Once);
        }

        #endregion
    }
}
