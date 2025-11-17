using Microsoft.EntityFrameworkCore;
using Repository.DBContext;
using Repository.Entities;
using Repository.Interfaces;

namespace Repository.Repositories
{
    public class SubscriptionRepo : ISubscriptionRepo
    {
        private readonly MyDbContext _context;

        public SubscriptionRepo(MyDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Subscription>> GetAllAsync()
        {
            return await _context.Subscriptions.ToListAsync();
        }

        public async Task<Subscription?> GetByIdAsync(Guid id)
        {
            return await _context.Subscriptions.FindAsync(id);
        }

        public async Task<Subscription> CreateAsync(Subscription subscription)
        {
            _context.Subscriptions.Add(subscription);
            await _context.SaveChangesAsync();
            return subscription;
        }

        public async Task<Subscription?> UpdateAsync(Guid id, Subscription subscription)
        {
            var existing = await _context.Subscriptions.FindAsync(id);
            if (existing == null) return null;

            if(existing.SubscriptionPlanId != subscription.SubscriptionPlanId)
                existing.SubscriptionPlanId = subscription.SubscriptionPlanId;
            if(existing.AiGenerateQuizSetRemaining != subscription.AiGenerateQuizSetRemaining)
                existing.AiGenerateQuizSetRemaining = subscription.AiGenerateQuizSetRemaining;
            if(existing.EndDate != subscription.EndDate)
                existing.EndDate = subscription.EndDate;

            existing.UpdatedAt = DateTime.UtcNow;

            _context.Subscriptions.Update(existing);
            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var entity = await _context.Subscriptions.FindAsync(id);
            if (entity == null) return false;

            _context.Subscriptions.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
