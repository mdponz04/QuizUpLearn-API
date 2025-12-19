using BusinessLogic.DTOs.AiDtos;

namespace BusinessLogic.Interfaces
{
    public interface IAIService
    {
        Task AnalyzeUserMistakesAndAdviseAsync(Guid userId);
        Task<(Guid quizGroupItemId, Guid? singleQuizid)> GeneratePracticeQuizzesAsync(AiGenerateQuizRequestDto inputData);
    }
}
