using Repository.Enums;

namespace BusinessLogic.DTOs.NotificationDtos
{
    public class NotificationResponseDto
    {
        public Guid Id { get; set; }
        public required string Title { get; set; }
        public required string Message { get; set; }
        public NotificationType Type { get; set; }
        public string? ActionUrl { get; set; }
        public string? ImageUrl { get; set; }
        public string? Metadata { get; set; }
        public DateTime? ScheduledAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
