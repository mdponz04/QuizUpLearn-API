using BusinessLogic.DTOs;

namespace BusinessLogic.Interfaces
{
    public interface IQuizAttemptDetailService
    {
        Task<ResponseQuizAttemptDetailDto> CreateAsync(RequestQuizAttemptDetailDto dto);
        Task<ResponseQuizAttemptDetailDto?> GetByIdAsync(Guid id);
        Task<IEnumerable<ResponseQuizAttemptDetailDto>> GetAllAsync(bool includeDeleted = false);
        Task<IEnumerable<ResponseQuizAttemptDetailDto>> GetByAttemptIdAsync(Guid attemptId, bool includeDeleted = false);
        Task<ResponseQuizAttemptDetailDto?> UpdateAsync(Guid id, RequestQuizAttemptDetailDto dto);
        Task<bool> SoftDeleteAsync(Guid id);
        Task<bool> RestoreAsync(Guid id);
        Task<ResponseSubmitAnswersDto> SubmitAnswersAsync(RequestSubmitAnswersDto dto);
        Task<ResponsePlacementTestDto> SubmitPlacementTestAsync(RequestSubmitAnswersDto dto);
    }
}
