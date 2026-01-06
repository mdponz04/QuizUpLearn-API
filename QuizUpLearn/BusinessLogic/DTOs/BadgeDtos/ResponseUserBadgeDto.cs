namespace BusinessLogic.DTOs.BadgeDtos
{
    public class ResponseUserBadgeDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid BadgeId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
        public ResponseBadgeDto? Badge { get; set; }
        public ResponseUserDto? User { get; set; }
    }
}

