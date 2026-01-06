namespace BusinessLogic.DTOs.EventDtos
{
    /// <summary>
    /// Response khi start event thành công
    /// Chứa thông tin GamePin để participants có thể join
    /// </summary>
    public class StartEventResponseDto
    {
        public Guid EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public string GamePin { get; set; } = string.Empty;
        public Guid GameSessionId { get; set; }
        public DateTime StartedAt { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}

