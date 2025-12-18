using BusinessLogic.DTOs;
using BusinessLogic.DTOs.QuizReportDtos;
using BusinessLogic.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizUpLearn.API.Attributes;
using QuizUpLearn.API.Models;
using System.Net;

namespace QuizUpLearn.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class QuizReportController : ControllerBase
    {
        private readonly IQuizReportService _quizReportService;

        public QuizReportController(IQuizReportService quizReportService)
        {
            _quizReportService = quizReportService;
        }

        [HttpPost]
        [SubscriptionAndRoleAuthorize]
        public async Task<ActionResult<ResponseQuizReportDto>> Create([FromBody] RequestQuizReportDto dto)
        {
            if (dto.UserId == Guid.Empty)
            {
                dto.UserId = (Guid)HttpContext.Items["UserId"]!;
            }
            try
            {
                var result = await _quizReportService.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
            }
            catch (ArgumentException ex)
            {
                throw new HttpException(HttpStatusCode.BadRequest, ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                throw new HttpException(HttpStatusCode.Conflict, ex.Message);
            }
        }

        [HttpGet("{id}")]
        [SubscriptionAndRoleAuthorize("Administrator", "Moderator")]
        public async Task<ActionResult<ResponseQuizReportDto>> GetById(Guid id)
        {
            var result = await _quizReportService.GetByIdAsync(id);
            if (result == null)
                throw new HttpException(HttpStatusCode.NotFound, "Quiz report not found");

            return Ok(result);
        }

        [HttpPost("search")]
        [SubscriptionAndRoleAuthorize("Administrator", "Moderator")]
        public async Task<ActionResult<PaginationResponseDto<ResponseQuizReportDto>>> GetAll(
            [FromBody] PaginationRequestDto pagination, 
            [FromQuery] bool includeDeleted = false)
        {
            var result = await _quizReportService.GetAllAsync(pagination, includeDeleted);
            return Ok(result);
        }

        [HttpPost("user/search")]
        [SubscriptionAndRoleAuthorize]
        public async Task<ActionResult<PaginationResponseDto<ResponseQuizReportDto>>> GetByUserId(
            Guid? userId,
            [FromBody] PaginationRequestDto pagination, 
            [FromQuery] bool includeDeleted = false)
        {
            if (userId == null)
            {
                userId = (Guid)HttpContext.Items["UserId"]!;
            }
            var result = await _quizReportService.GetByUserIdAsync(userId.Value, pagination, includeDeleted);
            return Ok(result);
        }

        [HttpPost("quiz/{quizId}/search")]
        [SubscriptionAndRoleAuthorize("Administrator", "Moderator")]
        public async Task<ActionResult<PaginationResponseDto<ResponseQuizReportDto>>> GetByQuizId(
            Guid quizId,
            [FromBody] PaginationRequestDto pagination, 
            [FromQuery] bool includeDeleted = false)
        {
            var result = await _quizReportService.GetByQuizIdAsync(quizId, pagination, includeDeleted);
            return Ok(result);
        }

        [HttpGet("user/{userId}/quiz/{quizId}")]
        public async Task<ActionResult<ResponseQuizReportDto>> GetByUserAndQuiz(
            Guid userId, 
            Guid quizId, 
            [FromQuery] bool includeDeleted = false)
        {
            var result = await _quizReportService.GetByUserAndQuizAsync(userId, quizId, includeDeleted);
            if (result == null)
                throw new HttpException(HttpStatusCode.NotFound, "Quiz report not found");

            return Ok(result);
        }

        [HttpGet("exists/user/{userId}/quiz/{quizId}")]
        public async Task<ActionResult<bool>> IsReportExists(Guid userId, Guid quizId)
        {
            var exists = await _quizReportService.IsExistAsync(userId, quizId);
            return Ok(exists);
        }

        [HttpDelete("{id}/hard")]
        [SubscriptionAndRoleAuthorize("Moderator")]
        public async Task<ActionResult<bool>> HardDelete(Guid id)
        {
            var result = await _quizReportService.HardDeleteAsync(id);
            if (!result)
                throw new HttpException(HttpStatusCode.NotFound, "Quiz report not found");

            return Ok(result);
        }
    }
}