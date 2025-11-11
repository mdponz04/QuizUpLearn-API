using BusinessLogic.DTOs.AiDtos;
using BusinessLogic.DTOs.QuizSetDtos;
using BusinessLogic.DTOs.UserWeakPointDtos;

namespace BusinessLogic.Interfaces
{
    public interface IAIService
    {
        Task<IEnumerable<ResponseUserWeakPointDto>> AnalyzeUserMistakesAndAdviseAsync(Guid userId);
        Task<(bool, string)> ValidateQuizSetAsync(Guid quizSetId);
        Task<bool> GeneratePracticeQuizSetPart1Async(AiGenerateQuizSetRequestDto inputData, Guid quizSetId);
        Task<bool> GeneratePracticeQuizSetPart2Async(AiGenerateQuizSetRequestDto inputData, Guid quizSetId);
        Task<bool> GeneratePracticeQuizSetPart3Async(AiGenerateQuizSetRequestDto inputData, Guid quizSetId);
        Task<bool> GeneratePracticeQuizSetPart4Async(AiGenerateQuizSetRequestDto inputData, Guid quizSetId);
        Task<bool> GeneratePracticeQuizSetPart5Async(AiGenerateQuizSetRequestDto inputData, Guid quizSetId);
        Task<bool> GeneratePracticeQuizSetPart6Async(AiGenerateQuizSetRequestDto inputData, Guid quizSetId);
        Task<bool> GeneratePracticeQuizSetPart7Async(AiGenerateQuizSetRequestDto inputData, Guid quizSetId);
        Task<QuizSetResponseDto> GenerateFixWeakPointQuizSetAsync();
    }
}
