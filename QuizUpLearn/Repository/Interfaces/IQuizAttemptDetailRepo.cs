using Repository.Entities;

namespace Repository.Interfaces
{
    public interface IQuizAttemptDetailRepo
    {
        Task<QuizAttemptDetail> CreateAsync(QuizAttemptDetail quizAttemptDetail);
        Task<int> CreateBatchAsync(IEnumerable<QuizAttemptDetail> quizAttemptDetails);
        Task<QuizAttemptDetail?> GetByIdAsync(Guid id);
        Task<IEnumerable<QuizAttemptDetail>> GetAllAsync(bool includeDeleted = false);
        Task<IEnumerable<QuizAttemptDetail>> GetByAttemptIdAsync(Guid attemptId, bool includeDeleted = false);
        Task<(IEnumerable<QuizAttemptDetail> details, int totalCount)> GetByAttemptIdPagedAsync(
            Guid attemptId, 
            int page = 1, 
            int pageSize = 10, 
            bool includeDeleted = false);
        Task<QuizAttemptDetail?> UpdateAsync(Guid id, QuizAttemptDetail quizAttemptDetail);
        Task<bool> SoftDeleteAsync(Guid id);
        Task<bool> RestoreAsync(Guid id);
    }
}
