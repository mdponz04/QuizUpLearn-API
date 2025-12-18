using BusinessLogic.DTOs;
using BusinessLogic.DTOs.VocabularyDtos;
using BusinessLogic.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizUpLearn.API.Attributes;

namespace QuizUpLearn.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class VocabularyController : ControllerBase
    {
        private readonly IVocabularyService _vocabularyService;

        public VocabularyController(IVocabularyService vocabularyService)
        {
            _vocabularyService = vocabularyService;
        }

        [HttpGet]
        [SubscriptionAndRoleAuthorize("Moderator")]
        public async Task<IActionResult> GetAll(
            [FromQuery] PaginationRequestDto pagination,
            [FromQuery] Repository.Enums.VocabularyDifficultyEnum? difficulty)
        {
            var vocabularies = await _vocabularyService.GetAllAsync(pagination, difficulty);
            return Ok(vocabularies);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var vocabulary = await _vocabularyService.GetByIdAsync(id);
            if (vocabulary == null)
            {
                return NotFound();
            }
            return Ok(vocabulary);
        }

        [HttpPost]
        [SubscriptionAndRoleAuthorize("Moderator")]
        public async Task<IActionResult> Create([FromBody] RequestVocabularyDto request)
        {
            var created = await _vocabularyService.CreateAsync(request);
            if (created == null)
            {
                return StatusCode(500, "Creation failed");
            }
            return Ok(created);
        }

        [HttpPut("{id:guid}")]
        [SubscriptionAndRoleAuthorize("Moderator")]
        public async Task<IActionResult> Update(Guid id, [FromBody] RequestVocabularyDto request)
        {
            var updated = await _vocabularyService.UpdateAsync(id, request);
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
            var existing = await _vocabularyService.GetByIdAsync(id);
            if (existing == null)
            {
                return NotFound();
            }

            var deleted = await _vocabularyService.DeleteAsync(id);
            if (!deleted)
            {
                return BadRequest("Không thể xoá Vocabulary khi vẫn còn Quiz tham chiếu.");
            }

            return Ok();
        }
    }
}

