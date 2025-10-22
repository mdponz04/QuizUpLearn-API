using Microsoft.EntityFrameworkCore;
using Repository.DBContext;
using Repository.Entities;
using Repository.Interfaces;

namespace Repository.Repositories
{
    public class UserRepo : IUserRepo
    {
        private readonly MyDbContext _context;

        public UserRepo(MyDbContext context)
        {
            _context = context;
        }

        public async Task<User> CreateAsync(User user)
        {
            user.Username = user.Username.Trim();
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<IEnumerable<User>> GetAllAsync(bool includeDeleted = false)
        {
            return await _context.Users
                .AsQueryable()
                .Where(u => includeDeleted || u.DeletedAt == null)
                .ToListAsync();
        }

        public async Task<User?> GetByIdAsync(Guid id)
        {
            return await _context.Users.FindAsync(id);
        }

        public async Task<User?> GetByUsernameAsync(string username)
        {
            var normalized = username.Trim();
            return await _context.Users.FirstOrDefaultAsync(u => u.Username == normalized);
        }

        public async Task<User?> GetByAccountIdAsync(Guid accountId)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.AccountId == accountId);
        }

        public async Task<bool> RestoreAsync(Guid id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return false;
            user.DeletedAt = null;
            user.UpdatedAt = DateTime.UtcNow;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> SoftDeleteAsync(Guid id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return false;
            user.DeletedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<User?> UpdateAsync(Guid id, User user)
        {
            var existing = await _context.Users.FindAsync(id);
            if (existing == null) return null;

            existing.Username = user.Username.Trim();
            existing.FullName = user.FullName;
            existing.AvatarUrl = user.AvatarUrl;
            existing.Bio = user.Bio;
            existing.PreferredLanguage = user.PreferredLanguage;
            existing.Timezone = user.Timezone;
            existing.UpdatedAt = DateTime.UtcNow;

            _context.Users.Update(existing);
            await _context.SaveChangesAsync();
            return existing;
        }
    }
}
