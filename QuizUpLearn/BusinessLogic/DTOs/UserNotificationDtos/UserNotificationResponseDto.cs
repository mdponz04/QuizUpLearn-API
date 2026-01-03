using BusinessLogic.DTOs.NotificationDtos;

namespace BusinessLogic.DTOs.UserNotificationDtos
{
    public class UserNotificationResponseDto
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public Guid NotificationId { get; set; }
        public bool IsRead { get; set; } = false;
        public DateTime? ReadAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
        public NotificationResponseDto? Notification { get; set; }
        public ResponseUserDto? User { get; set; }
    }
}
