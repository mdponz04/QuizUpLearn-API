using BusinessLogic.DTOs;
using BusinessLogic.Interfaces;
using BusinessLogic.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace QuizUpLearn.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SubscriptionUsageController : ControllerBase
    {
        private readonly IWorkerService _workerService;
        private readonly IAppSettingService _appSettingService;
        private readonly ISubscriptionUsageService _subscriptionUsageService;

        public SubscriptionUsageController(IWorkerService workerService, IAppSettingService appSettingService, ISubscriptionUsageService subscriptionUsageService)
        {
            _workerService = workerService;
            _appSettingService = appSettingService;
            _subscriptionUsageService = subscriptionUsageService;
        }
        [HttpPost("reset")]
        public async Task<IActionResult> ResetSubscriptionUsageMonthly()
        {
            var appSetting = await _appSettingService.GetByKeyAsync("NextAllowRunResetUsageAt");

            // If missing -> create with current time (allows immediate first run)
            if (appSetting == null)
            {
                appSetting = await _appSettingService.CreateAsync(new AppSettingDto
                {
                    Key = "NextAllowRunResetUsageAt",
                    Value = DateTime.UtcNow.ToString("o")
                });
            }

            var nextAllowed = DateTimeOffset.Parse(appSetting.Value).UtcDateTime;

            if (DateTime.UtcNow < nextAllowed)
                return Ok("Reset already scheduled. Try again later.");

            var newCooldown = DateTime.UtcNow.AddHours(1);

            await _appSettingService.UpdateAsync("NextAllowRunResetUsageAt", new AppSettingDto
            {
                Value = newCooldown.ToString("o")
            });

            _ = _workerService.EnqueueJob(async (sp, token) =>
            {
                var usageService = sp.GetRequiredService<ISubscriptionUsageService>();
                //set reset day to 1 (first day of month)
                await usageService.ResetUsageForFreeSubscriptions(1);
            });

            return Ok("Reset scheduled.");
        }

        /*[HttpPost("reset-test")]
        public async Task<IActionResult> ResetSubscriptionUsageNow(int resetDay)
        {
            await _subscriptionUsageService.ResetUsageForFreeSubscriptions(resetDay);
            return Ok("Reset completed.");
        }*/
    }
}
