using BusinessLogic.DTOs;
using BusinessLogic.DTOs.QuizQuizSetDtos;

namespace BusinessLogic.Interfaces
{
    public interface IQuizQuizSetService
    {
        Task<ResponseQuizQuizSetDto> CreateQuizQuizSetAsync(RequestQuizQuizSetDto dto);
        Task<ResponseQuizQuizSetDto?> GetQuizQuizSetByIdAsync(Guid id);
        Task<PaginationResponseDto<ResponseQuizQuizSetDto>> GetAllQuizQuizSetAsync(PaginationRequestDto pagination, bool includeDeleted = false);
        Task<PaginationResponseDto<ResponseQuizQuizSetDto>> GetQuizQuizSetByQuizIdAsync(Guid quizId, PaginationRequestDto pagination, bool includeDeleted = false);
        Task<PaginationResponseDto<ResponseQuizQuizSetDto>> GetQuizQuizSetByQuizSetIdAsync(Guid quizSetId, PaginationRequestDto pagination, bool includeDeleted = false);
        Task<ResponseQuizQuizSetDto?> GetQuizQuizSetByQuizAndQuizSetAsync(Guid quizId, Guid quizSetId, bool includeDeleted = false);
        Task<ResponseQuizQuizSetDto?> UpdateQuizQuizSetAsync(Guid id, RequestQuizQuizSetDto dto);
        Task<bool> HardDeleteQuizQuizSetAsync(Guid id);
        Task<bool> IsQuizQuizSetExistedAsync(Guid quizId, Guid quizSetId);
        Task<int> GetQuizCountByQuizSetAsync(Guid quizSetId);
        Task<bool> AddQuizToQuizSetAsync(Guid quizId, Guid quizSetId);
        Task<bool> AddQuizzesToQuizSetAsync(List<Guid> quizIds, Guid quizSetId);
        Task<bool> DeleteQuizQuizSetByQuizIdAsync(Guid quizId);
        Task<bool> DeleteQuizQuizSetByQuizSetIdAsync(Guid quizSetId);
    }
}