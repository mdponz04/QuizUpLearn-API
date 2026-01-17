using Repository.Entities.BaseModelEntity;

namespace Repository.Entities
{
    public class User : BaseEntity
    {
        public Guid AccountId { get; set; }
        public required string Username { get; set; }
        public string FullName { get; set; } = string.Empty;
        public required string AvatarUrl { get; set; }
        public string? Bio { get; set; }
        public int LoginStreak { get; set; }
        public DateTime? LastLoginDate { get; set; }
        public int TotalPoints { get; set; }
        public string? PreferredLanguage { get; set; }
        public string? Timezone { get; set; }

        // Navigation
        public virtual Account? Account { get; set; }
        public virtual ICollection<QuizSet> CreatedQuizSets { get; set; } = new List<QuizSet>();
        public virtual ICollection<QuizAttempt> QuizAttempts { get; set; } = new List<QuizAttempt>();
        public virtual ICollection<UserMistake> UserMistakes { get; set; } = new List<UserMistake>();
        public virtual ICollection<UserWeakPoint> UserWeakPoints { get; set; } = new List<UserWeakPoint>();
        public virtual ICollection<UserBadge> UserBadges { get; set; } = new List<UserBadge>();
        public virtual ICollection<UserReport> UserReports { get; set; } = new List<UserReport>();
    }
}
