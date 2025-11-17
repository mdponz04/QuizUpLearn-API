using Repository.Entities;

namespace Repository.Interfaces
{
    public interface IPaymentTransactionRepo
    {
        Task<IEnumerable<PaymentTransaction>> GetAllAsync();
        Task<PaymentTransaction> GetByIdAsync(Guid id);
        Task<PaymentTransaction> CreateAsync(PaymentTransaction subscriptionPlan);
        Task<PaymentTransaction> UpdateAsync(Guid id, PaymentTransaction subscriptionPlan);
        Task<bool> DeleteAsync(Guid id);
    }
}
