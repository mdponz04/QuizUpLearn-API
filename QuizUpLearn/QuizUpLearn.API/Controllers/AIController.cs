using BusinessLogic.DTOs.AiDtos;
using BusinessLogic.DTOs.QuizSetDtos;
using BusinessLogic.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using QuizUpLearn.API.Attributes;
using QuizUpLearn.API.Hubs;
using Repository.Enums;

namespace QuizUpLearn.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
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

        [HttpPost("validate-quiz-set/{quizSetId}")]
        [SubscriptionAndRoleAuthorize(RequireAiFeatures = true, AllowBypass = true)]
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
        /// Quiz Type enums: 0 = Practice, 1 = Placement, 2 = Tournament, 3 = Event
        /// Admin can generate any type, Teachers can generate Practice/Event, Premium Users can generate Practice only
        /// </summary>
        /// <param name="inputData"></param>
        /// <param name="quizPart"></param>
        /// <param name="quizSetType"></param>
        /// <returns></returns>
        [HttpPost("generate-quiz-set")]
        [SubscriptionAndRoleAuthorize(RequireAiFeatures = true, CheckRemainingUsage = true, AllowBypass = true)]
        public async Task<IActionResult> GeneratePracticeQuizSet([FromBody] AiGenerateQuizSetRequestDto inputData, QuizPartEnum quizPart, QuizSetTypeEnum quizSetType)
        {
            if (inputData == null)
            {
                return BadRequest("Prompt cannot be empty.");
            }

            // Get user role and check permissions for specific quiz set types
            var userRole = HttpContext.Items["UserRole"]?.ToString();
            var isAdmin = (bool)(HttpContext.Items["IsAdmin"] ?? false);
            var isMod = (bool)(HttpContext.Items["IsMod"] ?? false);
            var remainingUsage = (int) (HttpContext.Items["RemainingUsage"] ?? 0);
            // Role-based quiz set type restrictions
            switch (quizSetType)
            {
                case QuizSetTypeEnum.Tournament:
                case QuizSetTypeEnum.Placement:
                case QuizSetTypeEnum.Event:
                    if (!isAdmin && !isMod)
                    {
                        return Forbid($"Only Admin and Mod can generate {quizSetType} quiz sets.");
                    }
                    break;
                case QuizSetTypeEnum.Practice:
                    // All authorized users can generate practice quiz sets
                    break;
                default:
                    return BadRequest("Invalid quiz set type.");
            }
            //Check if user has enough remaining usage
            if(inputData.QuestionQuantity > remainingUsage && (!isAdmin && !isMod))
            {
                return BadRequest("Insufficient remaining usage to generate the requested number of questions.");
            }
            // Get user ID from HttpContext (set by SubscriptionAndRoleAuthorizeAttribute)
            var userId = (Guid)HttpContext.Items["UserId"]!;

            inputData.CreatorId = userId;

            var jobId = Guid.NewGuid();
            var createdQuizSet = await _quizSetService.CreateQuizSetAsync(new QuizSetRequestDto
            {
                Title = inputData.Topic,
                Description = $"AI-generated TOEIC {quizSetType.ToString()} quiz set on {inputData.Topic} focus on TOEIC {quizPart.ToString()}",
                QuizSetType = quizSetType,
                DifficultyLevel = inputData.Difficulty,
                CreatedBy = inputData.CreatorId,
                IsAIGenerated = true
            });
            var quizSetId = createdQuizSet.Id;

            _workerService.RegisterActiveJob(userId, jobId, quizSetId);

            _ = _workerService.EnqueueJob(async (sp, token) =>
            {
                var subscriptionService = sp.GetRequiredService<ISubscriptionService>();
                var aiService = sp.GetRequiredService<IAIService>();
                var hubContext = sp.GetRequiredService<IHubContext<BackgroundJobHub>>();
                var workerService = sp.GetRequiredService<IWorkerService>();

                (bool, string) validateResult = (false, "Not validate yet");
                try
                {
                    var result = false;

                    await hubContext.Clients.Group(jobId.ToString()).SendAsync("JobStarted", new
                    {
                        JobId = jobId,
                        Result = "",
                        Status = "Processing",
                        QuizSetId = quizSetId
                    });
                    try
                    {
                        switch (quizPart)
                        {

                            case QuizPartEnum.PART1:
                                result = await aiService.GeneratePracticeQuizSetPart1Async(inputData, quizSetId);
                                break;
                            case QuizPartEnum.PART2:
                                result = await aiService.GeneratePracticeQuizSetPart2Async(inputData, quizSetId);
                                break;
                            case QuizPartEnum.PART3:
                                result = await aiService.GeneratePracticeQuizSetPart3Async(inputData, quizSetId);
                                break;
                            case QuizPartEnum.PART4:
                                result = await aiService.GeneratePracticeQuizSetPart4Async(inputData, quizSetId);
                                break;
                            case QuizPartEnum.PART5:
                                result = await aiService.GeneratePracticeQuizSetPart5Async(inputData, quizSetId);
                                break;
                            case QuizPartEnum.PART6:
                                result = await aiService.GeneratePracticeQuizSetPart6Async(inputData, quizSetId);
                                break;
                            case QuizPartEnum.PART7:
                                result = await aiService.GeneratePracticeQuizSetPart7Async(inputData, quizSetId);
                                break;
                        }
                    }
                    catch(Exception ex)
                    {
                        workerService.RemoveActiveJob(jobId);

                        await hubContext.Clients.Group(jobId.ToString()).SendAsync("JobFailed", new
                        {
                            JobId = jobId,
                            Result = "Failed at generate quiz: " + ex.Message,
                            Status = "Failed",
                            QuizSetId = Guid.Empty
                        });

                        return;
                    }
                    

                    await hubContext.Clients.Group(jobId.ToString()).SendAsync("JobValidating", new
                    {
                        JobId = jobId,
                        Result = "Validating",
                        Status = "Validating",
                        QuizSetId = quizSetId
                    });

                    validateResult = await aiService.ValidateQuizSetAsync(quizSetId);
                }
                catch (Exception ex)
                {
                    workerService.RemoveActiveJob(jobId);
                    
                    await hubContext.Clients.Group(jobId.ToString()).SendAsync("JobFailed", new
                    {
                        JobId = jobId,
                        Result = "Failed at generate quiz: " + ex.Message,
                        Status = "Failed",
                        QuizSetId = Guid.Empty
                    });

                    return;
                }

                if (!validateResult.Item1)
                {
                    workerService.RemoveActiveJob(jobId);
                    
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
                    if (!isAdmin || !isMod)
                    {
                        await subscriptionService.CalculateRemainingUsageByUserId(userId, inputData.QuestionQuantity);
                    }

                    workerService.RemoveActiveJob(jobId);

                    await hubContext.Clients.Group(jobId.ToString()).SendAsync("JobCompleted", new
                    {
                        JobId = jobId,
                        Result = "Completed",
                        Status = "Completed",
                        QuizSetId = quizSetId
                    });
                }
            });

            return Ok(new { JobId = jobId, Message = "Quiz set generation started in background.", Status = "Processing", QuizSetId = quizSetId });
        }

        [HttpGet("active-job")]
        [SubscriptionAndRoleAuthorize(RequireAiFeatures = true, AllowBypass = true)]
        public IActionResult GetActiveJob()
        {
            var userId = (Guid)HttpContext.Items["UserId"]!;
            var activeJob = _workerService.GetActiveJobForUser(userId);
            
            if (activeJob.HasValue)
            {
                return Ok(new 
                { 
                    HasActiveJob = true,
                    JobId = activeJob.Value.jobId, 
                    QuizSetId = activeJob.Value.quizSetId 
                });
            }
            
            return Ok(new { HasActiveJob = false });
        }

        [HttpPost("ai-analyze-user-mistakes")]
        [SubscriptionAndRoleAuthorize(RequireAiFeatures = true)]
        public async Task<IActionResult> AnalyzeUserMistakesAndAdvise()
        {
            var userId = (Guid)HttpContext.Items["UserId"]!;

            await _aiService.AnalyzeUserMistakesAndAdviseAsync(userId);
            return Ok();
        }
        /*[HttpPost("generate-fix-weakpoint-quiz-set")]
        [SubscriptionAndRoleAuthorize("Moderator", "User", RequireAiFeatures = true, CheckRemainingUsage = true, RequirePremiumContent = true)]
        public async Task<IActionResult> GenerateFixWeakPointQuizSet()
        {
            var userId = (Guid)HttpContext.Items["UserId"]!;

            var result = await _aiService.GenerateFixWeakPointQuizSetAsync(userId);
            return Ok(result);
        }*/
    }
}
