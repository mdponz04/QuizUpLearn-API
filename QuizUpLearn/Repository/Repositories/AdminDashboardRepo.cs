using Microsoft.EntityFrameworkCore;
using Repository.DBContext;
using Repository.Entities;
using Repository.Enums;
using Repository.Interfaces;

namespace Repository.Repositories
{
    public class AdminDashboardRepo : IAdminDashboardRepo
    {
        private readonly MyDbContext _context;

        public AdminDashboardRepo(MyDbContext context)
        {
            _context = context;
        }

        public async Task<long> GetTotalRevenueAsync()
        {
            return await _context.PaymentTransactions
                .Where(pt => pt.Status == TransactionStatusEnum.Completed && pt.DeletedAt == null)
                .SumAsync(pt => pt.Amount);
        }

        public async Task<int> GetCompletedTransactionsCountAsync()
        {
            return await _context.PaymentTransactions
                .Where(pt => pt.Status == TransactionStatusEnum.Completed && pt.DeletedAt == null)
                .CountAsync();
        }

        public async Task<int> GetTotalUsersCountAsync()
        {
            return await _context.Users
                .Where(u => u.DeletedAt == null)
                .CountAsync();
        }

        public async Task<int> GetActiveSubscriptionsCountAsync()
        {
            return await _context.Subscriptions
                .Where(s => s.DeletedAt == null && 
                           (s.EndDate == null || s.EndDate > DateTime.UtcNow))
                .CountAsync();
        }

        public async Task<int> GetTotalEventsCountAsync()
        {
            return await _context.Events
                .Where(e => e.DeletedAt == null)
                .CountAsync();
        }

        public async Task<int> GetTotalTournamentsCountAsync()
        {
            return await _context.Tournaments
                .Where(t => t.DeletedAt == null)
                .CountAsync();
        }

        public async Task<int> GetTotalAIUsageCountAsync()
        {
            return await _context.QuizSets
                .Where(qs => qs.IsAIGenerated && qs.DeletedAt == null)
                .CountAsync();
        }

        public async Task<int> GetTotalQuizSetsCountAsync()
        {
            return await _context.QuizSets
                .Where(qs => qs.DeletedAt == null)
                .CountAsync();
        }

        public async Task<List<PaymentTransaction>> GetCompletedTransactionsAsync(DateTime? startDate, DateTime? endDate)
        {
            var query = _context.PaymentTransactions
                .Where(pt => pt.Status == TransactionStatusEnum.Completed && 
                            pt.CompletedDate != null && 
                            pt.DeletedAt == null);

            if (startDate.HasValue)
                query = query.Where(pt => pt.CompletedDate >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(pt => pt.CompletedDate <= endDate.Value);

            return await query.ToListAsync();
        }

        public async Task<Dictionary<TransactionStatusEnum, int>> GetTransactionStatusCountsAsync()
        {
            var completed = await _context.PaymentTransactions
                .Where(pt => pt.Status == TransactionStatusEnum.Completed && pt.DeletedAt == null)
                .CountAsync();

            var pending = await _context.PaymentTransactions
                .Where(pt => pt.Status == TransactionStatusEnum.Pending && pt.DeletedAt == null)
                .CountAsync();

            var failed = await _context.PaymentTransactions
                .Where(pt => pt.Status == TransactionStatusEnum.Failed && pt.DeletedAt == null)
                .CountAsync();

            return new Dictionary<TransactionStatusEnum, int>
            {
                { TransactionStatusEnum.Completed, completed },
                { TransactionStatusEnum.Pending, pending },
                { TransactionStatusEnum.Failed, failed }
            };
        }

        public async Task<List<User>> GetUsersByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.Users
                .Where(u => u.DeletedAt == null && u.CreatedAt >= startDate && u.CreatedAt <= endDate)
                .OrderBy(u => u.CreatedAt)
                .ToListAsync();
        }

        public async Task<int> GetUsersCountBeforeDateAsync(DateTime date)
        {
            return await _context.Users
                .Where(u => u.DeletedAt == null && u.CreatedAt < date)
                .CountAsync();
        }

