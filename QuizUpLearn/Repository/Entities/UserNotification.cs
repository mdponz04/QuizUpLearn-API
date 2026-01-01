using Repository.Entities.BaseModelEntity;

namespace Repository.Entities
{
    public class UserNotification : BaseEntity
    {
        public Guid UserId { get; set; }
        public Guid NotificationId { get; set; }
        public bool IsRead { get; set; } = false;
        public DateTime? ReadAt { get; set; }
        public virtual Notification? Notification { get; set; }
        public virtual User? User { get; set; }
    }
}
