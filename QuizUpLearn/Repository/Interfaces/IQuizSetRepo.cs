using Repository.Entities;
using Repository.Enums;

namespace Repository.Interfaces
{
    public interface IQuizSetRepo
    {
        Task<QuizSet> CreateQuizSetAsync(QuizSet quizSet);
        Task<QuizSet?> GetQuizSetByIdAsync(Guid id);
        Task<IEnumerable<QuizSet>> GetAllQuizSetsAsync(
            string? searchTerm = null, 
            string? sortBy = null, 
            string? sortDirection = null,
            bool? isDeleted = null,
            bool? isPremiumOnly = null,
            bool? isPublished = null,
            QuizSetTypeEnum? quizSetType = null);
        Task<IEnumerable<QuizSet>> GetQuizSetsByCreatorAsync(
            Guid creatorId, 
            string? searchTerm = null, 
            string? sortBy = null, 
            string? sortDirection = null,
            bool? isDeleted = null,
            bool? isPremiumOnly = null,
            bool? isPublished = null,
            QuizSetTypeEnum? quizSetType = null);
        Task<IEnumerable<QuizSet>> GetPublishedQuizSetsAsync(
            string? searchTerm = null,
            string? sortBy = null,
            string? sortDirection = null,
            bool? isPremiumOnly = null,
            QuizSetTypeEnum? quizSetType = null);
        Task<QuizSet?> UpdateQuizSetAsync(Guid id, QuizSet quizSet);
        Task<bool> SoftDeleteQuizSetAsync(Guid id);
        Task<bool> HardDeleteQuizSetAsync(Guid id);
        Task<bool> QuizSetExistsAsync(Guid id);
        Task<QuizSet?> RestoreQuizSetAsync(Guid id);
        Task<bool> RequestValidateByMod(Guid id);
        Task<bool> ValidateQuizSet(Guid id);
    }
}
