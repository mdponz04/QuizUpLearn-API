using Repository.Entities.BaseModelEntity;

namespace Repository.Entities
{
    public class QuizQuizSet : BaseEntity
    {
        public Guid QuizId { get; set; }
        public Guid QuizSetId { get; set; }
        // Navigation
        public virtual Quiz? Quiz { get; set; }
        public virtual QuizSet? QuizSet { get; set; }
    }
}
