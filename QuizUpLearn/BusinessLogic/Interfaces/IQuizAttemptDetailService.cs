using BusinessLogic.DTOs;

namespace BusinessLogic.Interfaces
{
    public interface IQuizAttemptDetailService
    {
        Task<ResponseQuizAttemptDetailDto> CreateAsync(RequestQuizAttemptDetailDto dto);
        Task<ResponseQuizAttemptDetailDto?> GetByIdAsync(Guid id);
        Task<IEnumerable<ResponseQuizAttemptDetailDto>> GetAllAsync(bool includeDeleted = false);
        Task<IEnumerable<ResponseQuizAttemptDetailDto>> GetByAttemptIdAsync(Guid attemptId, bool includeDeleted = false);
        Task<PaginationResponseDto<ResponseQuizAttemptDetailExtendedDto>> GetByAttemptIdPagedAsync(
            Guid attemptId, 
            PaginationRequestDto pagination, 
            bool includeDeleted = false);
        Task<ResponsePlacementTestDto> GetPlacementTestByAttemptIdAsync(Guid attemptId);
        Task<ResponseQuizAttemptDetailDto?> UpdateAsync(Guid id, RequestQuizAttemptDetailDto dto);
        Task<bool> SoftDeleteAsync(Guid id);
        Task<bool> RestoreAsync(Guid id);
        /// <summary>
        /// Chấm điểm bài làm thông thường (không phải flow làm lại MistakeQuiz)
        /// </summary>
        Task<ResponseSubmitAnswersDto> SubmitAnswersAsync(RequestSubmitAnswersDto dto);
        /// <summary>
        /// Chấm điểm bài làm lại các câu sai (MistakeQuiz) và xoá UserMistake cho những câu đã làm đúng
        /// </summary>
        Task<ResponseSubmitAnswersDto> SubmitMistakeQuizAnswersAsync(RequestSubmitAnswersDto dto);
        Task<ResponsePlacementTestDto> SubmitPlacementTestAsync(RequestSubmitAnswersDto dto);
    }
}
