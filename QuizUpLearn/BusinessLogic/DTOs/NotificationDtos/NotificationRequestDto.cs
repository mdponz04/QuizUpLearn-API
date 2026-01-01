using Repository.Enums;

namespace BusinessLogic.DTOs.NotificationDtos
{
    public class NotificationRequestDto
    {
        public required string Title { get; set; }
        public required string Message { get; set; }
        public NotificationType Type { get; set; }
    }
}
