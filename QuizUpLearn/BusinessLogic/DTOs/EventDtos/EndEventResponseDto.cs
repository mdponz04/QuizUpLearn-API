namespace BusinessLogic.DTOs.EventDtos
{
    /// <summary>
    /// Response khi end event thành công
    /// </summary>
    public class EndEventResponseDto
    {
        public Guid EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime EndedAt { get; set; }
        public int TotalParticipants { get; set; }
    }
}

