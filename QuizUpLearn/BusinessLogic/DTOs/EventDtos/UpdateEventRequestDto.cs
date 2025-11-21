namespace BusinessLogic.DTOs.EventDtos
{
    public class UpdateEventRequestDto
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public long? MaxParticipants { get; set; }
        public string? Status { get; set; }
    }
}

