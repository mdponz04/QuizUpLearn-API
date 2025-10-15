using Repository.Entities;

namespace Repository.Interfaces
{
    public interface IUserRepo
    {
        Task<User> CreateAsync(User user);
        Task<User?> GetByIdAsync(Guid id);
        Task<IEnumerable<User>> GetAllAsync(bool includeDeleted = false);
        Task<User?> GetByUsernameAsync(string username);
        Task<User?> GetByAccountIdAsync(Guid accountId);
        Task<User?> UpdateAsync(Guid id, User user);
        Task<bool> SoftDeleteAsync(Guid id);
        Task<bool> RestoreAsync(Guid id);
    }
}
