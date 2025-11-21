namespace BusinessLogic.DTOs.EventDtos
{
    public class EventResponseDto
    {
        public Guid Id { get; set; }
        public Guid QuizSetId { get; set; }
        public string QuizSetTitle { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public long MaxParticipants { get; set; }
        public long CurrentParticipants { get; set; }
        public string Status { get; set; } = string.Empty;
        public Guid CreatedBy { get; set; }
        public string CreatorName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}

