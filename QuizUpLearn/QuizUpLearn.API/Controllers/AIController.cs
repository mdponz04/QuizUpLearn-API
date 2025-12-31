using BusinessLogic.DTOs.AiDtos;
using BusinessLogic.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using QuizUpLearn.API.Attributes;
using QuizUpLearn.API.Hubs;

namespace QuizUpLearn.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AIController : ControllerBase
    {
        private readonly IAIService _aiService;
        private readonly IWorkerService _workerService;
        private readonly IQuizService _quizService;

        public AIController(IAIService aiService, IWorkerService workerService, IQuizService quizService)
        {
            _aiService = aiService;
            _workerService = workerService;
            _quizService = quizService;
        }

        [HttpPost("generate-quiz-set")]
        [SubscriptionAndRoleAuthorize("Moderator")]
        public async Task<IActionResult> GeneratePracticeQuizSet([FromBody] AiGenerateQuizRequestDto inputData)
        {
            if (inputData == null)
            {
                return BadRequest("Prompt cannot be empty.");
            }
            
            var userId = (Guid)HttpContext.Items["UserId"]!;

            inputData.CreatorId = userId;
            var jobId = Guid.NewGuid();

            _workerService.RegisterActiveJob(userId, jobId);

            _ = _workerService.EnqueueJob(async (sp, token) =>
            {
                var subscriptionService = sp.GetRequiredService<ISubscriptionService>();
                var aiService = sp.GetRequiredService<IAIService>();
                var hubContext = sp.GetRequiredService<IHubContext<BackgroundJobHub>>();
                var workerService = sp.GetRequiredService<IWorkerService>();
                (Guid, Guid?) result = (Guid.Empty, Guid.Empty);

                await hubContext.Clients.Group(jobId.ToString()).SendAsync("JobStarted", new
                {
                    JobId = jobId,
                    Result = "Start generate quiz",
                    Status = "Processing"
                });

                try
                {
                    result = await aiService.GeneratePracticeQuizzesAsync(inputData);
                }
                catch (Exception ex)
                {
                    workerService.RemoveActiveJob(jobId);

                    await hubContext.Clients.Group(jobId.ToString()).SendAsync("JobFailed", new
                    {
                        JobId = jobId,
                        Result = "Failed at generate quiz: " + ex.Message,
                        Status = "Failed"
                    });

                    return;
                }

                await hubContext.Clients.Group(jobId.ToString()).SendAsync("JobCompleted", new
                {
                    JobId = jobId,
                    Result = "Success",
                    Status = "Success",
                    QuizGroupItemId = result.Item1,
                    SingleQuizId = result.Item2
                });
            });

            return Ok(new { JobId = jobId, Message = "Quiz set generation started in background.", Status = "Processing" });
        }

        [HttpGet("active-job")]
        [SubscriptionAndRoleAuthorize("Moderator")]
        public IActionResult GetActiveJob()
        {
            var userId = (Guid)HttpContext.Items["UserId"]!;
            var activeJob = _workerService.GetActiveJobForUser(userId);
            
            if (activeJob.HasValue)
            {
                return Ok(new 
                { 
                    HasActiveJob = true,
                    JobId = activeJob.Value
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
    }
}
