using Repository.Entities;

namespace Repository.Interfaces
{
    public interface IUserQuizSetLikeRepo
    {
        Task<UserQuizSetLike> CreateAsync(UserQuizSetLike entity);
        Task<UserQuizSetLike?> GetByIdAsync(Guid id);
        Task<IEnumerable<UserQuizSetLike>> GetAllAsync(bool includeDeleted = false);
        Task<IEnumerable<UserQuizSetLike>> GetByUserIdAsync(Guid userId, bool includeDeleted = false);
        Task<IEnumerable<UserQuizSetLike>> GetByQuizSetIdAsync(Guid quizSetId, bool includeDeleted = false);
        Task<UserQuizSetLike?> GetByUserAndQuizSetAsync(Guid userId, Guid quizSetId, bool includeDeleted = false);
        Task<bool> HardDeleteAsync(Guid id);
        Task<bool> IsExistAsync(Guid userId, Guid quizSetId);
        Task<int> GetLikeCountByQuizSetAsync(Guid quizSetId);
    }
}
