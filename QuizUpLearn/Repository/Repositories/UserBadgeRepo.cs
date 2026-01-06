using Microsoft.EntityFrameworkCore;
using Repository.DBContext;
using Repository.Entities;
using Repository.Interfaces;

namespace Repository.Repositories
{
    public class UserBadgeRepo : IUserBadgeRepo
    {
        private readonly MyDbContext _context;

        public UserBadgeRepo(MyDbContext context)
        {
            _context = context;
        }

        public async Task<UserBadge> CreateAsync(UserBadge userBadge)
        {
            await _context.UserBadges.AddAsync(userBadge);
            await _context.SaveChangesAsync();
            return userBadge;
        }

        public async Task<UserBadge?> GetByIdAsync(Guid id)
        {
            return await _context.UserBadges.FindAsync(id);
        }

        public async Task<IEnumerable<UserBadge>> GetByUserIdAsync(Guid userId, bool includeDeleted = false)
        {
            return await _context.UserBadges
                .AsQueryable()
                .Where(ub => ub.UserId == userId && (includeDeleted || ub.DeletedAt == null))
                .Include(ub => ub.Badge)
                .ToListAsync();
        }

        public async Task<UserBadge?> GetByUserAndBadgeAsync(Guid userId, Guid badgeId, bool includeDeleted = false)
        {
            return await _context.UserBadges
                .FirstOrDefaultAsync(ub => 
                    ub.UserId == userId 
                    && ub.BadgeId == badgeId 
                    && (includeDeleted || ub.DeletedAt == null));
        }

        public async Task<bool> ExistsAsync(Guid userId, Guid badgeId)
        {
            return await _context.UserBadges
                .AnyAsync(ub => 
                    ub.UserId == userId 
                    && ub.BadgeId == badgeId 
                    && ub.DeletedAt == null);
        }
    }
}

