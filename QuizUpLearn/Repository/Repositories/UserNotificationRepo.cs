using Microsoft.EntityFrameworkCore;
using Repository.DBContext;
using Repository.Entities;
using Repository.Interfaces;

namespace Repository.Repositories
{
    public class UserNotificationRepo : IUserNotificationRepo
    {
        private readonly MyDbContext _context;

        public UserNotificationRepo(MyDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<UserNotification>> GetAllAsync()
        {
            return await _context.UserNotifications
                .Include(un => un.User)
                .Include(un => un.Notification)
                .Where(un => un.DeletedAt == null)
                .ToListAsync();
        }

        public async Task<UserNotification?> GetByIdAsync(Guid id)
        {
            return await _context.UserNotifications
                .Include(un => un.User)
                .Include(un => un.Notification)
                .FirstOrDefaultAsync(un => un.Id == id && un.DeletedAt == null);
        }

        public async Task<IEnumerable<UserNotification>> GetByUserIdAsync(Guid userId)
        {
            return await _context.UserNotifications
                .Include(un => un.User)
                .Include(un => un.Notification)
                .Where(un => un.UserId == userId && un.DeletedAt == null)
                .OrderByDescending(un => un.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<UserNotification>> GetUnreadByUserIdAsync(Guid userId)
        {
            return await _context.UserNotifications
                .Include(un => un.User)
                .Include(un => un.Notification)
                .Where(un => un.UserId == userId && !un.IsRead && un.DeletedAt == null)
                .OrderByDescending(un => un.CreatedAt)
                .ToListAsync();
        }

        public async Task<UserNotification> CreateAsync(UserNotification userNotification)
        {
            _context.UserNotifications.Add(userNotification);
            await _context.SaveChangesAsync();
            return await GetByIdAsync(userNotification.Id) ?? userNotification;
        }

        public async Task<UserNotification> UpdateAsync(UserNotification userNotification)
        {
            var existingEntity = await _context.UserNotifications.FindAsync(userNotification.Id);
            if (existingEntity == null)
            {
                throw new KeyNotFoundException($"UserNotification with id {userNotification.Id} not found");
            }

            existingEntity.IsRead = userNotification.IsRead;
            existingEntity.ReadAt = userNotification.ReadAt;
            existingEntity.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return await GetByIdAsync(userNotification.Id) ?? existingEntity;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var userNotification = await _context.UserNotifications.FindAsync(id);
            if (userNotification == null || userNotification.DeletedAt != null)
            {
                return false;
            }

            userNotification.DeletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> MarkAsReadAsync(Guid id)
        {
            var userNotification = await _context.UserNotifications.FindAsync(id);
            if (userNotification == null || userNotification.DeletedAt != null)
            {
                return false;
            }

            userNotification.IsRead = true;
            userNotification.ReadAt = DateTime.UtcNow;
            userNotification.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> MarkAllAsReadByUserIdAsync(Guid userId)
        {
            var unreadNotifications = await _context.UserNotifications
                .Where(un => un.UserId == userId && !un.IsRead && un.DeletedAt == null)
                .ToListAsync();

            if (!unreadNotifications.Any())
            {
                return false;
            }

            var now = DateTime.UtcNow;
            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = true;
                notification.ReadAt = now;
                notification.UpdatedAt = now;
            }

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
