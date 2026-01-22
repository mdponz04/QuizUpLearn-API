namespace BusinessLogic.DTOs
{
    public class ResponseQuizAttemptDetailDto
    {
        public Guid Id { get; set; }
        public Guid AttemptId { get; set; }
        public Guid QuestionId { get; set; }
        public string UserAnswer { get; set; }
        public bool? IsCorrect { get; set; }
        public int? TimeSpent { get; set; }
        public int? OrderIndex { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
