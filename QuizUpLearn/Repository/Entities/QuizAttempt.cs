using Repository.Entities.BaseModelEntity;

namespace Repository.Entities
{
    public class QuizAttempt : BaseEntity
    {
        public Guid UserId { get; set; }
        public Guid QuizSetId { get; set; }
        public string AttemptType { get; set; } = string.Empty;
        public int TotalQuestions { get; set; }
        public int CorrectAnswers { get; set; } = 0;
        public int WrongAnswers { get; set; } = 0;
        public int Score { get; set; } = 0;
        public decimal Accuracy { get; set; } = 0;
        public bool IsCompleted { get; set; } = false;
        public int? TimeSpent { get; set; }

        public Guid? OpponentId { get; set; }
        public bool? IsWinner { get; set; }
        public string Status { get; set; } = string.Empty;

        public virtual User? User { get; set; }
        public virtual QuizSet? QuizSet { get; set; }
        public virtual ICollection<QuizAttemptDetail> QuizAttemptDetails { get; set; } = new List<QuizAttemptDetail>();
    }
}
