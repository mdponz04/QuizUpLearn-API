namespace BusinessLogic.DTOs.EventDtos
{
    /// <summary>
    /// DTO để start event và tạo game room trong GameHub
    /// </summary>
    public class StartEventRequestDto
    {
        public Guid EventId { get; set; }
        public string HostUserName { get; set; } = string.Empty;
    }
}

