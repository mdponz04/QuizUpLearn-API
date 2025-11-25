using BusinessLogic.DTOs;

namespace BusinessLogic.Interfaces
{
    /// <summary>
    /// Interface cho service quản lý game 1vs1 và Multiplayer
    /// Hỗ trợ cả 2 chế độ: OneVsOne (2 players) và Multiplayer (unlimited)
    /// </summary>
    public interface IOneVsOneGameService
    {
        // ==================== CREATE ROOM ====================
        Task<CreateOneVsOneRoomResponseDto> CreateRoomAsync(CreateOneVsOneRoomDto dto);
        
        // ==================== JOIN ROOM ====================
        Task<bool> PlayerConnectAsync(string roomPin, Guid userId, string connectionId);
        Task<OneVsOnePlayerDto?> PlayerJoinAsync(string roomPin, Guid userId, string playerName, string connectionId);
        Task<bool> ReconnectPlayerAsync(string roomPin, Guid userId, string newConnectionId);
        Task<bool> PlayerLeaveAsync(string roomPin, string connectionId);
        
        // ==================== GAME CONTROL ====================
        Task<bool> StartGameAsync(string roomPin);
        Task<OneVsOneRoundResultDto?> SubmitAnswerAsync(string roomPin, string connectionId, Guid questionId, Guid answerId);
        Task<bool> NextQuestionAsync(string roomPin);
        
        // ==================== GET STATE ====================
        Task<OneVsOneRoomDto?> GetRoomAsync(string roomPin);
        Task<string?> GetRoomPinByConnectionAsync(string connectionId);
        
        // ==================== RESULT MANAGEMENT ====================
        Task<OneVsOneRoundResultDto?> GetCurrentRoundResultAsync(string roomPin);
        Task MarkResultShownAsync(string roomPin);
        
        // ==================== GAME END ====================
        Task<OneVsOneFinalResultDto?> GetFinalResultAsync(string roomPin);
        Task CleanupRoomAsync(string roomPin);
    }
}

