using BusinessLogic.DTOs;
using BusinessLogic.DTOs.QuizSetCommentDtos;

namespace BusinessLogic.Interfaces
{
    public interface IQuizSetCommentService
    {
        Task<ResponseQuizSetCommentDto> CreateAsync(RequestQuizSetCommentDto dto);
        Task<ResponseQuizSetCommentDto?> GetByIdAsync(Guid id);
        Task<PaginationResponseDto<ResponseQuizSetCommentDto>> GetAllAsync(PaginationRequestDto pagination, bool includeDeleted = false);
        Task<PaginationResponseDto<ResponseQuizSetCommentDto>> GetByUserIdAsync(Guid userId, PaginationRequestDto pagination, bool includeDeleted = false);
        Task<PaginationResponseDto<ResponseQuizSetCommentDto>> GetByQuizSetIdAsync(Guid quizSetId, PaginationRequestDto pagination, bool includeDeleted = false);
        Task<ResponseQuizSetCommentDto?> UpdateAsync(Guid id, RequestQuizSetCommentDto dto);
        Task<bool> HardDeleteAsync(Guid id);
        Task<int> GetCommentCountByQuizSetAsync(Guid quizSetId);
    }
}
