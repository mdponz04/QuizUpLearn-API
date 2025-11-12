using BusinessLogic.DTOs.QuizSetDtos;
using BusinessLogic.DTOs;

namespace BusinessLogic.Interfaces
{
    public interface IQuizSetService
    {
        Task<QuizSetResponseDto> CreateQuizSetAsync(QuizSetRequestDto quizSetDto);
        Task<QuizSetResponseDto> GetQuizSetByIdAsync(Guid id);
        Task<PaginationResponseDto<QuizSetResponseDto>> GetAllQuizSetsAsync(bool includeDeleted, PaginationRequestDto pagination);
        Task<PaginationResponseDto<QuizSetResponseDto>> GetQuizSetsByCreatorAsync(Guid creatorId, PaginationRequestDto pagination);
        Task<PaginationResponseDto<QuizSetResponseDto>> GetPublishedQuizSetsAsync(PaginationRequestDto pagination);
        Task<QuizSetResponseDto> UpdateQuizSetAsync(Guid id, QuizSetRequestDto quizSetDto);
        Task<bool> SoftDeleteQuizSetAsync(Guid id);
        Task<bool> HardDeleteQuizSetAsync(Guid id);
        Task<QuizSetResponseDto> RestoreQuizSetAsync(Guid id);
    }
}
