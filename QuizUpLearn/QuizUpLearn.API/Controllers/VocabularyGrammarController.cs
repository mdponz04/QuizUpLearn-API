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
    public class VocabularyGrammarController : ControllerBase
    {
        private readonly IVocabularyGrammarService _vocabularyGrammarService;

        public VocabularyGrammarController(IVocabularyGrammarService vocabularyGrammarService)
        {
            _vocabularyGrammarService = vocabularyGrammarService;
        }

        [HttpPost("search/unused-pairs")]
        [SubscriptionAndRoleAuthorize("Moderator")]
        public async Task<IActionResult> GetUnusedPairs([FromBody] PaginationRequestDto pagination = null!)
        {
            var unusedPairs = await _vocabularyGrammarService.GetUnusedPairVocabularyGrammar(pagination);
            return Ok(unusedPairs);
        }
    }
}