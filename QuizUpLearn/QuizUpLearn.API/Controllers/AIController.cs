using BusinessLogic.DTOs.AiDtos;
using BusinessLogic.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace QuizUpLearn.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AIController : ControllerBase
    {
        private readonly IAIService _aiService;

        public AIController(IAIService aiService)
        {
            _aiService = aiService;
        }
        /// <summary>
        /// This endpoint validates an existing quiz set by its ID.
        /// </summary>
        /// <param name="quizSetId"></param>
        /// <returns></returns>
        [HttpGet("validate-quiz-set/{quizSetId}")]
        public async Task<IActionResult> ValidateQuizSet(Guid quizSetId)
        {
            if (quizSetId == Guid.Empty)
            {
                return BadRequest("QuizSetId cannot be empty.");
            }
            try
            {
                var (isValid, feedback) = await _aiService.ValidateQuizSetAsync(quizSetId);
                return Ok(new { isValid, feedback });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="inputData"></param>
        /// <returns></returns>
        [HttpPost("generate-quiz-set-part-1")]
        public async Task<IActionResult> GenerateQuizSetPart1([FromBody] AiGenerateQuizSetRequestDto inputData)
        {
            if (inputData == null)
            {
                return BadRequest("Prompt cannot be empty.");
            }
            try
            {
                var result = await _aiService.GeneratePracticeQuizSetPart1Async(inputData);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="inputData"></param>
        /// <returns></returns>
        [HttpPost("generate-quiz-set-part-2")]
        public async Task<IActionResult> GenerateQuizSetPart2([FromBody] AiGenerateQuizSetRequestDto inputData)
        {
            if (inputData == null)
            {
                return BadRequest("Prompt cannot be empty.");
            }
            try
            {
                var result = await _aiService.GeneratePracticeQuizSetPart2Async(inputData);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="inputData"></param>
        /// <returns></returns>
        [HttpPost("generate-quiz-set-part-3")]
        public async Task<IActionResult> GenerateQuizSetPart3([FromBody] AiGenerateQuizSetRequestDto inputData)
        {
            if (inputData == null)
            {
                return BadRequest("Prompt cannot be empty.");
            }
            try
            {
                var result = await _aiService.GeneratePracticeQuizSetPart3Async(inputData);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="inputData"></param>
        /// <returns></returns>
        [HttpPost("generate-quiz-set-part-5")]
        public async Task<IActionResult> GenerateQuizSetPart5([FromBody] AiGenerateQuizSetRequestDto inputData)
        {
            if (inputData == null)
            {
                return BadRequest("Prompt cannot be empty.");
            }
            try
            {
                var result = await _aiService.GeneratePracticeQuizSetPart5Async(inputData);
                var validate = await _aiService.ValidateQuizSetAsync(result.Id);
                if(!validate.Item1){
                    return StatusCode(500, $"The quiz set is invalid:\n{validate.Item2}");
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}
