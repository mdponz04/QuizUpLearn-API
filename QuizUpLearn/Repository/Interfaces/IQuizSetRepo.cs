using Repository.Entities;
using Repository.Enums;

namespace Repository.Interfaces
{
    public interface IQuizSetRepo
    {
        Task<QuizSet> CreateQuizSetAsync(QuizSet quizSet);
        Task<QuizSet?> GetQuizSetByIdAsync(Guid id);
        Task<IEnumerable<QuizSet>> GetAllQuizSetsAsync();
        Task<IEnumerable<QuizSet>> GetQuizSetsByCreatorAsync(Guid creatorId);
        Task<IEnumerable<QuizSet>> GetPublishedQuizSetsAsync();
        Task<QuizSet?> UpdateQuizSetAsync(Guid id, QuizSet quizSet);
        Task<bool> SoftDeleteQuizSetAsync(Guid id);
        Task<bool> HardDeleteQuizSetAsync(Guid id);
        Task<bool> QuizSetExistsAsync(Guid id);
        Task<QuizSet?> RestoreQuizSetAsync(Guid id);
        Task<bool> RequestValidateByModAsync(Guid id);
        Task<bool> ValidateQuizSetAsync(Guid id);
    }
}
