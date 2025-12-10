using BusinessLogic.DTOs;
using BusinessLogic.DTOs.GrammarDtos;
using BusinessLogic.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizUpLearn.API.Attributes;

namespace QuizUpLearn.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class GrammarController : ControllerBase
    {
        private readonly IGrammarService _grammarService;

        public GrammarController(IGrammarService grammarService)
        {
            _grammarService = grammarService;
        }

        [HttpGet]
        [SubscriptionAndRoleAuthorize("Moderator")]
        public async Task<IActionResult> GetAll([FromQuery] PaginationRequestDto pagination)
        {
            var grammars = await _grammarService.GetAllAsync(pagination);
            return Ok(grammars);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var grammar = await _grammarService.GetByIdAsync(id);
            if (grammar == null)
            {
                return NotFound();
            }
            return Ok(grammar);
        }

        [HttpPost]
        [SubscriptionAndRoleAuthorize("Moderator")]
        public async Task<IActionResult> Create([FromBody] RequestGrammarDto request)
        {
            var created = await _grammarService.CreateAsync(request);
            if (created == null)
            {
                return StatusCode(500, "Creation failed");
            }
            return Ok(created);
        }

        [HttpPut("{id:guid}")]
        [SubscriptionAndRoleAuthorize("Moderator")]
        public async Task<IActionResult> Update(Guid id, [FromBody] RequestGrammarDto request)
        {
            var updated = await _grammarService.UpdateAsync(id, request);
            if (updated == null)
            {
                return NotFound();
            }
            return Ok(updated);
        }

        [HttpDelete("{id:guid}")]
        [SubscriptionAndRoleAuthorize("Moderator")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var existing = await _grammarService.GetByIdAsync(id);
            if (existing == null)
            {
                return NotFound();
            }

            var deleted = await _grammarService.DeleteAsync(id);
            if (!deleted)
            {
                return BadRequest("Không thể xoá Grammar khi vẫn còn Quiz tham chiếu.");
            }

            return Ok();
        }
    }
}

