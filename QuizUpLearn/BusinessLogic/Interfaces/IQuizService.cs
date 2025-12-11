using BusinessLogic.DTOs.QuizDtos;
using BusinessLogic.DTOs;

namespace BusinessLogic.Interfaces
{
    public interface IQuizService
    {
        Task<QuizResponseDto> CreateQuizAsync(QuizRequestDto quizDto);
        Task<QuizResponseDto> GetQuizByIdAsync(Guid id);
        Task<PaginationResponseDto<QuizResponseDto>> GetAllQuizzesAsync(PaginationRequestDto pagination);
        Task<PaginationResponseDto<QuizResponseDto>> GetQuizzesByQuizSetIdAsync(Guid quizSetId, PaginationRequestDto pagination);
        Task<PaginationResponseDto<QuizResponseDto>> GetActiveQuizzesAsync(PaginationRequestDto pagination);
        Task<QuizResponseDto> UpdateQuizAsync(Guid id, QuizRequestDto quizDto);
        Task<bool> SoftDeleteQuizAsync(Guid id);
        Task<bool> HardDeleteQuizAsync(Guid id);
        Task<PaginationResponseDto<QuizResponseDto>> GetByGrammarIdAndVocabularyIdAsync(Guid grammarId, Guid vocabularyId, PaginationRequestDto pagination = null!);
    }
}
