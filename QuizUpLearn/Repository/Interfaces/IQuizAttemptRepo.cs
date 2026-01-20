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
        
        // Optimized methods for history with pagination
        Task<(IEnumerable<QuizAttempt> attempts, int totalCount)> GetUserHistoryPagedAsync(
            Guid userId,
            Guid? quizSetId = null,
            string? status = null,
            string? attemptType = null,
            string sortBy = "CreatedAt",
            string sortOrder = "desc",
            int page = 1,
            int pageSize = 10,
            bool includeDeleted = false);
        
        Task<(IEnumerable<QuizAttempt> attempts, int totalCount)> GetPlacementTestHistoryPagedAsync(
            Guid userId,
            Guid? quizSetId = null,
            string? status = null,
            string sortBy = "CreatedAt",
            string sortOrder = "desc",
            int page = 1,
            int pageSize = 10,
            bool includeDeleted = false);
    }
}