        public async Task<List<Event>> GetEventsWithDetailsAsync()
        {
            return await _context.Events
                .Where(e => e.DeletedAt == null)
                .Include(e => e.QuizSet)
                .ToListAsync();
        }

        public async Task<Dictionary<Guid, int>> GetEventParticipantCountsAsync()
        {
            var participants = await _context.EventParticipants
                .Where(ep => ep.DeletedAt == null)
                .GroupBy(ep => ep.EventId)
                .Select(g => new { EventId = g.Key, Count = g.Count() })
                .ToListAsync();

            return participants.ToDictionary(x => x.EventId, x => x.Count);
        }

        public async Task<List<Tournament>> GetTournamentsAsync()
        {
            return await _context.Tournaments
                .Where(t => t.DeletedAt == null)
                .ToListAsync();
        }

        public async Task<Dictionary<Guid, int>> GetTournamentParticipantCountsAsync()
        {
            var participants = await _context.TournamentParticipants
                .Where(tp => tp.DeletedAt == null)
                .GroupBy(tp => tp.TournamentId)
                .Select(g => new { TournamentId = g.Key, Count = g.Count() })
                .ToListAsync();

            return participants.ToDictionary(x => x.TournamentId, x => x.Count);
        }

        public async Task<List<QuizSet>> GetAIQuizSetsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.QuizSets
                .Where(qs => qs.IsAIGenerated && 
                            qs.DeletedAt == null && 
                            qs.CreatedAt >= startDate && 
                            qs.CreatedAt <= endDate)
                .ToListAsync();
        }

        public async Task<List<Subscription>> GetActiveSubscriptionsWithPlansAsync()
        {
            return await _context.Subscriptions
                .Where(s => s.DeletedAt == null && 
                           (s.EndDate == null || s.EndDate > DateTime.UtcNow))
                .Include(s => s.SubscriptionPlan)
                .ToListAsync();
        }

        public async Task<List<PaymentTransaction>> GetTransactionsByPlanIdAsync(Guid planId)
        {
            return await _context.PaymentTransactions
                .Where(pt => pt.Status == TransactionStatusEnum.Completed && 
                            pt.DeletedAt == null &&
                            pt.SubscriptionPlanId == planId)
                .ToListAsync();
        }

        public async Task<List<QuizSet>> GetQuizSetsWithQuizzesAsync()
        {
            return await _context.QuizSets
                .Where(qs => qs.DeletedAt == null)
                .ToListAsync();
        }

        public async Task<List<User>> GetUsersByDateRangeForActivityAsync(DateTime startDate)
        {
            return await _context.Users
                .Where(u => u.DeletedAt == null && u.CreatedAt >= startDate)
                .ToListAsync();
        }

        public async Task<List<QuizAttempt>> GetQuizAttemptsByDateRangeAsync(DateTime startDate)
        {
            return await _context.QuizAttempts
                .Where(qa => qa.DeletedAt == null && qa.CreatedAt >= startDate)
                .ToListAsync();
        }

        public async Task<List<Event>> GetEventsByDateRangeAsync(DateTime startDate)
        {
            return await _context.Events
                .Where(e => e.DeletedAt == null && e.CreatedAt >= startDate)
                .ToListAsync();
        }

        public async Task<List<Tournament>> GetTournamentsByDateRangeAsync(DateTime startDate)
        {
            return await _context.Tournaments
                .Where(t => t.DeletedAt == null && t.CreatedAt >= startDate)
                .ToListAsync();
        }

        public async Task<List<PaymentTransaction>> GetTransactionsByDateRangeAsync(DateTime startDate)
        {
            return await _context.PaymentTransactions
                .Where(pt => pt.DeletedAt == null && pt.CreatedAt >= startDate)
                .ToListAsync();
        }

        public async Task<int> GetAIQuizSetsCountByDateRangeAsync(DateTime startDate)
        {
            return await _context.QuizSets
                .Where(qs => qs.IsAIGenerated && 
                            qs.DeletedAt == null && 
                            qs.CreatedAt >= startDate)
                .CountAsync();
        }
    }
}

