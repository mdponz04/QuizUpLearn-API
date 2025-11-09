using Repository.Entities.BaseModelEntity;

namespace Repository.Entities
{
    public class TournamentQuizSet : BaseEntity
    {
        public Guid TournamentId { get; set; }
        public Guid QuizSetId { get; set; }
        public DateTime UnlockDate{ get; set; }
        public bool IsActive { get; set; } = true;
        public int DateNumber { get; set; } = 0;
        public virtual Tournament? Tournament { get; set; }
        public virtual QuizSet? QuizSet { get; set; }
    }
}
