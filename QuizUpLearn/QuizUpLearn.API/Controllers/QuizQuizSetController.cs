using BusinessLogic.DTOs;
using BusinessLogic.DTOs.QuizQuizSetDtos;
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
    public class QuizQuizSetController : ControllerBase
    {
        private readonly IQuizQuizSetService _quizQuizSetService;

        public QuizQuizSetController(IQuizQuizSetService quizQuizSetService)
        {
            _quizQuizSetService = quizQuizSetService;
        }

        [HttpPost]
        [SubscriptionAndRoleAuthorize("Administrator", "Moderator")]
        public async Task<ActionResult<ResponseQuizQuizSetDto>> Create([FromBody] RequestQuizQuizSetDto dto)
        {
            if (!ModelState.IsValid)
                throw new HttpException(HttpStatusCode.BadRequest, "Invalid model state");

            try
            {
                var result = await _quizQuizSetService.CreateAsync(dto);
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
        public async Task<ActionResult<ResponseQuizQuizSetDto>> GetById(Guid id)
        {
            var result = await _quizQuizSetService.GetByIdAsync(id);
            if (result == null)
                throw new HttpException(HttpStatusCode.NotFound, "Quiz-QuizSet association not found");

            return Ok(result);
        }

        [HttpGet()]
        [SubscriptionAndRoleAuthorize("Administrator", "Moderator")]
        public async Task<ActionResult<PaginationResponseDto<ResponseQuizQuizSetDto>>> GetAll(
            [FromQuery] PaginationRequestDto pagination, [FromQuery] bool includeDeleted = false)
        {
            var result = await _quizQuizSetService.GetAllAsync(pagination, includeDeleted);
            return Ok(result);
        }

        [HttpPost("quiz/{quizId}/search")]
        public async Task<ActionResult<PaginationResponseDto<ResponseQuizQuizSetDto>>> GetByQuizId(
            Guid quizId,
            [FromQuery] PaginationRequestDto pagination, [FromQuery] bool includeDeleted = false)
        {
            var result = await _quizQuizSetService.GetByQuizIdAsync(quizId, pagination, includeDeleted);
            return Ok(result);
        }

        [HttpPost("quizset/{quizSetId}/search")]
        public async Task<ActionResult<PaginationResponseDto<ResponseQuizQuizSetDto>>> GetByQuizSetId(
            Guid quizSetId,
            [FromQuery] PaginationRequestDto pagination, [FromQuery] bool includeDeleted = false)
        {
            var result = await _quizQuizSetService.GetByQuizSetIdAsync(quizSetId, pagination, includeDeleted);
            return Ok(result);
        }

        [HttpGet("quiz/{quizId}/quizset/{quizSetId}")]
        public async Task<ActionResult<ResponseQuizQuizSetDto>> GetByQuizAndQuizSet(
            Guid quizId,
            Guid quizSetId,
            [FromQuery] bool includeDeleted = false)
        {
            var result = await _quizQuizSetService.GetByQuizAndQuizSetAsync(quizId, quizSetId, includeDeleted);
            if (result == null)
                throw new HttpException(HttpStatusCode.NotFound, "Quiz-QuizSet association not found");

            return Ok(result);
        }

        [HttpPut("{id}")]
        [SubscriptionAndRoleAuthorize("Administrator", "Moderator")]
        public async Task<ActionResult<ResponseQuizQuizSetDto>> Update(
            Guid id,
            [FromBody] RequestQuizQuizSetDto dto)
        {
            if (!ModelState.IsValid)
                throw new HttpException(HttpStatusCode.BadRequest, "Invalid model state");

            try
            {
                var result = await _quizQuizSetService.UpdateAsync(id, dto);
                if (result == null)
                    throw new HttpException(HttpStatusCode.NotFound, "Quiz-QuizSet association not found");

                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                throw new HttpException(HttpStatusCode.BadRequest, ex.Message);
            }
        }

        [HttpDelete("{id}/permanent")]
        [SubscriptionAndRoleAuthorize("Administrator")]
        public async Task<IActionResult> HardDelete(Guid id)
        {
            var result = await _quizQuizSetService.HardDeleteAsync(id);
            if (!result)
                throw new HttpException(HttpStatusCode.NotFound, "Quiz-QuizSet association not found");

            return NoContent();
        }

        [HttpGet("exists/quiz/{quizId}/quizset/{quizSetId}")]
        public async Task<ActionResult<bool>> Exists(Guid quizId, Guid quizSetId)
        {
            var result = await _quizQuizSetService.IsExistedAsync(quizId, quizSetId);
            return Ok(new { exists = result });
        }

        [HttpGet("quizset/{quizSetId}/count")]
        public async Task<ActionResult<int>> GetQuizCount(Guid quizSetId)
        {
            var result = await _quizQuizSetService.GetQuizCountByQuizSetAsync(quizSetId);
            return Ok(new { quizSetId, count = result });
        }

        [HttpPost("quiz/{quizId}/quizset/{quizSetId}")]
        [SubscriptionAndRoleAuthorize("Administrator", "Moderator")]
        public async Task<IActionResult> AddQuizToQuizSet(Guid quizId, Guid quizSetId)
        {
            var result = await _quizQuizSetService.AddQuizToQuizSetAsync(quizId, quizSetId);
            if (!result)
                throw new HttpException(HttpStatusCode.BadRequest, "Failed to add quiz to quiz set");

            return Ok(new { message = "Quiz successfully added to quiz set" });
        }

        [HttpPost("quizset/{quizSetId}/add-quizzes")]
        [SubscriptionAndRoleAuthorize("Administrator", "Moderator")]
        public async Task<IActionResult> AddQuizzesToQuizSet(
            Guid quizSetId,
            [FromBody] BulkAddQuizzesRequest request)
        {
            if (request?.QuizIds == null || !request.QuizIds.Any())
                throw new HttpException(HttpStatusCode.BadRequest, "Quiz IDs list cannot be empty");

            var result = await _quizQuizSetService.AddQuizzesToQuizSetAsync(request.QuizIds, quizSetId);
            if (!result)
                throw new HttpException(HttpStatusCode.BadRequest, "Failed to add quizzes to quiz set");

            return Ok(new { 
                message = "Quizzes successfully added to quiz set",
                quizSetId,
                addedQuizIds = request.QuizIds
            });
        }

        [HttpDelete("quiz/{quizId}/all-associations")]
        [SubscriptionAndRoleAuthorize("Administrator", "Moderator")]
        public async Task<IActionResult> DeleteByQuizId(Guid quizId)
        {
            var result = await _quizQuizSetService.DeleteByQuizIdAsync(quizId);
            if (!result)
                throw new HttpException(HttpStatusCode.NotFound, "No associations found for the specified quiz");

            return Ok(new { message = "All quiz associations successfully removed" });
        }

        [HttpDelete("quizset/{quizSetId}/all-associations")]
        [SubscriptionAndRoleAuthorize("Administrator", "Moderator")]
        public async Task<IActionResult> DeleteByQuizSetId(Guid quizSetId)
        {
            var result = await _quizQuizSetService.DeleteByQuizSetIdAsync(quizSetId);
            if (!result)
                throw new HttpException(HttpStatusCode.NotFound, "No associations found for the specified quiz set");

            return Ok(new { message = "All quiz set associations successfully removed" });
        }
    }

    public class BulkAddQuizzesRequest
    {
        public List<Guid> QuizIds { get; set; } = new List<Guid>();
    }
}