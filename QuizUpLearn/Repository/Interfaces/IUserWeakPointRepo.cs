using Repository.Entities;

namespace Repository.Interfaces
{
    public interface IUserWeakPointRepo
    {
        Task<IEnumerable<UserWeakPoint>> GetByUserIdAsync(Guid userId);
        Task<UserWeakPoint?> GetByIdAsync(Guid id);
        Task<bool> IsWeakPointExisted(string weakPoint, Guid userId);
        Task<UserWeakPoint?> AddAsync(UserWeakPoint userWeakPoint);
        Task<UserWeakPoint?> UpdateAsync(Guid id, UserWeakPoint userWeakPoint);
        Task<bool> DeleteAsync(Guid id);
    }
}
