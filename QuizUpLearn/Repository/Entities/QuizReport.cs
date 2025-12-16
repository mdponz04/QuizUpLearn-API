using Repository.Entities.BaseModelEntity;

namespace Repository.Entities
{
    public class QuizReport : BaseEntity
    {
        public Guid UserId { get; set; }
        public Guid QuizId { get; set; }
        public string Description { get; set; } = string.Empty;
        public virtual User? User { get; set; }
        public virtual Quiz? Quiz { get; set; }
    }
}
