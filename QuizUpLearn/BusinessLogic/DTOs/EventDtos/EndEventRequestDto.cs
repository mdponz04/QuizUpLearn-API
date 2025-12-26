namespace BusinessLogic.DTOs.EventDtos
{
    /// <summary>
    /// DTO để end event
    /// </summary>
    public class EndEventRequestDto
    {
        public Guid EventId { get; set; }
        public string GamePin { get; set; } = string.Empty;
    }
}

