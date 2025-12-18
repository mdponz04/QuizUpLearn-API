using BusinessLogic.DTOs.QuizDtos;
using BusinessLogic.Interfaces;
using Microsoft.AspNetCore.Mvc;
using BusinessLogic.DTOs;
using QuizUpLearn.API.Models;
using System.Net;
using Microsoft.AspNetCore.Authorization;
using QuizUpLearn.API.Attributes;

namespace QuizUpLearn.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class QuizController : ControllerBase
    {
        private readonly IQuizService _quizService;

        public QuizController(IQuizService quizService)
        {
            _quizService = quizService;
        }

        [HttpPost]
        [SubscriptionAndRoleAuthorize("Moderator")]
        public async Task<ActionResult<QuizResponseDto>> CreateQuiz([FromBody] QuizRequestDto quizDto)
        {
            if (!ModelState.IsValid)
                throw new HttpException(HttpStatusCode.BadRequest, "Invalid model state");

            var createdQuiz = await _quizService.CreateQuizAsync(quizDto);
            return CreatedAtAction(nameof(GetQuizById), new { id = createdQuiz.Id }, createdQuiz);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<QuizResponseDto>> GetQuizById(Guid id)
        {
            var quiz = await _quizService.GetQuizByIdAsync(id);
            if (quiz == null)
                throw new HttpException(HttpStatusCode.NotFound, "Quiz not found");

            return Ok(quiz);
        }

        [HttpPost("search")]
        [SubscriptionAndRoleAuthorize("Moderator")]
        public async Task<ActionResult<PaginationResponseDto<QuizResponseDto>>> GetAllQuizzes(
            PaginationRequestDto pagination)
        {
            var quizzes = await _quizService.GetAllQuizzesAsync(pagination);
            return Ok(quizzes);
        }

        [HttpPost("serach/quiz-set/{quizSetId:guid}")]
        public async Task<ActionResult<PaginationResponseDto<QuizResponseDto>>> GetQuizzesByQuizSet(
            Guid quizSetId,
            PaginationRequestDto pagination)
        {
            var quizzes = await _quizService.GetQuizzesByQuizSetIdAsync(quizSetId, pagination);
            return Ok(quizzes);
        }

        [HttpPut("{id}")]
        [SubscriptionAndRoleAuthorize("Moderator")]
        public async Task<ActionResult<QuizResponseDto>> UpdateQuiz(Guid id, [FromBody] QuizRequestDto quizDto)
        {
            if (!ModelState.IsValid)
                throw new HttpException(HttpStatusCode.BadRequest, "Invalid model state");

            var updatedQuiz = await _quizService.UpdateQuizAsync(id, quizDto);
            if (updatedQuiz == null)
                throw new HttpException(HttpStatusCode.NotFound, "Quiz not found");

            return Ok(updatedQuiz);
        }

        [HttpDelete("{id}")]
        [SubscriptionAndRoleAuthorize("Moderator")]
        public async Task<IActionResult> SoftDeleteQuiz(Guid id)
        {
            var result = await _quizService.SoftDeleteQuizAsync(id);
            if (!result)
                throw new HttpException(HttpStatusCode.NotFound, "Quiz not found");

            return NoContent();
        }

        [HttpDelete("{id}/permanent")]
        [SubscriptionAndRoleAuthorize("Moderator")]
        public async Task<IActionResult> HardDeleteQuiz(Guid id)
        {
            var result = await _quizService.HardDeleteQuizAsync(id);
            if (!result)
                throw new HttpException(HttpStatusCode.NotFound, "Quiz not found");

            return NoContent();
        }

        [HttpPost("{id}/restore")]
        [SubscriptionAndRoleAuthorize("Moderator")]
        public async Task<IActionResult> RestoreQuiz(Guid id)
        {
            var result = await _quizService.RestoreQuizAsync(id);
            if (!result)
                throw new HttpException(HttpStatusCode.NotFound, "Quiz not found");

            return Ok();
        }

        [HttpGet("by-grammar-vocab")]
        public async Task<IActionResult> GetByGrammarIdAndVocabularyIdAsync(
            [FromQuery] Guid grammarId,
            [FromQuery] Guid vocabularyId,
            [FromQuery] PaginationRequestDto pagination)
        {
            var quizzes = await _quizService.GetByGrammarIdAndVocabularyIdAsync(grammarId, vocabularyId, pagination);
            return Ok(quizzes);
        }
    }
}
