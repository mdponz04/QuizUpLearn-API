using BusinessLogic.DTOs;
using BusinessLogic.DTOs.UserQuizSetLikeDtos;
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
    public class UserQuizSetLikeController : ControllerBase
    {
        private readonly IUserQuizSetLikeService _userQuizSetLikeService;

        public UserQuizSetLikeController(IUserQuizSetLikeService userQuizSetLikeService)
        {
            _userQuizSetLikeService = userQuizSetLikeService;
        }

        [HttpPost]
        public async Task<ActionResult<ResponseUserQuizSetLikeDto>> Create([FromBody] RequestUserQuizSetLikeDto dto)
        {
            try
            {
                var result = await _userQuizSetLikeService.CreateAsync(dto);
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
        public async Task<ActionResult<ResponseUserQuizSetLikeDto>> GetById(Guid id)
        {
            var result = await _userQuizSetLikeService.GetByIdAsync(id);
            if (result == null)
                throw new HttpException(HttpStatusCode.NotFound, "Like not found");

            return Ok(result);
        }

        [HttpPost("search")]
        [SubscriptionAndRoleAuthorize("Administrator", "Moderator")]
        public async Task<ActionResult<PaginationResponseDto<ResponseUserQuizSetLikeDto>>> GetAll(
            [FromBody] PaginationRequestDto pagination, 
            [FromQuery] bool includeDeleted = false)
        {
            var result = await _userQuizSetLikeService.GetAllAsync(pagination, includeDeleted);
            return Ok(result);
        }

        [HttpPost("user/search")]
        [SubscriptionAndRoleAuthorize]
        public async Task<ActionResult<PaginationResponseDto<ResponseUserQuizSetLikeDto>>> GetByUserId(
            Guid? userId,
            [FromBody] PaginationRequestDto pagination, 
            [FromQuery] bool includeDeleted = false)
        {
            if (userId == null)
            {
                userId = (Guid)HttpContext.Items["UserId"]!;
            }
            var result = await _userQuizSetLikeService.GetByUserIdAsync(userId.Value, pagination, includeDeleted);
            return Ok(result);
        }

        [HttpPost("quizset/{quizSetId}/search")]
        public async Task<ActionResult<PaginationResponseDto<ResponseUserQuizSetLikeDto>>> GetByQuizSetId(
            Guid quizSetId,
            [FromBody] PaginationRequestDto pagination, 
            [FromQuery] bool includeDeleted = false)
        {
            var result = await _userQuizSetLikeService.GetByQuizSetIdAsync(quizSetId, pagination, includeDeleted);
            return Ok(result);
        }

        [HttpGet("user/{userId}/quizset/{quizSetId}")]
        [SubscriptionAndRoleAuthorize]
        public async Task<ActionResult<ResponseUserQuizSetLikeDto>> GetByUserAndQuizSet(
            Guid? userId, 
            Guid quizSetId, 
            [FromQuery] bool includeDeleted = false)
        {
            if (userId == null)
            {
                userId = (Guid)HttpContext.Items["UserId"]!;
            }
            var result = await _userQuizSetLikeService.GetByUserAndQuizSetAsync(userId.Value, quizSetId, includeDeleted);
            if (result == null)
                throw new HttpException(HttpStatusCode.NotFound, "Like not found");

            return Ok(result);
        }

        [HttpPost("toggle-like/quiz-set-id/{quizSetId:guid}")]
        [SubscriptionAndRoleAuthorize]
        public async Task<ActionResult<bool>> ToggleLike(Guid? userId, Guid quizSetId)
        {
            if (userId == null)
            {
                userId = (Guid)HttpContext.Items["UserId"]!;
            }
            var result = await _userQuizSetLikeService.ToggleLikeAsync(userId.Value, quizSetId);
            return Ok(result);
        }

        [HttpGet("exists/user/{userId}/quizset/{quizSetId}")]
        public async Task<ActionResult<bool>> IsLikeExists(Guid userId, Guid quizSetId)
        {
            var exists = await _userQuizSetLikeService.IsExistAsync(userId, quizSetId);
            return Ok(exists);
        }

        [HttpGet("count/quizset/{quizSetId}")]
        public async Task<ActionResult<int>> GetLikeCount(Guid quizSetId)
        {
            var count = await _userQuizSetLikeService.GetLikeCountByQuizSetAsync(quizSetId);
            return Ok(count);
        }

        [HttpDelete("{id}/hard")]
        [SubscriptionAndRoleAuthorize("Administrator")]
        public async Task<ActionResult<bool>> HardDelete(Guid id)
        {
            var result = await _userQuizSetLikeService.HardDeleteAsync(id);
            if (!result)
                throw new HttpException(HttpStatusCode.NotFound, "Like not found");

            return Ok(result);
        }
    }
}