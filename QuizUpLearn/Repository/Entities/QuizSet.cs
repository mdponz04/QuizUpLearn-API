using Repository.Entities.BaseModelEntity;
using Repository.Enums;

namespace Repository.Entities
{
    public class QuizSet : BaseEntity
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public QuizSetTypeEnum QuizType { get; set; } = QuizSetTypeEnum.Practice;
        public string DifficultyLevel { get; set; } = string.Empty;
        public Guid CreatedBy { get; set; }
        public bool IsAIGenerated { get; set; } = true;
        public bool IsPublished { get; set; } = false;
        public bool IsPremiumOnly { get; set; } = false;
        public int TotalAttempts { get; set; } = 0;
        public decimal AverageScore { get; set; } = 0;

        // Navigation
        public virtual User? Creator { get; set; }
        public virtual ICollection<Quiz> Quizzes { get; set; } = new List<Quiz>();
        public virtual ICollection<QuizAttempt> QuizAttempts { get; set; } = new List<QuizAttempt>();
        public virtual ICollection<QuizGroupItem> QuizGroupItems { get; set; } = new List<QuizGroupItem>();
    }
}
