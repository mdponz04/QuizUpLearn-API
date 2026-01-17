using BusinessLogic.DTOs;
using BusinessLogic.DTOs.UserReportDtos;
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
    public class UserReportController : ControllerBase
    {
        private readonly IUserReportService _userReportService;

        public UserReportController(IUserReportService userReportService)
        {
            _userReportService = userReportService;
        }

        [HttpPost]
        [SubscriptionAndRoleAuthorize]
        public async Task<ActionResult<ResponseUserReportDto>> Create([FromBody] RequestUserReportDto dto)
        {
            try
            {
                var result = await _userReportService.CreateAsync(dto);
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
        [SubscriptionAndRoleAuthorize]
        public async Task<ActionResult<ResponseUserReportDto>> GetById(Guid id)
        {
            var result = await _userReportService.GetByIdAsync(id);
            if (result == null)
                throw new HttpException(HttpStatusCode.NotFound, "User report not found");

            return Ok(result);
        }

        [HttpPost("search")]
        [SubscriptionAndRoleAuthorize("Administrator", "Moderator")]
        public async Task<ActionResult<PaginationResponseDto<ResponseUserReportDto>>> GetAll(
            [FromBody] PaginationRequestDto pagination, 
            [FromQuery] bool includeDeleted = false)
        {
            var result = await _userReportService.GetAllAsync(pagination, includeDeleted);
            return Ok(result);
        }

        [HttpPost("user/{userId}/search")]
        [SubscriptionAndRoleAuthorize("Administrator", "Moderator")]
        public async Task<ActionResult<PaginationResponseDto<ResponseUserReportDto>>> GetByUserId(
            Guid userId,
            [FromBody] PaginationRequestDto pagination, 
            [FromQuery] bool includeDeleted = false)
        {
            var result = await _userReportService.GetByUserIdAsync(userId, pagination, includeDeleted);
            return Ok(result);
        }

        [HttpGet("exists/user/{userId}")]
        public async Task<ActionResult<bool>> IsReportExists(Guid userId)
        {
            var exists = await _userReportService.IsExistAsync(userId);
            return Ok(exists);
        }

        [HttpDelete("{id}/hard")]
        [SubscriptionAndRoleAuthorize("Moderator")]
        public async Task<ActionResult<bool>> HardDelete(Guid id)
        {
            var result = await _userReportService.HardDeleteAsync(id);
            if (!result)
                throw new HttpException(HttpStatusCode.NotFound, "User report not found");

            return Ok(result);
        }
    }
}


