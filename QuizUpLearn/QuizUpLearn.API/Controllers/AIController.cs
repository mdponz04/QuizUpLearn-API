using BusinessLogic.DTOs.AiDtos;
using BusinessLogic.DTOs.QuizSetDtos;
using BusinessLogic.Helpers;
using BusinessLogic.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using QuizUpLearn.API.Hubs;
using Repository.Enums;

namespace QuizUpLearn.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AIController : ControllerBase
    {
        private readonly IAIService _aiService;
        private readonly IWorkerService _workerService;
        private readonly IQuizSetService _quizSetService;

        public AIController(IAIService aiService, IWorkerService workerService, IQuizSetService quizSetService)
        {
            _aiService = aiService;
            _workerService = workerService;
            _quizSetService = quizSetService;
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
        public async Task<IActionResult> GenerateQuizSet([FromBody] AiGenerateQuizSetRequestDto inputData, QuizPartEnums quizPart)
        {
            if (inputData == null)
            {
                return BadRequest("Prompt cannot be empty.");
            }
            // Create a new quiz set first to hold the generated quizzes
            var jobId = Guid.NewGuid();
            var createdQuizSet = await _quizSetService.CreateQuizSetAsync(new QuizSetRequestDto
            {
                Title = inputData.Topic,
                Description = $"AI-generated TOEIC practice quiz on {inputData.Topic} focus on TOEIC {quizPart.ToString()}",
                QuizType = QuizSetTypeEnum.Practice,
                DifficultyLevel = inputData.Difficulty,
                CreatedBy = inputData.CreatorId
            });
            var quizSetId = createdQuizSet.Id;

            _ = _workerService.EnqueueJob(async (sp, token) =>
            {
                var aiService = sp.GetRequiredService<IAIService>();
                var hubContext = sp.GetRequiredService<IHubContext<BackgroundJobHub>>();
                
                try
                {
                    var result = false;
                    
                    await hubContext.Clients.Group(jobId.ToString()).SendAsync("JobFailed", new
                    {
                        JobId = jobId,
                        Result = "",
                        Status = "Processing",
                        QuizSetId = quizSetId
                    });

                    switch (quizPart)
                    {
                        case QuizPartEnums.PART1:

                            result = await aiService.GeneratePracticeQuizSetPart1Async(inputData, quizSetId);
                            break;
                        case QuizPartEnums.PART2:
                            result = await aiService.GeneratePracticeQuizSetPart2Async(inputData, quizSetId);
                            break;
                        case QuizPartEnums.PART3:
                            result = await aiService.GeneratePracticeQuizSetPart3Async(inputData, quizSetId);
                            break;
                        case QuizPartEnums.PART4:
                            result = await aiService.GeneratePracticeQuizSetPart4Async(inputData, quizSetId);
                            break;
                        case QuizPartEnums.PART5:
                            result = await aiService.GeneratePracticeQuizSetPart5Async(inputData, quizSetId);
                            break;
                        case QuizPartEnums.PART6:
                            result = await aiService.GeneratePracticeQuizSetPart6Async(inputData, quizSetId);
                            break;
                        case QuizPartEnums.PART7:
                            result = await aiService.GeneratePracticeQuizSetPart7Async(inputData, quizSetId);
                            break;
                    }

                    await hubContext.Clients.Group(jobId.ToString()).SendAsync("JobFailed", new
                    {
                        JobId = jobId,
                        Result = "",
                        Status = "Validating",
                        QuizSetId = quizSetId
                    });

                    var validateResult = await aiService.ValidateQuizSetAsync(quizSetId);

                    if (!validateResult.Item1)
                    {
                        await hubContext.Clients.Group(jobId.ToString()).SendAsync("JobFailed", new
                        {
                            JobId = jobId,
                            Result = "Invalid quiz set: " + validateResult.Item2,
                            Status = "Failed",
                            QuizSetId = Guid.Empty
                        });
                    }
                    else
                    {
                        await hubContext.Clients.Group(jobId.ToString()).SendAsync("JobCompleted", new
                        {
                            JobId = jobId,
                            Result = result,
                            Status = "Completed",
                            QuizSetId = quizSetId
                        });
                    }
                }
                catch (Exception ex)
                {
                    await hubContext.Clients.Group(jobId.ToString()).SendAsync("JobFailed", new
                    {
                        JobId = jobId,
                        Result = ex.Message,
                        Status = "Failed",
                        QuizSetId = Guid.Empty
                    });
                }
            });

            return Ok(new { JobId = jobId, Message = "Quiz set generation started in background.", Status = "Processing", QuizSetId = quizSetId });
        }
        /// <summary>
        /// This endpoint analyzes a user's mistakes and provides personalized advices & weak points.
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        [HttpPost("ai-analyze-user-mistakes")]
        public async Task<IActionResult> AnalyzeUserMistakesAndAdvise(Guid userId)
        {
            if(userId == Guid.Empty)
            {
                return BadRequest("UserId cannot be empty.");
            }

            var result = await _aiService.AnalyzeUserMistakesAndAdviseAsync(userId);
            return Ok(result);
        }
    }
}
