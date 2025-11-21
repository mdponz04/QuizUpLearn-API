namespace BusinessLogic.DTOs.EventDtos
{
    public class EventParticipantResponseDto
    {
        public Guid Id { get; set; }
        public Guid EventId { get; set; }
        public Guid ParticipantId { get; set; }
        public string ParticipantName { get; set; } = string.Empty;
        public long Score { get; set; }
        public double Accuracy { get; set; }
        public long Rank { get; set; }
        public DateTime JoinAt { get; set; }
        public DateTime? FinishAt { get; set; }
    }
}

