namespace BusinessLogic.DTOs.BadgeDtos
{
    public class RequestUserBadgeDto
    {
        public required Guid UserId { get; set; }
        public Guid? BadgeId { get; set; }
        public string? BadgeCode { get; set; }
    }
}

