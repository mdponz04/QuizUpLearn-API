using Microsoft.EntityFrameworkCore;
using Repository.DBContext;
using Repository.Entities;
using Repository.Interfaces;

namespace Repository.Repositories
{
    public class AccountRepo : IAccountRepo
    {
        private readonly MyDbContext _context;

        public AccountRepo(MyDbContext context)
        {
            _context = context;
        }

        public async Task<Account> CreateAsync(Account account)
        {
            account.Email = account.Email.Trim().ToLowerInvariant();
            // Ensure default Role exists (e.g., "User")
            var defaultRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "User");
            if (defaultRole == null)
            {
                defaultRole = new Role
                {
                    RoleName = "User",
                    DisplayName = "User",
                    Permissions = string.Empty,
                    IsActive = true
                };
                await _context.Roles.AddAsync(defaultRole);
                await _context.SaveChangesAsync();
            }

            // Create minimal User
            var username = account.Email.Contains('@') ? account.Email.Split('@')[0] : account.Email;
            var newUser = new User
            {
                Username = username,
                AvatarUrl = string.Empty,
                FullName = string.Empty,
                LoginStreak = 0,
                TotalPoints = 0
            };
            await _context.Users.AddAsync(newUser);
            await _context.SaveChangesAsync();

            // Complete Account fields
            account.UserId = newUser.Id;
            account.RoleId = defaultRole.Id;
            account.IsActive = true;
            account.IsEmailVerified = true;
            account.IsBanned = false;

            await _context.Accounts.AddAsync(account);
            await _context.SaveChangesAsync();

            // Link back to Account on User
            newUser.AccountId = account.Id;
            _context.Users.Update(newUser);
            await _context.SaveChangesAsync();

            account.User = newUser;
            return account;
        }

        public async Task<IEnumerable<Account>> GetAllAsync(bool includeDeleted = false)
        {
            return await _context.Accounts
                .Include(a => a.User)
                .Where(a => includeDeleted || a.DeletedAt == null)
                .ToListAsync();
        }

        public async Task<Account?> GetByIdAsync(Guid id)
        {
            return await _context.Accounts
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task<Account?> GetByEmailAsync(string email)
        {
            var normalized = email.Trim().ToLowerInvariant();
            return await _context.Accounts
                .Include(a => a.User)
                .Include(a => a.Role)
                .FirstOrDefaultAsync(a => a.Email == normalized);
        }

        public async Task<bool> RestoreAsync(Guid id)
        {
            var account = await _context.Accounts.FindAsync(id);
            if (account == null) return false;
            account.DeletedAt = null;
            account.IsActive = true;
            _context.Accounts.Update(account);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> SoftDeleteAsync(Guid id)
        {
            var account = await _context.Accounts.FindAsync(id);
            if (account == null) return false;
            account.DeletedAt = DateTime.UtcNow;
            account.IsActive = false;
            _context.Accounts.Update(account);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<Account?> UpdateAsync(Guid id, Account account)
        {
            var existing = await _context.Accounts
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.Id == id);
            if (existing == null) return null;

            existing.Email = account.Email;
            existing.PasswordHash = account.PasswordHash;
            existing.UserId = account.UserId;
            existing.RoleId = account.RoleId;
            existing.IsEmailVerified = account.IsEmailVerified;
            existing.LastLoginAt = account.LastLoginAt;
            existing.LoginAttempts = account.LoginAttempts;
            existing.LockoutUntil = account.LockoutUntil;
            existing.IsActive = account.IsActive;
            existing.IsBanned = account.IsBanned;
            existing.UpdatedAt = DateTime.UtcNow;

            _context.Accounts.Update(existing);
            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> UpdatePasswordByEmailAsync(string email, string newPasswordHash)
        {
            var normalized = email.Trim().ToLowerInvariant();
            var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Email == normalized);
            if (account == null) return false;
            account.PasswordHash = newPasswordHash;
            account.UpdatedAt = DateTime.UtcNow;
            _context.Accounts.Update(account);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}


