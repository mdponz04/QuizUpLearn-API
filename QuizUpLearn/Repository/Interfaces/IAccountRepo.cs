using Repository.Entities;

namespace Repository.Interfaces
{
    public interface IAccountRepo
    {
        Task<Account> CreateAsync(Account account);
        Task<Account?> GetByIdAsync(Guid id);
        Task<IEnumerable<Account>> GetAllAsync(bool includeDeleted = false);
        Task<Account?> GetByEmailAsync(string email);
        Task<Account?> UpdateAsync(Guid id, Account account);
        Task<bool> SoftDeleteAsync(Guid id);
        Task<bool> RestoreAsync(Guid id);
    }
}


