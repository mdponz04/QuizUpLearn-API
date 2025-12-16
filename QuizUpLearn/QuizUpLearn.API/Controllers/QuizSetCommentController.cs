using BusinessLogic.DTOs;
using BusinessLogic.DTOs.QuizSetCommentDtos;
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
    public class QuizSetCommentController : ControllerBase
    {
        private readonly IQuizSetCommentService _quizSetCommentService;

        public QuizSetCommentController(IQuizSetCommentService quizSetCommentService)
        {
            _quizSetCommentService = quizSetCommentService;
        }

        [HttpPost]
        public async Task<ActionResult<ResponseQuizSetCommentDto>> Create([FromBody] RequestQuizSetCommentDto dto)
        {
            if (!ModelState.IsValid)
                throw new HttpException(HttpStatusCode.BadRequest, "Invalid model state");

            try
            {
                var result = await _quizSetCommentService.CreateAsync(dto);
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
        public async Task<ActionResult<ResponseQuizSetCommentDto>> GetById(Guid id)
        {
            var result = await _quizSetCommentService.GetByIdAsync(id);
            if (result == null)
                throw new HttpException(HttpStatusCode.NotFound, "Comment not found");

            return Ok(result);
        }

        [HttpPost("search")]
        [SubscriptionAndRoleAuthorize("Administrator", "Moderator")]
        public async Task<ActionResult<PaginationResponseDto<ResponseQuizSetCommentDto>>> GetAll(
            [FromBody] PaginationRequestDto pagination, 
            [FromQuery] bool includeDeleted = false)
        {
            var result = await _quizSetCommentService.GetAllAsync(pagination, includeDeleted);
            return Ok(result);
        }

        [HttpPost("user/{userId}/search")]
        public async Task<ActionResult<PaginationResponseDto<ResponseQuizSetCommentDto>>> GetByUserId(
            Guid userId,
            [FromBody] PaginationRequestDto pagination, 
            [FromQuery] bool includeDeleted = false)
        {
            var result = await _quizSetCommentService.GetByUserIdAsync(userId, pagination, includeDeleted);
            return Ok(result);
        }

        [HttpPost("quizset/{quizSetId}/search")]
        public async Task<ActionResult<PaginationResponseDto<ResponseQuizSetCommentDto>>> GetByQuizSetId(
            Guid quizSetId,
            [FromBody] PaginationRequestDto pagination, 
            [FromQuery] bool includeDeleted = false)
        {
            var result = await _quizSetCommentService.GetByQuizSetIdAsync(quizSetId, pagination, includeDeleted);
            return Ok(result);
        }

        [HttpGet("count/quizset/{quizSetId}")]
        public async Task<ActionResult<int>> GetCommentCount(Guid quizSetId)
        {
            var count = await _quizSetCommentService.GetCommentCountByQuizSetAsync(quizSetId);
            return Ok(count);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ResponseQuizSetCommentDto>> Update(Guid id, [FromBody] RequestQuizSetCommentDto dto)
        {
            if (!ModelState.IsValid)
                throw new HttpException(HttpStatusCode.BadRequest, "Invalid model state");

            var result = await _quizSetCommentService.UpdateAsync(id, dto);
            if (result == null)
                throw new HttpException(HttpStatusCode.NotFound, "Comment not found");

            return Ok(result);
        }

        [HttpDelete("{id}/hard")]
        [SubscriptionAndRoleAuthorize("Administrator", "Moderator")]
        public async Task<ActionResult<bool>> HardDelete(Guid id)
        {
            var result = await _quizSetCommentService.HardDeleteAsync(id);
            if (!result)
                throw new HttpException(HttpStatusCode.NotFound, "Comment not found");

            return Ok(result);
        }
    }
}