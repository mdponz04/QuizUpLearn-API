using Repository.Entities;

namespace Repository.Interfaces
{
    public interface IUserNotificationRepo
    {
        Task<IEnumerable<UserNotification>> GetAllAsync();
        Task<UserNotification?> GetByIdAsync(Guid id);
        Task<IEnumerable<UserNotification>> GetByUserIdAsync(Guid userId);
        Task<IEnumerable<UserNotification>> GetUnreadByUserIdAsync(Guid userId);
        Task<UserNotification> CreateAsync(UserNotification userNotification);
        Task<UserNotification> UpdateAsync(UserNotification userNotification);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> MarkAsReadAsync(Guid id);
        Task<bool> MarkAllAsReadByUserIdAsync(Guid userId);
    }
}
