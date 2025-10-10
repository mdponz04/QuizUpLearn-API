namespace BusinessLogic.DTOs
{
    public class PlayerStatsDto
    {
        public Guid UserId { get; set; }
        public int TotalAttempts { get; set; }
        public int CompletedAttempts { get; set; }
        public int InProgressAttempts { get; set; }
        public decimal AverageScore { get; set; }
        public decimal AverageAccuracy { get; set; }
        public int BestScore { get; set; }
        public decimal BestAccuracy { get; set; }
        public int TotalQuestionsAnswered { get; set; }
        public int TotalCorrectAnswers { get; set; }
        public TimeSpan? TotalTimeSpent { get; set; }
        public DateTime? LastPlayedAt { get; set; }
    }
}
