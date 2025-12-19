using BusinessLogic.DTOs.AdminDashboardDtos;

namespace BusinessLogic.Interfaces
{
    public interface IAdminDashboardService
    {
        Task<AdminOverviewDto> GetOverviewAsync();
        Task<List<RevenuePeriodDto>> GetRevenueAsync(string timeRange, DateTime? startDate, DateTime? endDate);
        Task<TransactionStatusDistributionDto> GetTransactionStatusDistributionAsync();
        Task<List<UserGrowthDto>> GetUserGrowthAsync(int months);
        Task<List<EventStatsDto>> GetEventStatsAsync(int limit, string sortBy);
        Task<List<TournamentStatsDto>> GetTournamentStatsAsync(int limit, string sortBy);
        //Task<List<AIUsageDto>> GetAIUsageAsync(string timeRange, string groupBy);
        Task<List<SubscriptionDistributionDto>> GetSubscriptionDistributionAsync();
        Task<QuizSetStatsDto> GetQuizSetStatsAsync();
        Task<ActivitySummaryDto> GetActivitySummaryAsync(int days);
    }
}

