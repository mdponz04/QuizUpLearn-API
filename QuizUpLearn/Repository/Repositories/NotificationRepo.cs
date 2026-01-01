using Microsoft.EntityFrameworkCore;
using Repository.DBContext;
using Repository.Entities;
using Repository.Interfaces;

namespace Repository.Repositories
{
    public class NotificationRepo : INotificationRepo
    {
        private readonly MyDbContext _context;

        public NotificationRepo(MyDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Notification>> GetAllAsync()
        {
            return await _context.Notifications
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }

        public async Task<Notification?> GetByIdAsync(Guid id)
        {
            return await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == id);
        }

        public async Task<Notification> CreateAsync(Notification notification)
        {
            notification.Id = Guid.NewGuid();
            notification.CreatedAt = DateTime.UtcNow;
            
            await _context.Notifications.AddAsync(notification);
            await _context.SaveChangesAsync();
            
            return await GetByIdAsync(notification.Id) ?? notification;
        }

        public async Task<Notification> UpdateAsync(Notification notification)
        {
            var existingNotification = await _context.Notifications.FindAsync(notification.Id);
            if (existingNotification == null)
            {
                throw new KeyNotFoundException("Notification not found");
            }
            if (!string.IsNullOrEmpty(notification.Title))
                existingNotification.Title = notification.Title;
            if(!string.IsNullOrEmpty(notification.Message))
                existingNotification.Message = notification.Message;

            existingNotification.Type = notification.Type;
            existingNotification.ActionUrl = notification.ActionUrl;
            existingNotification.ImageUrl = notification.ImageUrl;
            existingNotification.Metadata = notification.Metadata;
            existingNotification.ScheduledAt = notification.ScheduledAt;
            existingNotification.ExpiresAt = notification.ExpiresAt;
            existingNotification.UpdatedAt = DateTime.UtcNow;

            _context.Notifications.Update(existingNotification);
            await _context.SaveChangesAsync();
            
            return await GetByIdAsync(existingNotification.Id) ?? existingNotification;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification == null)
            {
                return false;
            }

            _context.Notifications.Remove(notification);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
