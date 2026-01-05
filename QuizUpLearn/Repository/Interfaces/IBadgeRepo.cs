using Repository.Entities;

namespace Repository.Interfaces
{
    public interface IBadgeRepo
    {
        Task<Badge?> GetByIdAsync(Guid id);
        Task<Badge?> GetByCodeAsync(string code);
        Task<IEnumerable<Badge>> GetAllAsync(bool includeDeleted = false);
        Task<Badge> CreateAsync(Badge badge);
    }
}

