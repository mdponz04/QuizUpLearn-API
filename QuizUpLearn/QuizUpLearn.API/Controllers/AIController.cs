using BusinessLogic.DTOs.AiDtos;
using BusinessLogic.DTOs.QuizSetDtos;
using BusinessLogic.Helpers;
using BusinessLogic.Interfaces;
using BusinessLogic.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using QuizUpLearn.API.Hubs;

namespace QuizUpLearn.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AIController : ControllerBase
    {
        private readonly IAIService _aiService;
        private readonly IWorkerService _workerService;
        private readonly IHubContext<BackgroundJobHub> _hubContext;

        public AIController(IAIService aiService, IWorkerService workerService, IHubContext<BackgroundJobHub> hubContext)
        {
            _aiService = aiService;
            _workerService = workerService;
            _hubContext = hubContext;
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
        /// This is background job endpoint. It will return data to JobCompleted & JobFailed events in SignalR hub.
        /// Choose quiz part to generate quiz set for this part.
        /// </summary>
        /// <param name="inputData"></param>
        /// <returns></returns>
        [HttpPost("generate-quiz-set")]
        public async Task<IActionResult> GenerateQuizSetPart([FromBody] AiGenerateQuizSetRequestDto inputData, QuizPartEnums quizPart)
        {
            if (inputData == null)
            {
                return BadRequest("Prompt cannot be empty.");
            }

            var jobId = Guid.NewGuid();

            _ = _workerService.EnqueueJob(async (sp, token) =>
            {
                var aiService = sp.GetRequiredService<IAIService>();
                var hubContext = sp.GetRequiredService<IHubContext<BackgroundJobHub>>();
                
                try
                {
                    QuizSetResponseDto result = new();
                    switch (quizPart)
                    {
                        case QuizPartEnums.PART1:
                            result = await aiService.GeneratePracticeQuizSetPart1Async(inputData);
                            break;
                        case QuizPartEnums.PART2:
                            result = await aiService.GeneratePracticeQuizSetPart2Async(inputData);
                            break;
                        case QuizPartEnums.PART3:
                            result = await aiService.GeneratePracticeQuizSetPart3Async(inputData);
                            break;
                        case QuizPartEnums.PART4:
                            result = await aiService.GeneratePracticeQuizSetPart4Async(inputData);
                            break;
                        case QuizPartEnums.PART5:
                            result = await aiService.GeneratePracticeQuizSetPart5Async(inputData);
                            break;
                        case QuizPartEnums.PART6:
                            result = await aiService.GeneratePracticeQuizSetPart6Async(inputData);
                            break;
                        case QuizPartEnums.PART7:
                            result = await aiService.GeneratePracticeQuizSetPart7Async(inputData);
                            break;
                    }

                    var validateResult = await aiService.ValidateQuizSetAsync(result.Id);
                    
                    Console.WriteLine($"QuizSet Id: {result.Id}, Validation: {validateResult.Item1}, Feedback: {validateResult.Item2}");

                    if (!validateResult.Item1)
                    {
                        await hubContext.Clients.All.SendAsync("JobCompleted", new
                        {
                            JobId = jobId,
                            Error = "Invalid quiz set: " + validateResult.Item2
                        });
                    }
                    else
                    {
                        await hubContext.Clients.All.SendAsync("JobCompleted", new
                        {
                            JobId = jobId,
                            Result = result
                        });
                    }
                }
                catch (Exception ex)
                {
                    await hubContext.Clients.All.SendAsync("JobFailed", new
                    {
                        JobId = jobId,
                        Error = ex.Message
                    });
                }
            });

            return Ok(new { JobId = jobId, Message = "Quiz set generation started in background." });
        }

        /*[HttpPost("generate-quiz-set-part-2")]
        public async Task<IActionResult> GenerateQuizSetPart2([FromBody] AiGenerateQuizSetRequestDto inputData)
        {
            if (inputData == null)
            {
                return BadRequest("Prompt cannot be empty.");
            }
            var result = await _aiService.GeneratePracticeQuizSetPart2Async(inputData);
            var validateResult = await _aiService.ValidateQuizSetAsync(result.Id);

            if (!validateResult.Item1)
            {
                return BadRequest("Invalid quiz set: " + validateResult.Item2);
            }
            else
            {
                return Ok(result);
            }
        }
        [HttpPost("generate-quiz-set-part-3")]
        public async Task<IActionResult> GenerateQuizSetPart3([FromBody] AiGenerateQuizSetRequestDto inputData)
        {
            if (inputData == null)
            {
                return BadRequest("Prompt cannot be empty.");
            }
            var result = await _aiService.GeneratePracticeQuizSetPart3Async(inputData);
            var validateResult = await _aiService.ValidateQuizSetAsync(result.Id);

            if (!validateResult.Item1)
            {
                return BadRequest("Invalid quiz set: " + validateResult.Item2);
            }
            else
            {
                return Ok(result);
            }
        }
        [HttpPost("generate-quiz-set-part-4")]
        public async Task<IActionResult> GenerateQuizSetPart4([FromBody] AiGenerateQuizSetRequestDto inputData)
        {
            if (inputData == null)
            {
                return BadRequest("Prompt cannot be empty.");
            }
            var result = await _aiService.GeneratePracticeQuizSetPart4Async(inputData);
            var validateResult = await _aiService.ValidateQuizSetAsync(result.Id);

            if (!validateResult.Item1)
            {
                return BadRequest("Invalid quiz set: " + validateResult.Item2);
            }
            else
            {
                return Ok(result);
            }
        }
        [HttpPost("generate-quiz-set-part-5")]
        public async Task<IActionResult> GenerateQuizSetPart5([FromBody] AiGenerateQuizSetRequestDto inputData)
        {
            if (inputData == null)
            {
                return BadRequest("Prompt cannot be empty.");
            }
            var result = await _aiService.GeneratePracticeQuizSetPart5Async(inputData);
            var validateResult = await _aiService.ValidateQuizSetAsync(result.Id);

            if (!validateResult.Item1)
            {
                return BadRequest("Invalid quiz set: " + validateResult.Item2);
            }
            else
            {
                return Ok(result);
            }
        }
        [HttpPost("generate-quiz-set-part-6")]
        public async Task<IActionResult> GenerateQuizSetPart6([FromBody] AiGenerateQuizSetRequestDto inputData)
        {
            if (inputData == null)
            {
                return BadRequest("Prompt cannot be empty.");
            }
            var result = await _aiService.GeneratePracticeQuizSetPart6Async(inputData);
            var validateResult = await _aiService.ValidateQuizSetAsync(result.Id);

            if (!validateResult.Item1)
            {
                return BadRequest("Invalid quiz set: " + validateResult.Item2);
            }
            else
            {
                return Ok(result);
            }
        }
        [HttpPost("generate-quiz-set-part-7")]
        public async Task<IActionResult> GenerateQuizSetPart7([FromBody] AiGenerateQuizSetRequestDto inputData)
        {
            if (inputData == null)
            {
                return BadRequest("Prompt cannot be empty.");
            }
            var result = await _aiService.GeneratePracticeQuizSetPart7Async(inputData);
            var validateResult = await _aiService.ValidateQuizSetAsync(result.Id);

            if (!validateResult.Item1)
            {
                return BadRequest("Invalid quiz set: " + validateResult.Item2);
            }
            else
            {
                return Ok(result);
            }
        }*/
    }
}
