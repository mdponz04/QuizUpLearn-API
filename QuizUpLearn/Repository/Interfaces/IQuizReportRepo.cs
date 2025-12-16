using Repository.Entities;

namespace Repository.Interfaces
{
    public interface IQuizReportRepo
    {
        Task<QuizReport> CreateAsync(QuizReport entity);
        Task<QuizReport?> GetByIdAsync(Guid id);
        Task<IEnumerable<QuizReport>> GetAllAsync(bool includeDeleted = false);
        Task<IEnumerable<QuizReport>> GetByUserIdAsync(Guid userId, bool includeDeleted = false);
        Task<IEnumerable<QuizReport>> GetByQuizIdAsync(Guid quizId, bool includeDeleted = false);
        Task<QuizReport?> GetByUserAndQuizAsync(Guid userId, Guid quizId, bool includeDeleted = false);
        Task<bool> HardDeleteAsync(Guid id);
        Task<bool> IsExistAsync(Guid userId, Guid quizId);
    }
}
