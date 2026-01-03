using BusinessLogic.DTOs.UserNotificationDtos;

namespace BusinessLogic.Interfaces
{
    public interface IUserNotificationService
    {
        Task<IEnumerable<UserNotificationResponseDto>> GetAllAsync();
        Task<UserNotificationResponseDto?> GetByIdAsync(Guid id);
        Task<IEnumerable<UserNotificationResponseDto>> GetByUserIdAsync(Guid userId);
        Task<IEnumerable<UserNotificationResponseDto>> GetUnreadByUserIdAsync(Guid userId);
        Task<UserNotificationResponseDto> CreateAsync(UserNotificationRequestDto requestDto);
        Task<UserNotificationResponseDto> UpdateAsync(Guid id, UserNotificationRequestDto requestDto);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> MarkAsReadAsync(Guid id);
        Task<bool> MarkAllAsReadByUserIdAsync(Guid userId);
    }
}
