using BusinessLogic.DTOs.AiDtos;
using BusinessLogic.DTOs.QuizSetDtos;
using BusinessLogic.DTOs.UserWeakPointDtos;

namespace BusinessLogic.Interfaces
{
    public interface IAIService
    {
        Task<IEnumerable<ResponseUserWeakPointDto>> AnalyzeUserMistakesAndAdviseAsync(Guid userId);
        Task<(bool, string)> ValidateQuizSetAsync(Guid quizSetId);
        Task<QuizSetResponseDto> GeneratePracticeQuizSetPart1Async(AiGenerateQuizSetRequestDto inputData);
        Task<QuizSetResponseDto> GeneratePracticeQuizSetPart2Async(AiGenerateQuizSetRequestDto inputData);
        Task<QuizSetResponseDto> GeneratePracticeQuizSetPart3Async(AiGenerateQuizSetRequestDto inputData);
        Task<QuizSetResponseDto> GeneratePracticeQuizSetPart4Async(AiGenerateQuizSetRequestDto inputData);
        Task<QuizSetResponseDto> GeneratePracticeQuizSetPart5Async(AiGenerateQuizSetRequestDto inputData);
        Task<QuizSetResponseDto> GeneratePracticeQuizSetPart6Async(AiGenerateQuizSetRequestDto inputData);
        Task<QuizSetResponseDto> GeneratePracticeQuizSetPart7Async(AiGenerateQuizSetRequestDto inputData);
        Task<QuizSetResponseDto> GenerateFixWeakPointQuizSetAsync();
    }
}
