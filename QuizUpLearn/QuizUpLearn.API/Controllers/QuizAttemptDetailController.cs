using BusinessLogic.DTOs;
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
    public class QuizAttemptDetailController : ControllerBase
    {
        private readonly IQuizAttemptDetailService _service;

        public QuizAttemptDetailController(IQuizAttemptDetailService service)
        {
            _service = service;
        }

        [HttpGet]
        [SubscriptionAndRoleAuthorize("Moderator")]
        public async Task<IActionResult> GetAll([FromQuery] bool isDeleted = false)
        {
            var quizAttemptDetails = await _service.GetAllAsync(isDeleted);
            return Ok(quizAttemptDetails);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById([FromRoute] Guid id)
        {
            var quizAttemptDetail = await _service.GetByIdAsync(id);
            if (quizAttemptDetail == null) throw new HttpException(HttpStatusCode.NotFound, "Quiz attempt detail not found");
            return Ok(quizAttemptDetail);
        }

        [HttpGet("attempt/{attemptId}")]
        public async Task<IActionResult> GetByAttemptId(
            [FromRoute] Guid attemptId, 
            [FromQuery] PaginationRequestDto pagination,
            [FromQuery] bool isDeleted = false)
        {
            pagination ??= new PaginationRequestDto();
            var result = await _service.GetByAttemptIdPagedAsync(attemptId, pagination, isDeleted);
            return Ok(result);
        }

        [HttpGet("attempt/{attemptId}/placement-test")]
        public async Task<IActionResult> GetPlacementTestByAttemptId([FromRoute] Guid attemptId)
        {
            var result = await _service.GetPlacementTestByAttemptIdAsync(attemptId);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] RequestQuizAttemptDetailDto dto)
        {
            var created = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] RequestQuizAttemptDetailDto dto)
        {
            var updated = await _service.UpdateAsync(id, dto);
            if (updated == null) throw new HttpException(HttpStatusCode.NotFound, "Quiz attempt detail not found");
            return Ok(updated);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> SoftDelete([FromRoute] Guid id)
        {
            var ok = await _service.SoftDeleteAsync(id);
            if (!ok) throw new HttpException(HttpStatusCode.NotFound, "Quiz attempt detail not found");
            return NoContent();
        }

        [HttpPost("{id}/restore")]
        public async Task<IActionResult> Restore([FromRoute] Guid id)
        {
            var ok = await _service.RestoreAsync(id);
            if (!ok) throw new HttpException(HttpStatusCode.NotFound, "Quiz attempt detail not found");
            return Ok();
        }

        [HttpPost("submit")]
        public async Task<IActionResult> SubmitAnswers([FromBody] RequestSubmitAnswersDto dto)
        {
            var result = await _service.SubmitAnswersAsync(dto);
            return Ok(result);
        }

        [HttpPost("submit-placement-test")]
        public async Task<IActionResult> SubmitPlacementTest([FromBody] RequestSubmitAnswersDto dto)
        {
            var result = await _service.SubmitPlacementTestAsync(dto);
            return Ok(result);
        }
    }
}
