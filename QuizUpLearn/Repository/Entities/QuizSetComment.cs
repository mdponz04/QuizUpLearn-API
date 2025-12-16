using Repository.Entities.BaseModelEntity;

namespace Repository.Entities
{
    public class QuizSetComment : BaseEntity
    {
        public Guid UserId { get; set; }
        public Guid QuizSetId { get; set; }
        public string Content { get; set; } = string.Empty;
        public virtual User? User { get; set; }
        public virtual QuizSet? QuizSet { get; set; }
    }
}
