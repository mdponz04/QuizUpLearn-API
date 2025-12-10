using BusinessLogic.DTOs;
using BusinessLogic.DTOs.QuizQuizSetDtos;
using BusinessLogic.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizUpLearn.API.Attributes;
using QuizUpLearn.API.Models;
using System.Net;

namespace QuizUpLearn.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class QuizQuizSetController : ControllerBase
    {
        private readonly IQuizQuizSetService _quizQuizSetService;

        public QuizQuizSetController(IQuizQuizSetService quizQuizSetService)
        {
            _quizQuizSetService = quizQuizSetService;
        }

        /// <summary>
        /// Creates a new quiz-quizset association
        /// </summary>
        /// <param name="dto">Quiz-QuizSet association data</param>
        /// <returns>Created association</returns>
        [HttpPost]
        [SubscriptionAndRoleAuthorize("Administrator", "Moderator")]
        public async Task<ActionResult<ResponseQuizQuizSetDto>> Create([FromBody] RequestQuizQuizSetDto dto)
        {
            if (!ModelState.IsValid)
                throw new HttpException(HttpStatusCode.BadRequest, "Invalid model state");

            try
            {
                var result = await _quizQuizSetService.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
            }
            catch (ArgumentException ex)
            {
                throw new HttpException(HttpStatusCode.BadRequest, ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                throw new HttpException(HttpStatusCode.Conflict, ex.Message);
            }
        }

        /// <summary>
        /// Gets a quiz-quizset association by ID
        /// </summary>
        /// <param name="id">Association ID</param>
        /// <returns>Quiz-QuizSet association</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<ResponseQuizQuizSetDto>> GetById(Guid id)
        {
            var result = await _quizQuizSetService.GetByIdAsync(id);
            if (result == null)
                throw new HttpException(HttpStatusCode.NotFound, "Quiz-QuizSet association not found");

            return Ok(result);
        }

        /// <summary>
        /// Gets all quiz-quizset associations with pagination
        /// </summary>
        /// <param name="pagination">Pagination parameters</param>
        /// <returns>Paginated list of associations</returns>
        [HttpPost("search")]
        [SubscriptionAndRoleAuthorize("Administrator", "Moderator")]
        public async Task<ActionResult<PaginationResponseDto<ResponseQuizQuizSetDto>>> GetAll(
            [FromBody] PaginationRequestDto pagination)
        {
            var result = await _quizQuizSetService.GetAllAsync(pagination);
            return Ok(result);
        }

        /// <summary>
        /// Gets all quiz sets that contain a specific quiz
        /// </summary>
        /// <param name="quizId">Quiz ID</param>
        /// <param name="pagination">Pagination parameters</param>
        /// <returns>Paginated list of quiz-quizset associations</returns>
        [HttpPost("quiz/{quizId}/search")]
        public async Task<ActionResult<PaginationResponseDto<ResponseQuizQuizSetDto>>> GetByQuizId(
            Guid quizId,
            [FromBody] PaginationRequestDto pagination)
        {
            var result = await _quizQuizSetService.GetByQuizIdAsync(quizId, pagination);
            return Ok(result);
        }

        /// <summary>
        /// Gets all quizzes in a specific quiz set
        /// </summary>
        /// <param name="quizSetId">QuizSet ID</param>
        /// <param name="pagination">Pagination parameters</param>
        /// <returns>Paginated list of quiz-quizset associations</returns>
        [HttpPost("quizset/{quizSetId}/search")]
        public async Task<ActionResult<PaginationResponseDto<ResponseQuizQuizSetDto>>> GetByQuizSetId(
            Guid quizSetId,
            [FromBody] PaginationRequestDto pagination)
        {
            var result = await _quizQuizSetService.GetByQuizSetIdAsync(quizSetId, pagination);
            return Ok(result);
        }

        /// <summary>
        /// Gets specific quiz-quizset association by quiz and quizset IDs
        /// </summary>
        /// <param name="quizId">Quiz ID</param>
        /// <param name="quizSetId">QuizSet ID</param>
        /// <returns>Quiz-QuizSet association</returns>
        [HttpGet("quiz/{quizId}/quizset/{quizSetId}")]
        public async Task<ActionResult<ResponseQuizQuizSetDto>> GetByQuizAndQuizSet(
            Guid quizId,
            Guid quizSetId)
        {
            var result = await _quizQuizSetService.GetByQuizAndQuizSetAsync(quizId, quizSetId);
            if (result == null)
                throw new HttpException(HttpStatusCode.NotFound, "Quiz-QuizSet association not found");

            return Ok(result);
        }

        /// <summary>
        /// Updates a quiz-quizset association
        /// </summary>
        /// <param name="id">Association ID</param>
        /// <param name="dto">Updated association data</param>
        /// <returns>Updated association</returns>
        [HttpPut("{id}")]
        [SubscriptionAndRoleAuthorize("Administrator", "Moderator")]
        public async Task<ActionResult<ResponseQuizQuizSetDto>> Update(
            Guid id,
            [FromBody] RequestQuizQuizSetDto dto)
        {
            if (!ModelState.IsValid)
                throw new HttpException(HttpStatusCode.BadRequest, "Invalid model state");

            try
            {
                var result = await _quizQuizSetService.UpdateAsync(id, dto);
                if (result == null)
                    throw new HttpException(HttpStatusCode.NotFound, "Quiz-QuizSet association not found");

                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                throw new HttpException(HttpStatusCode.BadRequest, ex.Message);
            }
        }

        /// <summary>
        /// Soft deletes a quiz-quizset association
        /// </summary>
        /// <param name="id">Association ID</param>
        /// <returns>Success status</returns>
        [HttpDelete("{id}")]
        [SubscriptionAndRoleAuthorize("Administrator", "Moderator")]
        public async Task<IActionResult> SoftDelete(Guid id)
        {
            var result = await _quizQuizSetService.SoftDeleteAsync(id);
            if (!result)
                throw new HttpException(HttpStatusCode.NotFound, "Quiz-QuizSet association not found");

            return NoContent();
        }

        /// <summary>
        /// Hard deletes a quiz-quizset association (permanently removes from database)
        /// </summary>
        /// <param name="id">Association ID</param>
        /// <returns>Success status</returns>
        [HttpDelete("{id}/permanent")]
        [SubscriptionAndRoleAuthorize("Administrator")]
        public async Task<IActionResult> HardDelete(Guid id)
        {
            var result = await _quizQuizSetService.HardDeleteAsync(id);
            if (!result)
                throw new HttpException(HttpStatusCode.NotFound, "Quiz-QuizSet association not found");

            return NoContent();
        }

        /// <summary>
        /// Checks if a quiz is associated with a quiz set
        /// </summary>
        /// <param name="quizId">Quiz ID</param>
        /// <param name="quizSetId">QuizSet ID</param>
        /// <returns>Existence status</returns>
        [HttpGet("exists/quiz/{quizId}/quizset/{quizSetId}")]
        public async Task<ActionResult<bool>> Exists(Guid quizId, Guid quizSetId)
        {
            var result = await _quizQuizSetService.ExistsAsync(quizId, quizSetId);
            return Ok(new { exists = result });
        }

        /// <summary>
        /// Gets the count of quizzes in a quiz set
        /// </summary>
        /// <param name="quizSetId">QuizSet ID</param>
        /// <returns>Quiz count</returns>
        [HttpGet("quizset/{quizSetId}/count")]
        public async Task<ActionResult<int>> GetQuizCount(Guid quizSetId)
        {
            var result = await _quizQuizSetService.GetQuizCountByQuizSetAsync(quizSetId);
            return Ok(new { quizSetId, count = result });
        }

        /// <summary>
        /// Adds a quiz to a quiz set
        /// </summary>
        /// <param name="quizId">Quiz ID</param>
        /// <param name="quizSetId">QuizSet ID</param>
        /// <returns>Success status</returns>
        [HttpPost("quiz/{quizId}/quizset/{quizSetId}")]
        [SubscriptionAndRoleAuthorize("Administrator", "Moderator")]
        public async Task<IActionResult> AddQuizToQuizSet(Guid quizId, Guid quizSetId)
        {
            var result = await _quizQuizSetService.AddQuizToQuizSetAsync(quizId, quizSetId);
            if (!result)
                throw new HttpException(HttpStatusCode.BadRequest, "Failed to add quiz to quiz set");

            return Ok(new { message = "Quiz successfully added to quiz set" });
        }

        /// <summary>
        /// Removes a quiz from a quiz set
        /// </summary>
        /// <param name="quizId">Quiz ID</param>
        /// <param name="quizSetId">QuizSet ID</param>
        /// <returns>Success status</returns>
        [HttpDelete("quiz/{quizId}/quizset/{quizSetId}")]
        [SubscriptionAndRoleAuthorize("Administrator", "Moderator")]
        public async Task<IActionResult> RemoveQuizFromQuizSet(Guid quizId, Guid quizSetId)
        {
            var result = await _quizQuizSetService.RemoveQuizFromQuizSetAsync(quizId, quizSetId);
            if (!result)
                throw new HttpException(HttpStatusCode.NotFound, "Quiz-QuizSet association not found");

            return Ok(new { message = "Quiz successfully removed from quiz set" });
        }

        /// <summary>
        /// Adds multiple quizzes to a quiz set in bulk
        /// </summary>
        /// <param name="quizSetId">QuizSet ID</param>
        /// <param name="request">List of quiz IDs</param>
        /// <returns>Success status</returns>
        [HttpPost("quizset/{quizSetId}/bulk-add")]
        [SubscriptionAndRoleAuthorize("Administrator", "Moderator")]
        public async Task<IActionResult> AddQuizzesToQuizSet(
            Guid quizSetId,
            [FromBody] BulkAddQuizzesRequest request)
        {
            if (request?.QuizIds == null || !request.QuizIds.Any())
                throw new HttpException(HttpStatusCode.BadRequest, "Quiz IDs list cannot be empty");

            var result = await _quizQuizSetService.AddQuizzesToQuizSetAsync(request.QuizIds, quizSetId);
            if (!result)
                throw new HttpException(HttpStatusCode.BadRequest, "Failed to add quizzes to quiz set");

            return Ok(new { 
                message = "Quizzes successfully added to quiz set",
                quizSetId,
                addedQuizIds = request.QuizIds
            });
        }

        /// <summary>
        /// Removes all quiz associations for a specific quiz
        /// </summary>
        /// <param name="quizId">Quiz ID</param>
        /// <returns>Success status</returns>
        [HttpDelete("quiz/{quizId}/all-associations")]
        [SubscriptionAndRoleAuthorize("Administrator", "Moderator")]
        public async Task<IActionResult> DeleteByQuizId(Guid quizId)
        {
            var result = await _quizQuizSetService.DeleteByQuizIdAsync(quizId);
            if (!result)
                throw new HttpException(HttpStatusCode.NotFound, "No associations found for the specified quiz");

            return Ok(new { message = "All quiz associations successfully removed" });
        }

        /// <summary>
        /// Removes all quiz associations for a specific quiz set
        /// </summary>
        /// <param name="quizSetId">QuizSet ID</param>
        /// <returns>Success status</returns>
        [HttpDelete("quizset/{quizSetId}/all-associations")]
        [SubscriptionAndRoleAuthorize("Administrator", "Moderator")]
        public async Task<IActionResult> DeleteByQuizSetId(Guid quizSetId)
        {
            var result = await _quizQuizSetService.DeleteByQuizSetIdAsync(quizSetId);
            if (!result)
                throw new HttpException(HttpStatusCode.NotFound, "No associations found for the specified quiz set");

            return Ok(new { message = "All quiz set associations successfully removed" });
        }
    }

    /// <summary>
    /// Request model for bulk adding quizzes to a quiz set
    /// </summary>
    public class BulkAddQuizzesRequest
    {
        public List<Guid> QuizIds { get; set; } = new List<Guid>();
    }
}