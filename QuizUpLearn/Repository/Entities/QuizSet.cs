using Repository.Entities.BaseModelEntity;

namespace Repository.Entities
{
    public class QuizSet : BaseEntity
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string QuizType { get; set; }
        public string DifficultyLevel { get; set; }
        public Guid CreatedBy { get; set; }
        public bool IsAIGenerated { get; set; } = true;
        public bool IsPublished { get; set; } = false;
        public bool IsPremiumOnly { get; set; } = false;
        public Dictionary<string, string>? GroupItems { get; set; } = new Dictionary<string, string>(); //Map<GroupId, AudioUrl/ImageUrl/Passage>
        public int TotalAttempts { get; set; } = 0;
        public decimal AverageScore { get; set; } = 0;

        // Navigation
        public virtual User? Creator { get; set; }
        public virtual ICollection<Quiz> Quizzes { get; set; } = new List<Quiz>();
        public virtual ICollection<QuizAttempt> QuizAttempts { get; set; } = new List<QuizAttempt>();
    }
}
