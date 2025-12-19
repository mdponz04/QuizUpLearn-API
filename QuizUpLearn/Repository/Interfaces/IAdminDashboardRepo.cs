using Repository.Entities;
using Repository.Enums;

namespace Repository.Interfaces
{
    public interface IAdminDashboardRepo
    {
        Task<long> GetTotalRevenueAsync();
        Task<int> GetCompletedTransactionsCountAsync();
        Task<int> GetTotalUsersCountAsync();
        Task<int> GetActiveSubscriptionsCountAsync();
        Task<int> GetTotalEventsCountAsync();
        Task<int> GetTotalTournamentsCountAsync();
        /*Task<int> GetTotalAIUsageCountAsync();*/
        Task<int> GetTotalQuizSetsCountAsync();
        Task<List<PaymentTransaction>> GetCompletedTransactionsAsync(DateTime? startDate, DateTime? endDate);
        Task<Dictionary<TransactionStatusEnum, int>> GetTransactionStatusCountsAsync();
        Task<List<User>> GetUsersByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<int> GetUsersCountBeforeDateAsync(DateTime date);
        Task<List<Event>> GetEventsWithDetailsAsync();
        Task<Dictionary<Guid, int>> GetEventParticipantCountsAsync();
        Task<List<Tournament>> GetTournamentsAsync();
        Task<Dictionary<Guid, int>> GetTournamentParticipantCountsAsync();
        /*Task<List<QuizSet>> GetAIQuizSetsByDateRangeAsync(DateTime startDate, DateTime endDate);*/
        Task<List<Subscription>> GetActiveSubscriptionsWithPlansAsync();
        Task<List<PaymentTransaction>> GetTransactionsByPlanIdAsync(Guid planId);
        Task<List<QuizSet>> GetQuizSetsWithQuizzesAsync();
        Task<List<User>> GetUsersByDateRangeForActivityAsync(DateTime startDate);
        Task<List<QuizAttempt>> GetQuizAttemptsByDateRangeAsync(DateTime startDate);
        Task<List<Event>> GetEventsByDateRangeAsync(DateTime startDate);
        Task<List<Tournament>> GetTournamentsByDateRangeAsync(DateTime startDate);
        Task<List<PaymentTransaction>> GetTransactionsByDateRangeAsync(DateTime startDate);
        /*Task<int> GetAIQuizSetsCountByDateRangeAsync(DateTime startDate);*/
    }
}

