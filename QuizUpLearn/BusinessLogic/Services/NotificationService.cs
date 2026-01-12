using AutoMapper;
using BusinessLogic.DTOs.NotificationDtos;
using BusinessLogic.Interfaces;
using Repository.Entities;
using Repository.Interfaces;

namespace BusinessLogic.Services
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepo _notificationRepo;
        private readonly IMapper _mapper;

        public NotificationService(INotificationRepo notificationRepo, IMapper mapper)
        {
            _notificationRepo = notificationRepo;
            _mapper = mapper;
        }

        public async Task<IEnumerable<NotificationResponseDto>> GetAllAsync()
        {
            var notifications = await _notificationRepo.GetAllAsync();
            return _mapper.Map<IEnumerable<NotificationResponseDto>>(notifications);
        }

        public async Task<NotificationResponseDto?> GetByIdAsync(Guid id)
        {
            var notification = await _notificationRepo.GetByIdAsync(id);
            return notification == null ? null : _mapper.Map<NotificationResponseDto>(notification);
        }

        public async Task<NotificationResponseDto> CreateAsync(NotificationRequestDto requestDto)
        {
            var notification = _mapper.Map<Notification>(requestDto);
            var createdNotification = await _notificationRepo.CreateAsync(notification);
            return _mapper.Map<NotificationResponseDto>(createdNotification);
        }

        public async Task<NotificationResponseDto> UpdateAsync(Guid id, NotificationRequestDto requestDto)
        {
            if(id == Guid.Empty)
                throw new ArgumentException("Id cannot be empty");
            var notification = _mapper.Map<Notification>(requestDto);
            notification.Id = id;
            var updatedNotification = await _notificationRepo.UpdateAsync(notification);
            return _mapper.Map<NotificationResponseDto>(updatedNotification);
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            return await _notificationRepo.DeleteAsync(id);
        }
    }
}
