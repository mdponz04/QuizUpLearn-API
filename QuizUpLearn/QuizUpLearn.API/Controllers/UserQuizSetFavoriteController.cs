using BusinessLogic.DTOs;
using BusinessLogic.DTOs.UserQuizSetFavoriteDtos;
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
    public class UserQuizSetFavoriteController : ControllerBase
    {
        private readonly IUserQuizSetFavoriteService _userQuizSetFavoriteService;

        public UserQuizSetFavoriteController(IUserQuizSetFavoriteService userQuizSetFavoriteService)
        {
            _userQuizSetFavoriteService = userQuizSetFavoriteService;
        }

        [HttpPost]
        public async Task<ActionResult<ResponseUserQuizSetFavoriteDto>> Create([FromBody] RequestUserQuizSetFavoriteDto dto)
        {
            try
            {
                var result = await _userQuizSetFavoriteService.CreateAsync(dto);
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
        public async Task<ActionResult<ResponseUserQuizSetFavoriteDto>> GetById(Guid id)
        {
            var result = await _userQuizSetFavoriteService.GetByIdAsync(id);
            if (result == null)
                throw new HttpException(HttpStatusCode.NotFound, "Favorite not found");

            return Ok(result);
        }

        [HttpPost("search")]
        [SubscriptionAndRoleAuthorize("Administrator", "Moderator")]
        public async Task<ActionResult<PaginationResponseDto<ResponseUserQuizSetFavoriteDto>>> GetAll(
            [FromBody] PaginationRequestDto pagination, 
            [FromQuery] bool includeDeleted = false)
        {
            var result = await _userQuizSetFavoriteService.GetAllAsync(pagination, includeDeleted);
            return Ok(result);
        }

        [HttpPost("user/search")]
        [SubscriptionAndRoleAuthorize]
        public async Task<ActionResult<PaginationResponseDto<ResponseUserQuizSetFavoriteDto>>> GetByUserId(
            Guid? userId,
            [FromBody] PaginationRequestDto pagination, 
            [FromQuery] bool includeDeleted = false)
        {
            if (userId == null)
            {
                userId = (Guid)HttpContext.Items["UserId"]!;
            }
            var result = await _userQuizSetFavoriteService.GetByUserIdAsync(userId.Value, pagination, includeDeleted);
            return Ok(result);
        }

        [HttpPost("quizset/{quizSetId}/search")]
        public async Task<ActionResult<PaginationResponseDto<ResponseUserQuizSetFavoriteDto>>> GetByQuizSetId(
            Guid quizSetId,
            [FromBody] PaginationRequestDto pagination, 
            [FromQuery] bool includeDeleted = false)
        {
            var result = await _userQuizSetFavoriteService.GetByQuizSetIdAsync(quizSetId, pagination, includeDeleted);
            return Ok(result);
        }

        [HttpGet("user/{userId}/quizset/{quizSetId}")]
        public async Task<ActionResult<ResponseUserQuizSetFavoriteDto>> GetByUserAndQuizSet(
            Guid userId, 
            Guid quizSetId, 
            [FromQuery] bool includeDeleted = false)
        {
            var result = await _userQuizSetFavoriteService.GetByUserAndQuizSetAsync(userId, quizSetId, includeDeleted);
            if (result == null)
                throw new HttpException(HttpStatusCode.NotFound, "Favorite not found");

            return Ok(result);
        }

        [HttpPost("toggle-favorite/quiz-set-id/{quizSetId:guid}")]
        [SubscriptionAndRoleAuthorize]
        public async Task<ActionResult<bool>> ToggleFavorite(Guid? userId, Guid quizSetId)
        {
            if (userId == null)
            {
                userId = (Guid)HttpContext.Items["UserId"]!;
            }
            var result = await _userQuizSetFavoriteService.ToggleFavoriteAsync(userId.Value, quizSetId);
            return Ok(result);
        }

        [HttpGet("exists/user/{userId}/quizset/{quizSetId}")]
        public async Task<ActionResult<bool>> IsFavoriteExists(Guid userId, Guid quizSetId)
        {
            var exists = await _userQuizSetFavoriteService.IsExistAsync(userId, quizSetId);
            return Ok(exists);
        }

        [HttpGet("count/quizset/{quizSetId}")]
        public async Task<ActionResult<int>> GetFavoriteCount(Guid quizSetId)
        {
            var count = await _userQuizSetFavoriteService.GetFavoriteCountByQuizSetAsync(quizSetId);
            return Ok(count);
        }

        [HttpDelete("{id}/hard")]
        [SubscriptionAndRoleAuthorize("Administrator")]
        public async Task<ActionResult<bool>> HardDelete(Guid id)
        {
            var result = await _userQuizSetFavoriteService.HardDeleteAsync(id);
            if (!result)
                throw new HttpException(HttpStatusCode.NotFound, "Favorite not found");

            return Ok(result);
        }
    }
}