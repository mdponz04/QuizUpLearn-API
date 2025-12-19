using Repository.Entities.BaseModelEntity;
using Repository.Enums;

namespace Repository.Entities
{
    public class QuizSet : BaseEntity
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public QuizSetTypeEnum QuizSetType { get; set; }
        public Guid CreatedBy { get; set; }
        public bool IsPublished { get; set; } = false;
        public bool IsPremiumOnly { get; set; } = false;
        public int TotalAttempts { get; set; } = 0;
        public decimal AverageScore { get; set; } = 0;
        public bool IsRequireValidate { get; set; } = false;
        public DateTime? ValidatedAt { get; set; }

        // Navigation
        public virtual User? Creator { get; set; }
        public virtual ICollection<QuizQuizSet> QuizQuizSets { get; set; } = new List<QuizQuizSet>();
        public virtual ICollection<QuizAttempt> QuizAttempts { get; set; } = new List<QuizAttempt>();
        public virtual ICollection<TournamentQuizSet> TournamentQuizSets { get; set; } = new List<TournamentQuizSet>();
        public virtual ICollection<Event> Events { get; set; } = new List<Event>();
    }
}
