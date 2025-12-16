using Repository.Entities;

namespace Repository.Interfaces
{
    public interface IQuizQuizSetRepo
    {
        Task<QuizQuizSet> CreateAsync(QuizQuizSet entity);
        Task<QuizQuizSet?> GetByIdAsync(Guid id);
        Task<IEnumerable<QuizQuizSet>> GetAllAsync(bool includeDeleted = false);
        Task<IEnumerable<QuizQuizSet>> GetByQuizIdAsync(Guid quizId, bool includeDeleted = false);
        Task<IEnumerable<QuizQuizSet>> GetByQuizSetIdAsync(Guid quizSetId, bool includeDeleted = false);
        Task<QuizQuizSet?> GetByQuizAndQuizSetAsync(Guid quizId, Guid quizSetId, bool includeDeleted = false);
        Task<QuizQuizSet?> UpdateAsync(Guid id, QuizQuizSet entity);
        Task<bool> HardDeleteAsync(Guid id);
        Task<bool> IsExistedAsync(Guid quizId, Guid quizSetId);
        Task<int> GetQuizCountByQuizSetAsync(Guid quizSetId);
        Task<bool> DeleteByQuizIdAsync(Guid quizId);
        Task<bool> DeleteByQuizSetIdAsync(Guid quizSetId);
        Task AddRangeAsync(IEnumerable<QuizQuizSet> entities);
    }
}