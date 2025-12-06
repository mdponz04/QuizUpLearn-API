using BusinessLogic.DTOs;

namespace BusinessLogic.Interfaces
{
    /// <summary>
    /// Interface cho service quản lý Kahoot-style realtime quiz game
    /// </summary>
    public interface IRealtimeGameService
    {
        // ==================== CREATE GAME ====================
        Task<CreateGameResponseDto> CreateGameAsync(CreateGameDto dto);
        
        // ==================== CONNECTION ====================
        Task<bool> HostConnectAsync(string gamePin, string connectionId);
        
        // ==================== LOBBY ====================
        Task<PlayerInfo?> PlayerJoinAsync(string gamePin, string playerName, string connectionId, Guid? userId = null);
        Task<bool> PlayerLeaveAsync(string gamePin, string connectionId);
        Task<GameSessionDto?> GetGameSessionAsync(string gamePin);
        
        // ==================== GAME CONTROL ====================
        Task<QuestionDto?> StartGameAsync(string gamePin);
        Task<bool> SubmitAnswerAsync(string gamePin, string connectionId, Guid questionId, Guid answerId);
        Task<bool> SetTimeForCurrentQuestionAsync(string gamePin, int seconds);
        
        // ==================== RESULTS ====================
        Task<GameAnswerResultDto?> GetQuestionResultAsync(string gamePin);
        Task<LeaderboardDto?> GetCurrentLeaderboardAsync(string gamePin);
        Task<LeaderboardDto?> GetLeaderboardAsync(string gamePin);
        
        // ==================== NAVIGATION ====================
        Task<QuestionDto?> NextQuestionAsync(string gamePin);
        Task<FinalResultDto?> GetFinalResultAsync(string gamePin);
        
        // ==================== CLEANUP ====================
        Task CleanupGameAsync(string gamePin);
        
        // ==================== CONNECTION MANAGEMENT ====================
        Task<PlayerInfo?> HandleDisconnectAsync(string connectionId);
        Task<string?> GetGamePinByConnectionAsync(string connectionId);
        
        // ==================== BOSS FIGHT MODE ====================
        Task<bool> EnableBossFightModeAsync(string gamePin, int bossHP = 10000, int? timeLimitSeconds = null, int questionTimeLimitSeconds = 30, bool autoNextQuestion = true);
        Task<BossDamagedDto?> DealDamageToBossAsync(string gamePin, string connectionId, int damage);
        Task<BossDefeatedDto?> GetBossDefeatedResultAsync(string gamePin);
        Task<bool> IsBossFightTimeExpiredAsync(string gamePin);
        Task<BossDefeatedDto?> GetBossFightTimeUpResultAsync(string gamePin);
        Task<BossDamagedDto?> GetBossStateAsync(string gamePin);
        
        // ==================== BOSS FIGHT PLAYER SPECIFIC ====================
        Task<QuestionDto?> GetPlayerNextQuestionAsync(string gamePin, string connectionId);
        Task<bool> MovePlayerToNextQuestionAsync(string gamePin, string connectionId, Guid answeredQuestionId);
        Task<PlayerAnswerResult?> SubmitBossFightAnswerAsync(string gamePin, string connectionId, Guid questionId, Guid answerId);
        
        // ==================== ADMIN ====================
        Task<FinalResultDto?> ForceEndGameAsync(string gamePin, string reason = "Game ended by moderator");
        Task<Dictionary<Guid, bool>?> GetCorrectAnswersForQuestionAsync(string gamePin, Guid questionId);
        Task<bool> UpdateLobbySettingsAsync(string gamePin, int bossMaxHP, int? timeLimitSeconds, int questionTimeLimitSeconds);
    }
}

