using BusinessLogic.DTOs;
using BusinessLogic.DTOs.SubscriptionDtos;
using BusinessLogic.Interfaces;

namespace BusinessLogic.Services
{
    public class SubscriptionUsageService : ISubscriptionUsageService
    {
        private readonly ISubscriptionPlanService _subscriptionPlanService;
        private readonly ISubscriptionService _subscriptionService;
        private readonly IAppSettingService _appSettingService;

        public SubscriptionUsageService(ISubscriptionPlanService subscriptionPlanService, ISubscriptionService subscriptionService, IAppSettingService appSettingService)
        {
            _subscriptionPlanService = subscriptionPlanService;
            _subscriptionService = subscriptionService;
            _appSettingService = appSettingService;
        }
        public async Task ResetUsageForFreeSubscriptions(int resetDay)
        {
            var latestReset = await _appSettingService.GetByKeyAsync("LatestResetAt");
            
            // If not existed -> create new and do reset
            if (latestReset == null)
            {
                await DoReset();
                await _appSettingService.CreateAsync(new AppSettingDto
                {
                    Key = "LatestResetAt",
                    Value = DateTime.UtcNow.ToString("O")
                });
                return;
            }
            var latestResetAt = DateTimeOffset.Parse(latestReset.Value).UtcDateTime;
            var now = DateTime.UtcNow;
            
            // Check if we're on the reset day of a new month and haven't reset this month yet
            if (now.Day == resetDay && (latestResetAt.Month != now.Month || latestResetAt.Year != now.Year))
            {
                await DoReset();
                await _appSettingService.UpdateAsync("LatestResetAt", new AppSettingDto
                {
                    Value = now.ToString("O")
                });
            }
        }

        private async Task DoReset()
        {
            var freePlan = await _subscriptionPlanService.GetFreeSubscriptionPlanAsync();
            var freePlanSubscription = await _subscriptionService.GetByPlanIdAsync(freePlan.Id);

            if (freePlanSubscription == null)
            {
                Console.WriteLine("There are no free plan subscription to reset usage");
                return;
            }

            foreach (var subscription in freePlanSubscription)
            {
                await _subscriptionService.UpdateAsync(subscription.Id, new RequestSubscriptionDto
                {
                    SubscriptionPlanId = freePlan.Id,
                    AiGenerateQuizSetRemaining = freePlan.AiGenerateQuizSetMaxTimes
                });
            }
            return;
        }
    }
}
