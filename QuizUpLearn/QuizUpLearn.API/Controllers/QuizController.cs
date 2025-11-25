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

        /// <summary>
        /// Creates a new quiz
        /// </summary>
        /// <param name="quizDto">Quiz data</param>
        /// <returns>Newly created quiz</returns>
        [HttpPost]
        [SubscriptionAndRoleAuthorize("Moderator")]
        public async Task<ActionResult<QuizResponseDto>> CreateQuiz([FromBody] QuizRequestDto quizDto)
        {
            if (!ModelState.IsValid)
                throw new HttpException(HttpStatusCode.BadRequest, "Invalid model state");

            var createdQuiz = await _quizService.CreateQuizAsync(quizDto);
            return CreatedAtAction(nameof(GetQuizById), new { id = createdQuiz.Id }, createdQuiz);
        }

        /// <summary>
        /// Gets a quiz by ID
        /// </summary>
        /// <param name="id">Quiz ID</param>
        /// <returns>Quiz data</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<QuizResponseDto>> GetQuizById(Guid id)
        {
            var quiz = await _quizService.GetQuizByIdAsync(id);
            if (quiz == null)
                throw new HttpException(HttpStatusCode.NotFound, "Quiz not found");

            return Ok(quiz);
        }

        /// <summary>
        /// Gets all quizzes
        /// </summary>
        /// <returns>List of quizzes</returns>
        [HttpGet]
        public async Task<ActionResult<PaginationResponseDto<QuizResponseDto>>> GetAllQuizzes(
            [FromQuery] PaginationRequestDto pagination)
        {
            var quizzes = await _quizService.GetAllQuizzesAsync(pagination);
            return Ok(quizzes);
        }

        /// <summary>
        /// Gets all quizzes by quiz set ID
        /// </summary>
        /// <param name="quizSetId">Quiz set ID</param>
        /// <returns>List of quizzes in the specified quiz set</returns>
        [HttpGet("set/{quizSetId}")]
        public async Task<ActionResult<PaginationResponseDto<QuizResponseDto>>> GetQuizzesByQuizSet(
            Guid quizSetId,
            [FromQuery] PaginationRequestDto pagination)
        {
            var quizzes = await _quizService.GetQuizzesByQuizSetIdAsync(quizSetId, pagination);
            return Ok(quizzes);
        }

        /// <summary>
        /// Gets all active quizzes
        /// </summary>
        /// <returns>List of active quizzes</returns>
        [HttpGet("active")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<PaginationResponseDto<QuizResponseDto>>> GetActiveQuizzes(
            [FromQuery] PaginationRequestDto pagination)
        {
            var quizzes = await _quizService.GetActiveQuizzesAsync(pagination);
            return Ok(quizzes);
        }

        /// <summary>
        /// Updates an existing quiz
        /// </summary>
        /// <param name="id">Quiz ID</param>
        /// <param name="quizDto">Updated quiz data</param>
        /// <returns>Updated quiz</returns>
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

        /// <summary>
        /// Soft deletes a quiz (sets DeletedAt timestamp)
        /// </summary>
        /// <param name="id">Quiz ID</param>
        /// <returns>Success/failure status</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> SoftDeleteQuiz(Guid id)
        {
            var result = await _quizService.SoftDeleteQuizAsync(id);
            if (!result)
                throw new HttpException(HttpStatusCode.NotFound, "Quiz not found");

            return NoContent();
        }

        /// <summary>
        /// Hard deletes a quiz (removes from database)
        /// </summary>
        /// <param name="id">Quiz ID</param>
        /// <returns>Success/failure status</returns>
        [HttpDelete("{id}/permanent")]
        public async Task<IActionResult> HardDeleteQuiz(Guid id)
        {
            var result = await _quizService.HardDeleteQuizAsync(id);
            if (!result)
                throw new HttpException(HttpStatusCode.NotFound, "Quiz not found");

            return NoContent();
        }
    }
}
