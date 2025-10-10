using BusinessLogic.DTOs;
using BusinessLogic.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace QuizUpLearn.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class QuizAttemptController : ControllerBase
    {
        private readonly IQuizAttemptService _service;

        public QuizAttemptController(IQuizAttemptService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] bool isDeleted = false)
        {
            var quizAttempts = await _service.GetAllAsync(isDeleted);
            return Ok(quizAttempts);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById([FromRoute] Guid id)
        {
            var quizAttempt = await _service.GetByIdAsync(id);
            if (quizAttempt == null) return NotFound();
            return Ok(quizAttempt);
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetByUserId([FromRoute] Guid userId, [FromQuery] bool isDeleted = false)
        {
            var quizAttempts = await _service.GetByUserIdAsync(userId, isDeleted);
            return Ok(quizAttempts);
        }

        [HttpGet("quizset/{quizSetId}")]
        public async Task<IActionResult> GetByQuizSetId([FromRoute] Guid quizSetId, [FromQuery] bool isDeleted = false)
        {
            var quizAttempts = await _service.GetByQuizSetIdAsync(quizSetId, isDeleted);
            return Ok(quizAttempts);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] RequestQuizAttemptDto dto)
        {
            var created = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPost("single/start")]
        public async Task<IActionResult> StartSingle([FromBody] RequestSingleStartDto dto)
        {
            var started = await _service.StartSingleAsync(dto);
            return Ok(started);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] RequestQuizAttemptDto dto)
        {
            var updated = await _service.UpdateAsync(id, dto);
            if (updated == null) return NotFound();
            return Ok(updated);
        }

        [HttpPost("{id}/finish")]
        public async Task<IActionResult> Finish([FromRoute] Guid id)
        {
            var result = await _service.FinishAsync(id);
            if (result == null) return NotFound();
            return Ok(result);
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
