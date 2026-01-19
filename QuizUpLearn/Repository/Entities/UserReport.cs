using Repository.Entities.BaseModelEntity;

namespace Repository.Entities
{
    public class UserReport : BaseEntity
    {
        public Guid UserId { get; set; }
        public Guid? CommentId { get; set; }
        public string Reason { get; set; } = null!;
        public virtual User? User { get; set; }
    }
}
