using Repository.Entities;

namespace Repository.Interfaces
{
    public interface IUserReportRepo
    {
        Task<UserReport> CreateAsync(UserReport entity);
        Task<UserReport?> GetByIdAsync(Guid id);
        Task<IEnumerable<UserReport>> GetAllAsync(bool includeDeleted = false);
        Task<IEnumerable<UserReport>> GetByUserIdAsync(Guid userId, bool includeDeleted = false);
        Task<bool> HardDeleteAsync(Guid id);
        Task<bool> IsExistAsync(Guid userId);
    }
}


