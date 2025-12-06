using BusinessLogic.DTOs;
using BusinessLogic.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizUpLearn.API.Attributes;

namespace QuizUpLearn.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AnswerOptionController : ControllerBase
    {
        private readonly IAnswerOptionService _service;

        public AnswerOptionController(IAnswerOptionService service)
        {
            _service = service;
        }

        [HttpGet]
        [SubscriptionAndRoleAuthorize("Moderator")]
        public async Task<IActionResult> GetAll([FromQuery] bool isDeleted = false)
        {
            var answerOptions = await _service.GetAllAsync(isDeleted);
            return Ok(answerOptions);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById([FromRoute] Guid id)
        {
            var answerOption = await _service.GetByIdAsync(id);
            if (answerOption == null) return NotFound();
            return Ok(answerOption);
        }

        [HttpGet("quiz/{quizId}")]
        public async Task<IActionResult> GetByQuizId([FromRoute] Guid quizId, [FromQuery] bool isDeleted = false)
        {
            var answerOptions = await _service.GetByQuizIdAsync(quizId, isDeleted);
            return Ok(answerOptions);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] RequestAnswerOptionDto dto)
        {
            var created = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] RequestAnswerOptionDto dto)
        {
            var updated = await _service.UpdateAsync(id, dto);
            if (updated == null) return NotFound();
            return Ok(updated);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> SoftDelete([FromRoute] Guid id)
        {
            var ok = await _service.SoftDeleteAsync(id);
            if (!ok) return NotFound();
            return NoContent();
        }

        [HttpPost("{id}/restore")]
        public async Task<IActionResult> Restore([FromRoute] Guid id)
        {
            var ok = await _service.RestoreAsync(id);
            if (!ok) return NotFound();
            return Ok();
        }
    }
}
