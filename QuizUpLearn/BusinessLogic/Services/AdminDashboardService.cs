using BusinessLogic.DTOs.AdminDashboardDtos;
using BusinessLogic.Interfaces;
using Repository.Enums;
using Repository.Interfaces;

namespace BusinessLogic.Services
{
    public class AdminDashboardService : IAdminDashboardService
    {
        private readonly IAdminDashboardRepo _repo;

        public AdminDashboardService(IAdminDashboardRepo repo)
        {
            _repo = repo;
        }

        public async Task<AdminOverviewDto> GetOverviewAsync()
        {
            var totalRevenue = await _repo.GetTotalRevenueAsync();
            var completedTransactions = await _repo.GetCompletedTransactionsCountAsync();
            var totalUsers = await _repo.GetTotalUsersCountAsync();
            var activeSubscriptions = await _repo.GetActiveSubscriptionsCountAsync();
            var totalEvents = await _repo.GetTotalEventsCountAsync();
            var totalTournaments = await _repo.GetTotalTournamentsCountAsync();
            //var totalAIUsage = await _repo.GetTotalAIUsageCountAsync();
            var totalQuizSets = await _repo.GetTotalQuizSetsCountAsync();

            return new AdminOverviewDto
            {
                TotalRevenue = totalRevenue,
                CompletedTransactions = completedTransactions,
                TotalUsers = totalUsers,
                ActiveSubscriptions = activeSubscriptions,
                TotalEvents = totalEvents,
                TotalTournaments = totalTournaments,
                //TotalAIUsage = totalAIUsage,
                TotalQuizSets = totalQuizSets
            };
        }

        public async Task<List<RevenuePeriodDto>> GetRevenueAsync(string timeRange, DateTime? startDate, DateTime? endDate)
        {
            var transactions = await _repo.GetCompletedTransactionsAsync(startDate, endDate);

            var grouped = timeRange.ToLower() switch
            {
                "week" => transactions
                    .GroupBy(pt => GetWeekStart(pt.CompletedDate!.Value))
                    .Select(g => new RevenuePeriodDto
                    {
                        Period = FormatWeek(g.Key),
                        Amount = g.Sum(pt => pt.Amount),
                        TransactionCount = g.Count()
                    })
                    .OrderBy(r => r.Period)
                    .ToList(),

                "month" => transactions
                    .GroupBy(pt => new { pt.CompletedDate!.Value.Year, pt.CompletedDate.Value.Month })
                    .Select(g => new RevenuePeriodDto
                    {
                        Period = $"thg {g.Key.Month} {g.Key.Year}",
                        Amount = g.Sum(pt => pt.Amount),
                        TransactionCount = g.Count()
                    })
                    .OrderBy(r => r.Period)
                    .ToList(),

                "quarter" => transactions
                    .GroupBy(pt => new { pt.CompletedDate!.Value.Year, Quarter = (pt.CompletedDate.Value.Month - 1) / 3 + 1 })
                    .Select(g => new RevenuePeriodDto
                    {
                        Period = $"Q{g.Key.Quarter} {g.Key.Year}",
                        Amount = g.Sum(pt => pt.Amount),
                        TransactionCount = g.Count()
                    })
                    .OrderBy(r => r.Period)
                    .ToList(),

                "year" => transactions
                    .GroupBy(pt => pt.CompletedDate!.Value.Year)
                    .Select(g => new RevenuePeriodDto
                    {
                        Period = g.Key.ToString(),
                        Amount = g.Sum(pt => pt.Amount),
                        TransactionCount = g.Count()
                    })
                    .OrderBy(r => r.Period)
                    .ToList(),

                _ => transactions
                    .GroupBy(pt => new { pt.CompletedDate!.Value.Year, pt.CompletedDate.Value.Month })
                    .Select(g => new RevenuePeriodDto
                    {
                        Period = $"thg {g.Key.Month} {g.Key.Year}",
                        Amount = g.Sum(pt => pt.Amount),
                        TransactionCount = g.Count()
                    })
                    .OrderBy(r => r.Period)
                    .ToList()
            };

            return grouped;
        }

        public async Task<TransactionStatusDistributionDto> GetTransactionStatusDistributionAsync()
        {
            var statusCounts = await _repo.GetTransactionStatusCountsAsync();

            return new TransactionStatusDistributionDto
            {
                Completed = statusCounts.GetValueOrDefault(TransactionStatusEnum.Completed, 0),
                Pending = statusCounts.GetValueOrDefault(TransactionStatusEnum.Pending, 0),
                Failed = statusCounts.GetValueOrDefault(TransactionStatusEnum.Failed, 0)
            };
        }

