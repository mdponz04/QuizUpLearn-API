namespace BusinessLogic.DTOs.EventDtos
{
    public class CreateEventRequestDto
    {
        public Guid QuizSetId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public long MaxParticipants { get; set; }
    }
}

