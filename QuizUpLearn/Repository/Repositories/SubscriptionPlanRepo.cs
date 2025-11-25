using Microsoft.EntityFrameworkCore;
using Repository.DBContext;
using Repository.Entities;
using Repository.Interfaces;

namespace Repository.Repositories
{
    public class SubscriptionPlanRepo : ISubscriptionPlanRepo
    {
        private readonly MyDbContext _context;

        public SubscriptionPlanRepo(MyDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<SubscriptionPlan>> GetAllAsync()
        {
            return await _context.SubscriptionPlans.ToListAsync();
        }

        public async Task<SubscriptionPlan?> GetByIdAsync(Guid id)
        {
            return await _context.SubscriptionPlans.FindAsync(id);
        }

        public async Task<SubscriptionPlan?> CreateAsync(SubscriptionPlan subscriptionPlan)
        {
            _context.SubscriptionPlans.Add(subscriptionPlan);
            await _context.SaveChangesAsync();
            return subscriptionPlan;
        }

        public async Task<SubscriptionPlan?> UpdateAsync(Guid id, SubscriptionPlan subscriptionPlan)
        {
            var existing = await _context.SubscriptionPlans.FindAsync(id);
            if (existing == null) return null;

            _context.Entry(existing).CurrentValues.SetValues(subscriptionPlan);
            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var entity = await _context.SubscriptionPlans.FindAsync(id);
            if (entity == null) return false;

            _context.SubscriptionPlans.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<SubscriptionPlan> GetFreeSubscriptionPlan()
        {
            var existing = await _context.SubscriptionPlans
                .Where(sp => sp.Name.ToLower() == "free")
                .FirstOrDefaultAsync();
            if(existing != null) 
                return existing;

            var freePlan = new SubscriptionPlan
            {
                Name = "Free",
                Price = 0,
                DurationDays = 999999,
                CanAccessPremiumContent = false,
                CanAccessAiFeatures = false,
                AiGenerateQuizSetMaxTimes = 0,
                IsActive = true
            };

            _context.SubscriptionPlans.Add(freePlan);
            await _context.SaveChangesAsync();
            return freePlan;
        }
    }
}
