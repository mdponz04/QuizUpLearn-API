using BusinessLogic.DTOs.NotificationDtos;

namespace BusinessLogic.Interfaces
{
    public interface INotificationService
    {
        Task<IEnumerable<NotificationResponseDto>> GetAllAsync();
        Task<NotificationResponseDto?> GetByIdAsync(Guid id);
        Task<NotificationResponseDto> CreateAsync(NotificationRequestDto requestDto);
        Task<NotificationResponseDto> UpdateAsync(Guid id, NotificationRequestDto requestDto);
        Task<bool> DeleteAsync(Guid id);
    }
}
