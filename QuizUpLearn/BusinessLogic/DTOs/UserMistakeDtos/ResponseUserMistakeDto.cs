using BusinessLogic.DTOs.QuizDtos;
using Repository.Entities;

namespace BusinessLogic.DTOs.UserMistakeDtos
{
    public class ResponseUserMistakeDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid QuizId { get; set; }
        public Guid? UserWeakPointId { get; set; }
        public int TimesAttempted { get; set; }
        public int TimesWrong { get; set; }
        public DateTime LastAttemptedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsAnalyzed { get; set; }
        public string? UserAnswer { get; set; }
        public QuizResponseDto? QuizDto { get; set; }
        public virtual UserWeakPoint? UserWeakPoint { get; set; }
    }
}