        public async Task<List<UserGrowthDto>> GetUserGrowthAsync(int months)
        {
            if (months > 24) months = 24;
            if (months < 1) months = 12;

            var endDate = DateTime.UtcNow;
            var startDate = endDate.AddMonths(-months);

            var users = await _repo.GetUsersByDateRangeAsync(startDate, endDate);

            var grouped = users
                .GroupBy(u => new { u.CreatedAt.Year, u.CreatedAt.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Count = g.Count()
                })
                .OrderBy(g => g.Year)
                .ThenBy(g => g.Month)
                .ToList();

            var result = new List<UserGrowthDto>();
            int cumulativeCount = await _repo.GetUsersCountBeforeDateAsync(startDate);

            for (int i = 0; i < months; i++)
            {
                var date = startDate.AddMonths(i);
                var monthData = grouped.FirstOrDefault(g => g.Year == date.Year && g.Month == date.Month);
                
                var newUsers = monthData?.Count ?? 0;
                cumulativeCount += newUsers;

                result.Add(new UserGrowthDto
                {
                    Month = $"Th{date.Month} {date.Year}",
                    UserCount = cumulativeCount,
                    NewUsers = newUsers
                });
            }

            return result;
        }

        public async Task<List<EventStatsDto>> GetEventStatsAsync(int limit, string sortBy)
        {
            var events = await _repo.GetEventsWithDetailsAsync();
            var eventParticipants = await _repo.GetEventParticipantCountsAsync();

            var result = events.Select(e => new EventStatsDto
            {
                EventId = e.Id,
                EventName = e.Name,
                ParticipantCount = eventParticipants.GetValueOrDefault(e.Id, 0),
                StartDate = e.StartDate,
                Status = e.Status
            });

            if (sortBy?.ToLower() == "date")
                result = result.OrderByDescending(e => e.StartDate);
            else
                result = result.OrderByDescending(e => e.ParticipantCount);

            return result.Take(limit).ToList();
        }

        public async Task<List<TournamentStatsDto>> GetTournamentStatsAsync(int limit, string sortBy)
        {
            var tournaments = await _repo.GetTournamentsAsync();
            var tournamentParticipants = await _repo.GetTournamentParticipantCountsAsync();

            var result = tournaments.Select(t => new TournamentStatsDto
            {
                TournamentId = t.Id,
                TournamentName = t.Name,
                ParticipantCount = tournamentParticipants.GetValueOrDefault(t.Id, 0),
                StartDate = t.StartDate,
                EndDate = t.EndDate,
                Status = t.Status
            });

            if (sortBy?.ToLower() == "date")
                result = result.OrderByDescending(t => t.StartDate);
            else
                result = result.OrderByDescending(t => t.ParticipantCount);

            return result.Take(limit).ToList();
        }

        /*public async Task<List<AIUsageDto>> GetAIUsageAsync(string timeRange, string groupBy)
        {
            var endDate = DateTime.UtcNow;
            var startDate = timeRange.ToLower() switch
            {
                "week" => endDate.AddDays(-7),
                "month" => endDate.AddMonths(-1),
                "quarter" => endDate.AddMonths(-3),
                _ => endDate.AddDays(-7)
            };

            var aiQuizSets = await _repo.GetAIQuizSetsByDateRangeAsync(startDate, endDate);

            var grouped = groupBy.ToLower() switch
            {
                "day" => aiQuizSets
                    .GroupBy(qs => qs.CreatedAt.Date)
                    .Select(g => new AIUsageDto
                    {
                        Period = g.Key.ToString("dd/MM/yyyy"),
                        QueryCount = g.Count(),
                        //QuestionCount = g.Sum(qs => qs.Quizzes.Count),
                        SuccessRate = 100.0
                    })
                    .OrderBy(a => a.Period)
                    .ToList(),

                "month" => aiQuizSets
                    .GroupBy(qs => new { qs.CreatedAt.Year, qs.CreatedAt.Month })
                    .Select(g => new AIUsageDto
                    {
                        Period = $"Th{g.Key.Month} {g.Key.Year}",
                        QueryCount = g.Count(),
                        //QuestionCount = g.Sum(qs => qs.Quizzes.Count),
                        SuccessRate = 100.0
                    })
                    .OrderBy(a => a.Period)
                    .ToList(),

                _ => aiQuizSets
                    .GroupBy(qs => GetWeekStart(qs.CreatedAt))
                    .Select(g => new AIUsageDto
                    {
                        Period = $"Tuần {g.Key:dd/MM/yyyy}",
                        QueryCount = g.Count(),
                        //QuestionCount = g.Sum(qs => qs.Quizzes.Count),
                        SuccessRate = 100.0
                    })
                    .OrderBy(a => a.Period)
                    .ToList()
            };

            return grouped;
        }*/

