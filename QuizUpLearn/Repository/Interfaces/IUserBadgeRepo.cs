using Repository.Entities;

namespace Repository.Interfaces
{
    public interface IUserBadgeRepo
    {
        Task<UserBadge> CreateAsync(UserBadge userBadge);
        Task<UserBadge?> GetByIdAsync(Guid id);
        Task<IEnumerable<UserBadge>> GetByUserIdAsync(Guid userId, bool includeDeleted = false);
        Task<UserBadge?> GetByUserAndBadgeAsync(Guid userId, Guid badgeId, bool includeDeleted = false);
        Task<bool> ExistsAsync(Guid userId, Guid badgeId);
    }
}

