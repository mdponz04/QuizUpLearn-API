using Repository.Entities.BaseModelEntity;
using Repository.Enums;

namespace Repository.Entities
{
    public class Notification : BaseEntity
    {
        public Guid UserId { get; set; }
        public required string Title { get; set; }
        public required string Message { get; set; }
        public NotificationType Type { get; set; }
        public bool IsRead { get; set; } = false;
        public DateTime? ReadAt { get; set; }
        public string? ActionUrl { get; set; }
        public string? ImageUrl { get; set; }
        public string? Metadata { get; set; } // JSON data for additional context
        public DateTime? ScheduledAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        
        // Navigation properties
        public virtual User? User { get; set; }
    }
}