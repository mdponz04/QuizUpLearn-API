using AutoMapper;
using BusinessLogic.DTOs.DashboardDtos;
using BusinessLogic.Interfaces;
using Microsoft.Extensions.Logging;
using Repository.Entities;
using Repository.Interfaces;

namespace BusinessLogic.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly IQuizAttemptRepo _quizAttemptRepo;
        private readonly IQuizRepo _quizRepo;
        private readonly IQuizSetRepo _quizSetRepo;
        private readonly IAccountRepo _accountRepo;
        private readonly IUserRepo _userRepo;
        private readonly IMapper _mapper;
        private readonly ILogger<DashboardService> _logger;

        public DashboardService(
            IQuizAttemptRepo quizAttemptRepo,
            IQuizRepo quizRepo,
            IQuizSetRepo quizSetRepo,
            IAccountRepo accountRepo,
            IUserRepo userRepo,
            IMapper mapper,
            ILogger<DashboardService> logger)
        {
            _quizAttemptRepo = quizAttemptRepo;
            _quizRepo = quizRepo;
            _quizSetRepo = quizSetRepo;
            _accountRepo = accountRepo;
            _userRepo = userRepo;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<DashboardResponseDto> GetDashboardDataAsync(Guid userId)
        {
            try
            {
                var stats = await GetUserStatsAsync(userId);
                var progress = await GetUserProgressAsync(userId);
                var recentActivities = await GetRecentActivitiesAsync(userId);
                var eventParticipations = await GetEventParticipationsAsync(userId);
                var recentQuizHistory = await GetRecentQuizHistoryAsync(userId);
                var weakPoints = await GetUserWeakPointsAsync(userId);

                return new DashboardResponseDto
                {
                    Stats = stats,
                    Progress = progress,
                    RecentActivities = recentActivities,
                    EventParticipations = eventParticipations,
                    RecentQuizHistory = recentQuizHistory,
                    WeakPoints = weakPoints,
                    LastUpdated = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard data for user {UserId}", userId);
                throw;
            }
        }

        public async Task<DashboardStatsDto> GetUserStatsAsync(Guid userId)
        {
            try
            {
                var attempts = await GetUserAttemptsAsync(userId);
                var totalQuizzes = attempts.Count;
                
                var totalQuestions = attempts.Sum(a => a.TotalQuestions);
                var totalCorrect = attempts.Sum(a => a.CorrectAnswers);
                var totalWrong = totalQuestions - totalCorrect;
                
                var accuracyRate = totalQuestions > 0 ? (double)totalCorrect / totalQuestions * 100 : 0;
                
                // Calculate current streak (consecutive days with quiz attempts)
                var currentStreak = CalculateCurrentStreak(attempts);
                
                // Get user rank (simplified - would need more complex logic in real implementation)
                var currentRank = await GetUserRankAsync(userId);
                var totalPoints = await GetUserTotalPointsAsync(userId);

                return new DashboardStatsDto
                {
                    TotalQuizzes = totalQuizzes,
                    AccuracyRate = Math.Round(accuracyRate, 1),
                    CurrentStreak = currentStreak,
                    CurrentRank = currentRank,
                    TotalPoints = totalPoints,
                    TotalCorrectAnswers = totalCorrect,
                    TotalWrongAnswers = totalWrong,
                    TotalQuestions = totalQuestions
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user stats for user {UserId}", userId);
                throw;
            }
        }

        public async Task<ProgressChartDto> GetUserProgressAsync(Guid userId, int days = 7)
        {
            try
            {
                var attempts = await GetUserAttemptsAsync(userId);
                var recentAttempts = attempts
                    .Where(a => a.CreatedAt >= DateTime.UtcNow.AddDays(-days))
                    .OrderBy(a => a.CreatedAt)
                    .ToList();

                var weeklyProgress = new List<ProgressDataDto>();
                var startDate = DateTime.UtcNow.AddDays(-days);

                for (int i = 0; i < days; i++)
                {
                    var date = startDate.AddDays(i);
                    var dayAttempts = recentAttempts.Where(a => a.CreatedAt.Date == date.Date).ToList();
                    
                    var dayScore = dayAttempts.Any() 
                        ? dayAttempts.Average(a => (double)a.Accuracy) 
                        : 0;

                    weeklyProgress.Add(new ProgressDataDto
                    {
                        Day = date.ToString("ddd"),
                        ScorePercentage = Math.Round(dayScore, 1),
                        Date = date
                    });
                }

                var totalQuestions = attempts.Sum(a => a.TotalQuestions);
                var totalCorrect = attempts.Sum(a => a.CorrectAnswers);
                var totalWrong = totalQuestions - totalCorrect;
                var overallAccuracy = totalQuestions > 0 ? (double)totalCorrect / totalQuestions * 100 : 0;

                return new ProgressChartDto
                {
                    WeeklyProgress = weeklyProgress,
                    OverallAccuracy = Math.Round(overallAccuracy, 1),
                    TotalCorrectAnswers = totalCorrect,
                    TotalWrongAnswers = totalWrong,
                    TotalQuestions = totalQuestions
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user progress for user {UserId}", userId);
                throw;
            }
        }

        public async Task<List<RecentActivityDto>> GetRecentActivitiesAsync(Guid userId, int count = 10)
        {
            try
            {
                var attempts = await GetUserAttemptsAsync(userId);
                var recentAttempts = attempts
                    .OrderByDescending(a => a.CreatedAt)
                    .Take(count)
                    .ToList();

                var activities = new List<RecentActivityDto>();

                foreach (var attempt in recentAttempts)
                {
                    // Get quiz from quiz set
                    var quizSet = await _quizSetRepo.GetQuizSetByIdAsync(attempt.QuizSetId);
                    if (quizSet != null)
                    {
                        activities.Add(new RecentActivityDto
                        {
                            Id = attempt.Id,
                            ActivityType = "QuizCompleted",
                            Description = $"Completed {quizSet.Title} Quiz",
                            Timestamp = attempt.CreatedAt,
                            Metadata = new Dictionary<string, object>
                            {
                                { "QuizSetId", quizSet.Id },
                                { "Score", attempt.Score },
                                { "Accuracy", attempt.Accuracy },
                                { "Status", attempt.Accuracy >= 70 ? "Passed" : "Failed" }
                            }
                        });
                    }
                }

                return activities.OrderByDescending(a => a.Timestamp).Take(count).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent activities for user {UserId}", userId);
                throw;
            }
        }

        public async Task<List<EventParticipationDto>> GetEventParticipationsAsync(Guid userId, int count = 5)
        {
            try
            {
                // This would typically come from an events/competitions table
                // For now, returning mock data based on quiz attempts
                var attempts = await GetUserAttemptsAsync(userId);
                var recentAttempts = attempts
                    .OrderByDescending(a => a.CreatedAt)
                    .Take(count)
                    .ToList();

                var events = new List<EventParticipationDto>();

                foreach (var attempt in recentAttempts)
                {
                    var quizSet = await _quizSetRepo.GetQuizSetByIdAsync(attempt.QuizSetId);
                    if (quizSet != null)
                    {
                        events.Add(new EventParticipationDto
                        {
                            Id = attempt.Id,
                            EventName = $"{quizSet.Title} Challenge",
                            EventType = "Challenge",
                            Rank = $"#{new Random().Next(1, 50)}", // Mock rank
                            EventDate = attempt.CreatedAt,
                            PointsEarned = (int)(attempt.Accuracy * 2), // Mock points
                            Status = "Completed"
                        });
                    }
                }

                return events.OrderByDescending(e => e.EventDate).Take(count).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting event participations for user {UserId}", userId);
                throw;
            }
        }

        public async Task<List<QuizHistoryDto>> GetRecentQuizHistoryAsync(Guid userId, int count = 10)
        {
            try
            {
                var attempts = await GetUserAttemptsAsync(userId);
                var recentAttempts = attempts
                    .OrderByDescending(a => a.CreatedAt)
                    .Take(count)
                    .ToList();

                var quizHistory = new List<QuizHistoryDto>();

                foreach (var attempt in recentAttempts)
                {
                    var quizSet = await _quizSetRepo.GetQuizSetByIdAsync(attempt.QuizSetId);
                    if (quizSet != null)
                    {
                        quizHistory.Add(new QuizHistoryDto
                        {
                            Id = attempt.Id,
                            QuizName = quizSet.Title,
                            Category = quizSet.Description ?? "General",
                            CompletedAt = attempt.CreatedAt,
                            ScorePercentage = (double)attempt.Accuracy,
                            Status = attempt.Accuracy >= 70 ? "Passed" : "Failed",
                            TimeSpent = attempt.TimeSpent ?? 0,
                            TotalQuestions = attempt.TotalQuestions,
                            CorrectAnswers = attempt.CorrectAnswers
                        });
                    }
                }

                return quizHistory;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent quiz history for user {UserId}", userId);
                throw;
            }
        }

        public async Task<List<WeakPointDto>> GetUserWeakPointsAsync(Guid userId, int count = 5)
        {
            try
            {
                var attempts = await GetUserAttemptsAsync(userId);
                var weakTopics = attempts
                    .Where(a => a.Accuracy < 70)
                    .GroupBy(a => a.QuizSetId)
                    .Select(g => new
                    {
                        QuizSetId = g.Key,
                        MistakesCount = g.Count(),
                        AverageScore = g.Average(a => a.Accuracy)
                    })
                    .OrderByDescending(x => x.MistakesCount)
                    .Take(count)
                    .ToList();

                var weakPoints = new List<WeakPointDto>();

                foreach (var weakTopic in weakTopics)
                {
                    var quizSet = await _quizSetRepo.GetQuizSetByIdAsync(weakTopic.QuizSetId);
                    if (quizSet != null)
                    {
                        weakPoints.Add(new WeakPointDto
                        {
                            Topic = quizSet.Title,
                            Category = quizSet.Description ?? "General",
                            MistakesCount = weakTopic.MistakesCount,
                            AccuracyRate = Math.Round((double)weakTopic.AverageScore, 1),
                            CommonMistakes = new List<string> { "Incorrect answer selection", "Time management" },
                            DifficultyLevel = "Intermediate"
                        });
                    }
                }

                return weakPoints;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user weak points for user {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> RecordActivityAsync(Guid userId, string activityType, string description, Dictionary<string, object>? metadata = null)
        {
            try
            {
                // This would typically save to an activities/audit log table
                _logger.LogInformation("User {UserId} performed activity: {ActivityType} - {Description}", 
                    userId, activityType, description);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording activity for user {UserId}", userId);
                return false;
            }
        }

        private int CalculateCurrentStreak(List<Repository.Entities.QuizAttempt> attempts)
        {
            if (!attempts.Any()) return 0;

            var sortedAttempts = attempts
                .OrderByDescending(a => a.CreatedAt)
                .Select(a => a.CreatedAt.Date)
                .Distinct()
                .ToList();

            int streak = 0;
            var currentDate = DateTime.UtcNow.Date;

            foreach (var attemptDate in sortedAttempts)
            {
                if (attemptDate == currentDate || attemptDate == currentDate.AddDays(-streak))
                {
                    streak++;
                    currentDate = attemptDate.AddDays(-1);
                }
                else
                {
                    break;
                }
            }

            return streak;
        }

        private async Task<int> GetUserRankAsync(Guid userId)
        {
            try
            {
                // Simplified ranking logic - in real implementation, this would be more complex
                var allUsers = await _accountRepo.GetAllAsync();
                var userAttempts = await GetUserAttemptsAsync(userId);
                var userAverageScore = userAttempts.Any() ? userAttempts.Average(a => a.Accuracy) : 0;

                var userRank = 1;
                foreach (var user in allUsers)
                {
                    if (user.Id == userId) continue;
                    
                    var otherUserAttempts = await GetUserAttemptsAsync(user.Id);
                    var otherUserAverageScore = otherUserAttempts.Any() ? otherUserAttempts.Average(a => a.Accuracy) : 0;
                    
                    if (otherUserAverageScore > userAverageScore)
                    {
                        userRank++;
                    }
                }

                return userRank;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating user rank for user {UserId}", userId);
                return 999; // Default rank if calculation fails
            }
        }

        private async Task<int> GetUserTotalPointsAsync(Guid userId)
        {
            try
            {
                var user = await _userRepo.GetByIdAsync(userId);
                return user?.TotalPoints ?? 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading total points for user {UserId}", userId);
                return 0;
            }
        }
        private async Task<List<QuizAttempt>> GetUserAttemptsAsync(Guid userId)
        {
            try
            {
                var attempts = await _quizAttemptRepo.GetByUserIdAsync(userId);
                return attempts?.ToList() ?? new List<QuizAttempt>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading attempts for user {UserId}", userId);
                return new List<QuizAttempt>();
            }
        }
    }
}
