namespace BusinessLogic.DTOs.UserNotificationDtos
{
    public class UserNotificationRequestDto
    {
        public Guid UserId { get; set; }
        public Guid NotificationId { get; set; }
        public bool IsRead { get; set; } = false;
        public DateTime? ReadAt { get; set; }
    }
}
