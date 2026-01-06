using Microsoft.EntityFrameworkCore;
using Repository.DBContext;
using Repository.Entities;
using Repository.Interfaces;

namespace Repository.Repositories
{
    public class BadgeRepo : IBadgeRepo
    {
        private readonly MyDbContext _context;

        public BadgeRepo(MyDbContext context)
        {
            _context = context;
        }

        public async Task<Badge?> GetByIdAsync(Guid id)
        {
            return await _context.Badges.FindAsync(id);
        }

        public async Task<Badge?> GetByCodeAsync(string code)
        {
            return await _context.Badges
                .FirstOrDefaultAsync(b => b.Code == code && b.DeletedAt == null);
        }

        public async Task<IEnumerable<Badge>> GetAllAsync(bool includeDeleted = false)
        {
            return await _context.Badges
                .AsQueryable()
                .Where(b => includeDeleted || b.DeletedAt == null)
                .ToListAsync();
        }

        public async Task<Badge> CreateAsync(Badge badge)
        {
            await _context.Badges.AddAsync(badge);
            await _context.SaveChangesAsync();
            return badge;
        }
    }
}

