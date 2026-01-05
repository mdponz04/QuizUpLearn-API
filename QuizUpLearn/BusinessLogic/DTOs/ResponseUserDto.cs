using BusinessLogic.DTOs.BadgeDtos;

namespace BusinessLogic.DTOs
{
    public class ResponseUserDto
    {
        public Guid Id { get; set; }
        public Guid AccountId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
        public string? Bio { get; set; }
        public int LoginStreak { get; set; }
        public DateTime? LastLoginDate { get; set; }
        public int TotalPoints { get; set; }
        public string? PreferredLanguage { get; set; }
        public string? Timezone { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
        public List<BadgeDtos.ResponseBadgeDto>? EarnedBadges { get; set; }
    }
}
