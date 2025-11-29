namespace BusinessLogic.DTOs.UserWeakPointDtos
{
    public class ResponseUserWeakPointDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid? QuizSetId { get; set; }
        public string WeakPoint { get; set; } = string.Empty;
        public string? ToeicPart { get; set; }
        public string? DifficultyLevel { get; set; }
        public string? Advice { get; set; }
        public bool IsDone { get; set; }
        public DateTime? CompleteAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
