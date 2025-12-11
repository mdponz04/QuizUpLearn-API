using BusinessLogic.DTOs;
using BusinessLogic.DTOs.QuizQuizSetDtos;

namespace BusinessLogic.Interfaces
{
    public interface IQuizQuizSetService
    {
        Task<ResponseQuizQuizSetDto> CreateAsync(RequestQuizQuizSetDto dto);
        Task<ResponseQuizQuizSetDto?> GetByIdAsync(Guid id);
        Task<PaginationResponseDto<ResponseQuizQuizSetDto>> GetAllAsync(PaginationRequestDto pagination, bool includeDeleted = false);
        Task<PaginationResponseDto<ResponseQuizQuizSetDto>> GetByQuizIdAsync(Guid quizId, PaginationRequestDto pagination, bool includeDeleted = false);
        Task<PaginationResponseDto<ResponseQuizQuizSetDto>> GetByQuizSetIdAsync(Guid quizSetId, PaginationRequestDto pagination, bool includeDeleted = false);
        Task<ResponseQuizQuizSetDto?> GetByQuizAndQuizSetAsync(Guid quizId, Guid quizSetId, bool includeDeleted = false);
        Task<ResponseQuizQuizSetDto?> UpdateAsync(Guid id, RequestQuizQuizSetDto dto);
        Task<bool> SoftDeleteAsync(Guid id);
        Task<bool> HardDeleteAsync(Guid id);
        Task<bool> IsExistedAsync(Guid quizId, Guid quizSetId);
        Task<int> GetQuizCountByQuizSetAsync(Guid quizSetId);
        Task<bool> AddQuizToQuizSetAsync(Guid quizId, Guid quizSetId);
        Task<bool> RemoveQuizFromQuizSetAsync(Guid quizId, Guid quizSetId);
        Task<bool> AddQuizzesToQuizSetAsync(List<Guid> quizIds, Guid quizSetId);
        Task<bool> DeleteByQuizIdAsync(Guid quizId);
        Task<bool> DeleteByQuizSetIdAsync(Guid quizSetId);
    }
}