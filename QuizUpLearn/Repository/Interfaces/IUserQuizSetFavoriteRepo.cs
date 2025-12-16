using Repository.Entities;

namespace Repository.Interfaces
{
    public interface IUserQuizSetFavoriteRepo
    {
        Task<UserQuizSetFavorite> CreateAsync(UserQuizSetFavorite entity);
        Task<UserQuizSetFavorite?> GetByIdAsync(Guid id);
        Task<IEnumerable<UserQuizSetFavorite>> GetAllAsync(bool includeDeleted = false);
        Task<IEnumerable<UserQuizSetFavorite>> GetByUserIdAsync(Guid userId, bool includeDeleted = false);
        Task<IEnumerable<UserQuizSetFavorite>> GetByQuizSetIdAsync(Guid quizSetId, bool includeDeleted = false);
        Task<UserQuizSetFavorite?> GetByUserAndQuizSetAsync(Guid userId, Guid quizSetId, bool includeDeleted = false);
        Task<bool> HardDeleteAsync(Guid id);
        Task<bool> IsExistAsync(Guid userId, Guid quizSetId);
        Task<int> GetFavoriteCountByQuizSetAsync(Guid quizSetId);
    }
}
