using BusinessLogic.DTOs.QuizSetDtos;
using BusinessLogic.Interfaces;
using Microsoft.AspNetCore.Mvc;
using BusinessLogic.DTOs;
using QuizUpLearn.API.Models;
using System.Net;
using Microsoft.AspNetCore.Authorization;

namespace QuizUpLearn.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class QuizSetController : ControllerBase
    {
        private readonly IQuizSetService _quizSetService;

        public QuizSetController(IQuizSetService quizSetService)
        {
            _quizSetService = quizSetService;
        }

        /// <summary>
        /// Creates a new quiz set
        /// </summary>
        /// <param name="quizSetDto">Quiz set data</param>
        /// <returns>Newly created quiz set</returns>
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<QuizSetResponseDto>> CreateQuizSet([FromBody] QuizSetRequestDto quizSetDto)
        {
            if (!ModelState.IsValid)
                throw new HttpException(HttpStatusCode.BadRequest, "Invalid model state");

            var createdQuizSet = await _quizSetService.CreateQuizSetAsync(quizSetDto);
            return CreatedAtAction(nameof(GetQuizSetById), new { id = createdQuizSet.Id }, createdQuizSet);
        }
        /// <summary>
        /// Gets a quiz set by ID
        /// </summary>
        /// <param name="id">Quiz set ID</param>
        /// <returns>Quiz set data</returns>
        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<QuizSetResponseDto>> GetQuizSetById(Guid id)
        {
            var quizSet = await _quizSetService.GetQuizSetByIdAsync(id);
            if (quizSet == null)
                throw new HttpException(HttpStatusCode.NotFound, "Quiz set not found");

            return Ok(quizSet);
        }

        /// <summary>
        /// Gets all quiz sets with filters
        /// </summary>
        /// <param name="request">Pagination and filter parameters</param>
        /// <returns>List of quiz sets</returns>
        [HttpPost("search")]
        [Authorize]
        public async Task<ActionResult<PaginationResponseDto<QuizSetResponseDto>>> GetAllQuizSets(
            [FromBody] PaginationRequestDto request)
        {
            var quizSets = await _quizSetService.GetAllQuizSetsAsync(request);
            return Ok(quizSets);
        }

        /// <summary>
        /// Gets quiz sets created by a specific user with filters
        /// </summary>
        /// <param name="creatorId">Creator ID</param>
        /// <param name="request">Pagination and filter parameters</param>
        /// <returns>List of quiz sets by creator</returns>
        [HttpPost("creator/{creatorId}/search")]
        [Authorize]
        public async Task<ActionResult<PaginationResponseDto<QuizSetResponseDto>>> GetQuizSetsByCreator(
            Guid creatorId,
            [FromBody] PaginationRequestDto request)
        {
            var quizSets = await _quizSetService.GetQuizSetsByCreatorAsync(creatorId, request);
            return Ok(quizSets);
        }

        /// <summary>
        /// Gets only published and non-deleted quiz sets
        /// </summary>
        /// <param name="request">Pagination and filter parameters</param>
        /// <returns>List of published quiz sets</returns>
        [HttpPost("published/search")]
        [AllowAnonymous]
        public async Task<ActionResult<PaginationResponseDto<QuizSetResponseDto>>> GetPublishedQuizSets(
            [FromBody] PaginationRequestDto request)
        {
            var quizSets = await _quizSetService.GetPublishedQuizSetsAsync(request);
            return Ok(quizSets);
        }

        /// <summary>
        /// Updates an existing quiz set
        /// </summary>
        /// <param name="id">Quiz set ID</param>
        /// <param name="quizSetDto">Updated quiz set data</param>
        /// <returns>Updated quiz set</returns>
        [HttpPut("{id}")]
        [Authorize]
        public async Task<ActionResult<QuizSetResponseDto>> UpdateQuizSet(Guid id, [FromBody] QuizSetRequestDto quizSetDto)
        {
            if (!ModelState.IsValid)
                throw new HttpException(HttpStatusCode.BadRequest, "Invalid model state");

            var updatedQuizSet = await _quizSetService.UpdateQuizSetAsync(id, quizSetDto);
            if (updatedQuizSet == null)
                throw new HttpException(HttpStatusCode.NotFound, "Quiz set not found");

            return Ok(updatedQuizSet);
        }

        /// <summary>
        /// Soft deletes a quiz set (sets DeletedAt timestamp)
        /// </summary>
        /// <param name="id">Quiz set ID</param>
        /// <returns>Success/failure status</returns>
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> SoftDeleteQuizSet(Guid id)
        {
            var result = await _quizSetService.SoftDeleteQuizSetAsync(id);
            if (!result)
                throw new HttpException(HttpStatusCode.NotFound, "Quiz set not found");

            return NoContent();
        }

        /// <summary>
        /// Hard deletes a quiz set (removes from database)
        /// </summary>
        /// <param name="id">Quiz set ID</param>
        /// <returns>Success/failure status</returns>
        [HttpDelete("{id}/permanent")]
        [Authorize]
        public async Task<IActionResult> HardDeleteQuizSet(Guid id)
        {
            var result = await _quizSetService.HardDeleteQuizSetAsync(id);
            if (!result)
                throw new HttpException(HttpStatusCode.NotFound, "Quiz set not found");

            return NoContent();
        }

        [HttpPost("{id}/restore")]
        [Authorize]
        public async Task<ActionResult<QuizSetResponseDto>> RestoreQuizSet(Guid id)
        {
            var restoredQuizSet = await _quizSetService.RestoreQuizSetAsync(id);
            if (restoredQuizSet == null)
                throw new HttpException(HttpStatusCode.NotFound, "Quiz set not found");
            return Ok(restoredQuizSet);
        }
    }
}
