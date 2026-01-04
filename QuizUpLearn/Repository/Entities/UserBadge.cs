using Repository.Entities.BaseModelEntity;

namespace Repository.Entities
{
    public class UserBadge : BaseEntity
    {
        public Guid UserId { get; set; }
        public Guid BadgeId { get; set; }
        public virtual User? User { get; set; }
        public virtual Badge? Badge { get; set; }
    }
}
