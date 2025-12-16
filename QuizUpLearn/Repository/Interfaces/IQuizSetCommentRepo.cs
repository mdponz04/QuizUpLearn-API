using Repository.Entities;

namespace Repository.Interfaces
{
    public interface IQuizSetCommentRepo
    {
        Task<QuizSetComment> CreateAsync(QuizSetComment entity);
        Task<QuizSetComment?> GetByIdAsync(Guid id);
        Task<IEnumerable<QuizSetComment>> GetAllAsync(bool includeDeleted = false);
        Task<IEnumerable<QuizSetComment>> GetByUserIdAsync(Guid userId, bool includeDeleted = false);
        Task<IEnumerable<QuizSetComment>> GetByQuizSetIdAsync(Guid quizSetId, bool includeDeleted = false);
        Task<QuizSetComment?> UpdateAsync(Guid id, QuizSetComment entity);
        Task<bool> HardDeleteAsync(Guid id);
        Task<int> GetCommentCountByQuizSetAsync(Guid quizSetId);
    }
}
