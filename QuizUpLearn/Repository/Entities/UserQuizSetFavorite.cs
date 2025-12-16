using Repository.Entities.BaseModelEntity;

namespace Repository.Entities
{
    public class UserQuizSetFavorite : BaseEntity
    {
        public Guid UserId { get; set; }
        public Guid QuizSetId { get; set; }
        public virtual User? User { get; set; }
        public virtual QuizSet? QuizSet { get; set; }
    }
}
