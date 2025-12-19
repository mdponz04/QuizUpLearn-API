using BusinessLogic.DTOs.AiDtos;

namespace BusinessLogic.Interfaces
{
    public interface IAIService
    {
        Task AnalyzeUserMistakesAndAdviseAsync(Guid userId);
        Task<(Guid quizGroupItemId, Guid? singleQuizId)> GeneratePracticeQuizzesAsync(AiGenerateQuizRequestDto inputData);
    }
}
