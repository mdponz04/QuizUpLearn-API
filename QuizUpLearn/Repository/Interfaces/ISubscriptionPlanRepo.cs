using Repository.Entities;

namespace Repository.Interfaces
{
    public interface ISubscriptionPlanRepo
    {
        Task<IEnumerable<SubscriptionPlan>> GetAllAsync();
        Task<SubscriptionPlan> GetByIdAsync(Guid id);
        Task<SubscriptionPlan> CreateAsync(SubscriptionPlan subscriptionPlan);
        Task<SubscriptionPlan> UpdateAsync(Guid id, SubscriptionPlan subscriptionPlan);
        Task<bool> DeleteAsync(Guid id);
    }
}
