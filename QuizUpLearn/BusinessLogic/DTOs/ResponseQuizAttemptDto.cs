namespace BusinessLogic.DTOs
{
    public class ResponseQuizAttemptDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid QuizSetId { get; set; }
        public string? QuizSetName { get; set; }
        public string AttemptType { get; set; }
        public int TotalQuestions { get; set; }
        public int CorrectAnswers { get; set; }
        public int WrongAnswers { get; set; }
        public int Score { get; set; }
        public decimal Accuracy { get; set; }
        public int? TimeSpent { get; set; }
        public Guid? OpponentId { get; set; }
        public bool? IsWinner { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
