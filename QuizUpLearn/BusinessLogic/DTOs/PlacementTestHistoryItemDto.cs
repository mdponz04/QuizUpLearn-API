namespace BusinessLogic.DTOs
{
    public class PlacementTestHistoryItemDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid QuizSetId { get; set; }
        public string AttemptType { get; set; } = string.Empty;
        public int TotalQuestions { get; set; }
        public int CorrectAnswers { get; set; }
        public int WrongAnswers { get; set; }
        public int Score { get; set; }
        public decimal Accuracy { get; set; }
        public int? TimeSpent { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        
        public int LisPoint { get; set; }
        public int TotalCorrectLisAns { get; set; }
        public int ReaPoint { get; set; }
        public int TotalCorrectReaAns { get; set; }
    }
}

