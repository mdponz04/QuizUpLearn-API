namespace BusinessLogic.DTOs.DashboardDtos
{
    public class DashboardStatsDto
    {
        public int TotalQuizzes { get; set; }
        public double AccuracyRate { get; set; }
        public int CurrentStreak { get; set; }
        public int CurrentRank { get; set; }
        public int TotalPoints { get; set; }
        public int TotalCorrectAnswers { get; set; }
        public int TotalWrongAnswers { get; set; }
        public int TotalQuestions { get; set; }
    }
}
