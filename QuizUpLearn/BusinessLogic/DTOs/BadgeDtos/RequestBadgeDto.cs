using Repository.Enums;

namespace BusinessLogic.DTOs.BadgeDtos
{
    public class RequestBadgeDto
    {
        public string? Code { get; set; }
        public required string Name { get; set; }
        public required string Description { get; set; }
        public BadgeTypeEnum Type { get; set; }
    }
}

