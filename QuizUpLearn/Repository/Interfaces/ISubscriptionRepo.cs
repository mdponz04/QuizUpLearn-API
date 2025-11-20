using Repository.Entities;

namespace Repository.Interfaces
{
    public interface ISubscriptionRepo
    {
        Task<IEnumerable<Subscription>> GetAllAsync();
        Task<Subscription> GetByUserIdAsync(Guid userId);
        Task<Subscription> GetByIdAsync(Guid id);
        Task<Subscription> CreateAsync(Subscription subscriptionPlan);
        Task<Subscription> UpdateAsync(Guid id, Subscription subscriptionPlan);
        Task<bool> DeleteAsync(Guid id);
    }
}