        public async Task<List<SubscriptionDistributionDto>> GetSubscriptionDistributionAsync()
        {
            var activeSubscriptions = await _repo.GetActiveSubscriptionsWithPlansAsync();
            var totalActive = activeSubscriptions.Count;

            var grouped = activeSubscriptions
                .GroupBy(s => new { s.SubscriptionPlanId, s.SubscriptionPlan!.Name })
                .Select(g => new SubscriptionDistributionDto
                {
                    PlanName = g.Key.Name,
                    PlanId = g.Key.SubscriptionPlanId,
                    ActiveCount = g.Count(),
                    Percentage = totalActive > 0 ? (double)g.Count() / totalActive * 100 : 0,
                    TotalRevenue = 0
                })
                .OrderByDescending(s => s.ActiveCount)
                .ToList();

            foreach (var item in grouped)
            {
                var transactions = await _repo.GetTransactionsByPlanIdAsync(item.PlanId);
                item.TotalRevenue = transactions.Sum(pt => pt.Amount);
            }

            return grouped;
        }

        public async Task<QuizSetStatsDto> GetQuizSetStatsAsync()
        {
            var quizSets = await _repo.GetQuizSetsWithQuizzesAsync();

            var total = quizSets.Count;
            var byType = new QuizSetTypeCountDto
            {
                Practice = quizSets.Count(qs => qs.QuizSetType == QuizSetTypeEnum.Practice),
                Placement = quizSets.Count(qs => qs.QuizSetType == QuizSetTypeEnum.Placement),
                Tournament = quizSets.Count(qs => qs.QuizSetType == QuizSetTypeEnum.Tournament),
                Event = quizSets.Count(qs => qs.QuizSetType == QuizSetTypeEnum.Event)
            };

            var published = quizSets.Count(qs => qs.IsPublished);
            var draft = total - published;
            //var aiGenerated = quizSets.Count(qs => qs.IsAIGenerated);
            //var manuallyCreated = total - aiGenerated;

            //var totalQuestions = quizSets.Sum(qs => qs.Quizzes.Count);
            //var averageQuestions = total > 0 ? (double)totalQuestions / total : 0;

            return new QuizSetStatsDto
            {
                Total = total,
                ByType = byType,
                Published = published,
                Draft = draft,
                //AIGenerated = aiGenerated,
                //ManuallyCreated = manuallyCreated,
                //AverageQuestionsPerSet = Math.Round(averageQuestions, 1)
            };
        }

        public async Task<ActivitySummaryDto> GetActivitySummaryAsync(int days)
        {
            if (days > 90) days = 90;
            if (days < 1) days = 7;

            var startDate = DateTime.UtcNow.AddDays(-days);

            var newUsers = (await _repo.GetUsersByDateRangeForActivityAsync(startDate)).Count;

            var quizAttempts = await _repo.GetQuizAttemptsByDateRangeAsync(startDate);
            var totalQuizAttempts = quizAttempts.Count;
            var completedQuizzes = quizAttempts.Count(qa => qa.IsCompleted);

            var newEvents = (await _repo.GetEventsByDateRangeAsync(startDate)).Count;
            var newTournaments = (await _repo.GetTournamentsByDateRangeAsync(startDate)).Count;

            var transactions = await _repo.GetTransactionsByDateRangeAsync(startDate);
            var transactionSummary = new TransactionSummaryDto
            {
                Total = transactions.Count,
                Completed = transactions.Count(t => t.Status == TransactionStatusEnum.Completed),
                Revenue = transactions
                    .Where(t => t.Status == TransactionStatusEnum.Completed)
                    .Sum(t => t.Amount)
            };

            //var aiGenerations = await _repo.GetAIQuizSetsCountByDateRangeAsync(startDate);

            return new ActivitySummaryDto
            {
                Period = $"Last {days} days",
                NewUsers = newUsers,
                TotalQuizAttempts = totalQuizAttempts,
                CompletedQuizzes = completedQuizzes,
                NewEvents = newEvents,
                NewTournaments = newTournaments,
                Transactions = transactionSummary,
                //AIGenerations = aiGenerations
            };
        }

        private DateTime GetWeekStart(DateTime date)
        {
            var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
            return date.AddDays(-1 * diff).Date;
        }

        private string FormatWeek(DateTime weekStart)
        {
            return $"Tuần {weekStart:dd/MM/yyyy}";
        }
    }
}
