using BusinessLogic.DTOs.EventDtos;

namespace BusinessLogic.Interfaces
{
    public interface IEventService
    {
        Task<EventResponseDto> CreateEventAsync(Guid userId, CreateEventRequestDto dto);
        Task<EventResponseDto?> GetEventByIdAsync(Guid id);
        Task<IEnumerable<EventResponseDto>> GetAllEventsAsync();
        Task<IEnumerable<EventResponseDto>> GetActiveEventsAsync();
        Task<IEnumerable<EventResponseDto>> GetUpcomingEventsAsync();
        Task<IEnumerable<EventResponseDto>> GetMyEventsAsync(Guid userId);
        Task<EventResponseDto?> UpdateEventAsync(Guid id, UpdateEventRequestDto dto);
        Task<bool> DeleteEventAsync(Guid id);
        Task<StartEventResponseDto> StartEventAsync(Guid userId, StartEventRequestDto dto);
        Task<IEnumerable<EventParticipantResponseDto>> GetEventParticipantsAsync(Guid eventId);
        Task<EventLeaderboardResponseDto> GetEventLeaderboardAsync(Guid eventId);
        Task<bool> JoinEventAsync(Guid eventId, Guid userId);
        Task<bool> IsUserJoinedAsync(Guid eventId, Guid userId);
        Task SyncPlayerScoreAsync(Guid eventId, Guid userId, long score, double accuracy);
        Task SaveEventGameHistoryAsync(Guid eventId, Guid userId, Guid quizSetId, int totalQuestions, int correctAnswers, int wrongAnswers, long score, double accuracy, int? timeSpent);
        Task<bool> UpdateEventStatusAsync(Guid eventId, string status);
        Task<EndEventResponseDto> EndEventAsync(Guid userId, EndEventRequestDto dto);
    }
}

