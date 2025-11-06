using Repository.Entities.BaseModelEntity;

namespace Repository.Entities
{
    public class Quiz : BaseEntity
    {
        public Guid QuizSetId { get; set; }
        public Guid? QuizGroupItemId { get; set; }
        public required string QuestionText { get; set; }
        public string? CorrectAnswer { get; set; }
        public string? AudioURL { get; set; }
        public string? ImageURL { get; set; }
        public required string TOEICPart { get; set; }
        public int TimesAnswered { get; set; } = 0;
        public int TimesCorrect { get; set; } = 0;
        public int? OrderIndex { get; set; }

        public bool IsActive { get; set; } = true;
        
        // Navigation
        public virtual QuizGroupItem? QuizGroupItem { get; set; }
        public virtual QuizSet? QuizSet { get; set; }
        public virtual ICollection<AnswerOption> AnswerOptions { get; set; } = new List<AnswerOption>();
        public virtual ICollection<QuizAttemptDetail> QuizAttemptDetails { get; set; } = new List<QuizAttemptDetail>();
        public virtual ICollection<UserMistake> UserMistakes { get; set; } = new List<UserMistake>();
    }
}
