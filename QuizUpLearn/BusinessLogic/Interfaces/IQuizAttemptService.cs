using BusinessLogic.DTOs;

namespace BusinessLogic.Interfaces
{
    public interface IQuizAttemptService
    {
        Task<ResponseQuizAttemptDto> CreateAsync(RequestQuizAttemptDto dto);
        Task<ResponseSingleStartDto> StartSingleAsync(RequestSingleStartDto dto);
        Task<ResponseSingleStartDto> StartMistakeQuizzesAsync(RequestStartMistakeQuizzesDto dto);
        Task<ResponseQuizAttemptDto?> GetByIdAsync(Guid id);
        Task<IEnumerable<ResponseQuizAttemptDto>> GetAllAsync(bool includeDeleted = false);
        Task<IEnumerable<ResponseQuizAttemptDto>> GetByUserIdAsync(Guid userId, bool includeDeleted = false);
        Task<IEnumerable<ResponseQuizAttemptDto>> GetByQuizSetIdAsync(Guid quizSetId, bool includeDeleted = false);
        Task<ResponseQuizAttemptDto?> UpdateAsync(Guid id, RequestQuizAttemptDto dto);
        Task<PlayerHistoryResponseDto> GetPlayerHistoryAsync(PlayerHistoryRequestDto request);
        Task<PlacementTestHistoryResponseDto> GetPlacementTestHistoryAsync(PlayerHistoryRequestDto request);
        Task<PlayerStatsDto> GetPlayerStatsAsync(Guid userId);
        Task<bool> SoftDeleteAsync(Guid id);
        Task<bool> RestoreAsync(Guid id);
    }
}
