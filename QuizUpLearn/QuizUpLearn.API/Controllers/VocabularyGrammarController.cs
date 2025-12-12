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

        [HttpGet("unused-pairs")]
        [SubscriptionAndRoleAuthorize("Moderator")]
        public async Task<IActionResult> GetUnusedPairs()
        {
            var unusedPairs = await _vocabularyGrammarService.GetUnusedPairVocabularyGrammar();
            return Ok(unusedPairs);
        }
    }
}