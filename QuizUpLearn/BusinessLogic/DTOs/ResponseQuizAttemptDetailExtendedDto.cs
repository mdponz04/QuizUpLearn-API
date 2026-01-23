namespace BusinessLogic.DTOs
{
    public class ResponseQuizAttemptDetailExtendedDto
    {
        public Guid Id { get; set; }
        public Guid AttemptId { get; set; }
        public Guid QuestionId { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public string UserAnswer { get; set; } = string.Empty;
        public string? UserAnswerText { get; set; }
        public bool? IsCorrect { get; set; }
        public int? TimeSpent { get; set; }
        public int? OrderIndex { get; set; } // Thứ tự câu hỏi (1, 2, 4, 5...)
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
        
        public string? QuizSetName { get; set; } 
        public string? AudioURL { get; set; }
        public string? ImageURL { get; set; }
        public Guid? QuizGroupItemId { get; set; }
    }
}

