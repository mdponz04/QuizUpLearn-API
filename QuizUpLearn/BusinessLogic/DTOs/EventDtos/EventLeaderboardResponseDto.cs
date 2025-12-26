namespace BusinessLogic.DTOs.EventDtos
{
    /// <summary>
    /// Response DTO cho Event Leaderboard
    /// </summary>
    public class EventLeaderboardResponseDto
    {
        public Guid EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public string EventStatus { get; set; } = string.Empty;
        public int TotalParticipants { get; set; }
        public DateTime? EventStartDate { get; set; }
        public DateTime? EventEndDate { get; set; }
        public List<EventLeaderboardItemDto> Rankings { get; set; } = new();
        public EventLeaderboardItemDto? TopPlayer { get; set; }
        public DateTime GeneratedAt { get; set; }
    }

    /// <summary>
    /// Item trong leaderboard - thông tin từng participant
    /// </summary>
    public class EventLeaderboardItemDto
    {
        public long Rank { get; set; }
        public Guid ParticipantId { get; set; }
        public string ParticipantName { get; set; } = string.Empty;
        // Alias cho FE dùng chung field tên player giữa nhiều màn hình
        public string PlayerName
        {
            get => ParticipantName;
            set => ParticipantName = value;
        }
        public string? AvatarUrl { get; set; }
        public long Score { get; set; }
        public double Accuracy { get; set; }
        public DateTime JoinAt { get; set; }
        public DateTime? FinishAt { get; set; }
        public bool IsTopThree { get; set; }
        public string Badge { get; set; } = string.Empty; // 🥇, 🥈, 🥉
    }
}

