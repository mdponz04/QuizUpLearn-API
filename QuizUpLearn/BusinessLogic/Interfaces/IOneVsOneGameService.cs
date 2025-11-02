using BusinessLogic.DTOs;

namespace BusinessLogic.Interfaces
{
    /// <summary>
    /// Interface cho service quản lý game 1vs1
    /// </summary>
    public interface IOneVsOneGameService
    {
        // ==================== CREATE ROOM ====================
        Task<CreateOneVsOneRoomResponseDto> CreateRoomAsync(CreateOneVsOneRoomDto dto);
        
        // ==================== JOIN ROOM ====================
        Task<bool> PlayerConnectAsync(string roomPin, string connectionId);
        Task<OneVsOnePlayerDto?> PlayerJoinAsync(string roomPin, Guid userId, string playerName, string connectionId);
        Task<bool> PlayerLeaveAsync(string roomPin, string connectionId);
        
        // ==================== GAME CONTROL ====================
        Task<bool> StartGameAsync(string roomPin);
        Task<OneVsOneRoundResultDto?> SubmitAnswerAsync(string roomPin, string connectionId, Guid questionId, Guid answerId);
        Task<bool> NextQuestionAsync(string roomPin);
        
        // ==================== GET STATE ====================
        Task<OneVsOneRoomDto?> GetRoomAsync(string roomPin);
        Task<string?> GetRoomPinByConnectionAsync(string connectionId);
        
        // ==================== GAME END ====================
        Task<OneVsOneFinalResultDto?> GetFinalResultAsync(string roomPin);
        Task CleanupRoomAsync(string roomPin);
    }
}

