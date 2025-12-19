using Repository.Entities.BaseModelEntity;

namespace Repository.Entities
{
    public class Quiz : BaseEntity
    {
        public Guid? QuizGroupItemId { get; set; }
        public Guid? VocabularyId { get; set; }
        public Guid? GrammarId { get; set; }
        public required string QuestionText { get; set; }
        public string? CorrectAnswer { get; set; }
        public string? AudioURL { get; set; }
        public string? ImageURL { get; set; }
        public required string TOEICPart { get; set; }
        public int TimesAnswered { get; set; } = 0;
        public int TimesCorrect { get; set; } = 0;
        public int? OrderIndex { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsAIGenerated { get; set; } = true;
        public string DifficultyLevel { get; set; } = string.Empty;

        // Navigation
        public virtual Vocabulary? Vocabulary { get; set; }
        public virtual Grammar? Grammar { get; set; }
        public virtual QuizGroupItem? QuizGroupItem { get; set; }
        public virtual ICollection<QuizQuizSet> QuizQuizSets { get; set; } = new List<QuizQuizSet>();
        public virtual ICollection<AnswerOption> AnswerOptions { get; set; } = new List<AnswerOption>();
        public virtual ICollection<QuizAttemptDetail> QuizAttemptDetails { get; set; } = new List<QuizAttemptDetail>();
        public virtual ICollection<UserMistake> UserMistakes { get; set; } = new List<UserMistake>();
    }
}
