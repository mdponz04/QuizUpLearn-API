using BusinessLogic.DTOs.QuizSetDtos;
using BusinessLogic.Interfaces;
using Microsoft.AspNetCore.Mvc;
using BusinessLogic.DTOs;

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
        public async Task<ActionResult<QuizSetResponseDto>> CreateQuizSet([FromBody] QuizSetRequestDto quizSetDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var createdQuizSet = await _quizSetService.CreateQuizSetAsync(quizSetDto);
            return CreatedAtAction(nameof(GetQuizSetById), new { id = createdQuizSet.Id }, createdQuizSet);
        }

        /// <summary>
        /// Gets a quiz set by ID
        /// </summary>
        /// <param name="id">Quiz set ID</param>
        /// <returns>Quiz set data</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<QuizSetResponseDto>> GetQuizSetById(Guid id)
        {
            var quizSet = await _quizSetService.GetQuizSetByIdAsync(id);
            if (quizSet == null)
                return NotFound();

            return Ok(quizSet);
        }

        /// <summary>
        /// Gets all quiz sets
        /// </summary>
        /// <returns>List of quiz sets</returns>
        [HttpGet]
        public async Task<ActionResult<PaginationResponseDto<QuizSetResponseDto>>> GetAllQuizSets(
            [FromQuery] PaginationRequestDto pagination,
            [FromQuery] bool includeDeleted = false)
        {
            var quizSets = await _quizSetService.GetAllQuizSetsAsync(includeDeleted, pagination);

            return Ok(quizSets);
        }

        /// <summary>
        /// Gets quiz sets created by a specific user
        /// </summary>
        /// <param name="creatorId">Creator ID</param>
        /// <returns>List of quiz sets by creator</returns>
        [HttpGet("creator/{creatorId}")]
        public async Task<ActionResult<PaginationResponseDto<QuizSetResponseDto>>> GetQuizSetsByCreator(
            Guid creatorId,
            [FromQuery] PaginationRequestDto pagination)
        {
            var quizSets = await _quizSetService.GetQuizSetsByCreatorAsync(creatorId, pagination);
            return Ok(quizSets);
        }

        /// <summary>
        /// Gets all published quiz sets
        /// </summary>
        /// <returns>List of published quiz sets</returns>
        [HttpGet("published")]
        public async Task<ActionResult<PaginationResponseDto<QuizSetResponseDto>>> GetPublishedQuizSets(
            [FromQuery] PaginationRequestDto pagination)
        {
            var quizSets = await _quizSetService.GetPublishedQuizSetsAsync(pagination);
            return Ok(quizSets);
        }

        /// <summary>
        /// Updates an existing quiz set
        /// </summary>
        /// <param name="id">Quiz set ID</param>
        /// <param name="quizSetDto">Updated quiz set data</param>
        /// <returns>Updated quiz set</returns>
        [HttpPut("{id}")]
        public async Task<ActionResult<QuizSetResponseDto>> UpdateQuizSet(Guid id, [FromBody] QuizSetRequestDto quizSetDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var updatedQuizSet = await _quizSetService.UpdateQuizSetAsync(id, quizSetDto);
            if (updatedQuizSet == null)
                return NotFound();

            return Ok(updatedQuizSet);
        }

        /// <summary>
        /// Soft deletes a quiz set (sets DeletedAt timestamp)
        /// </summary>
        /// <param name="id">Quiz set ID</param>
        /// <returns>Success/failure status</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> SoftDeleteQuizSet(Guid id)
        {
            var result = await _quizSetService.SoftDeleteQuizSetAsync(id);
            if (!result)
                return NotFound();

            return NoContent();
        }

        /// <summary>
        /// Hard deletes a quiz set (removes from database)
        /// </summary>
        /// <param name="id">Quiz set ID</param>
        /// <returns>Success/failure status</returns>
        [HttpDelete("{id}/permanent")]
        public async Task<IActionResult> HardDeleteQuizSet(Guid id)
        {
            var result = await _quizSetService.HardDeleteQuizSetAsync(id);
            if (!result)
                return NotFound();

            return NoContent();
        }

        [HttpPost("{id}/restore")]
        public async Task<ActionResult<QuizSetResponseDto>> RestoreQuizSet(Guid id)
        {
            var restoredQuizSet = await _quizSetService.RestoreQuizSetAsync(id);
            if (restoredQuizSet == null)
                return NotFound();
            return Ok(restoredQuizSet);
        }
    }
}
