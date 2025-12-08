using Repository.Entities;

namespace Repository.Interfaces
{
    public interface IQuizAttemptRepo
    {
        Task<QuizAttempt> CreateAsync(QuizAttempt quizAttempt);
        Task<QuizAttempt?> GetByIdAsync(Guid id);
        Task<IEnumerable<QuizAttempt>> GetAllAsync(bool includeDeleted = false);
        Task<IEnumerable<QuizAttempt>> GetByUserIdAsync(Guid userId, bool includeDeleted = false);
        Task<IEnumerable<QuizAttempt>> GetByQuizSetIdAsync(Guid quizSetId, bool includeDeleted = false);
        Task<IEnumerable<QuizAttempt>> GetByQuizSetIdsAsync(IEnumerable<Guid> quizSetIds, bool includeDeleted = false);
        Task<QuizAttempt?> UpdateAsync(Guid id, QuizAttempt quizAttempt);
        Task<bool> SoftDeleteAsync(Guid id);
        Task<bool> RestoreAsync(Guid id);
    }
}
