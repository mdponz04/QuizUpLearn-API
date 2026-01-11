using AutoMapper;
using BusinessLogic.DTOs.UserNotificationDtos;
using BusinessLogic.Interfaces;
using Repository.Entities;
using Repository.Interfaces;

namespace BusinessLogic.Services
{
    public class UserNotificationService : IUserNotificationService
    {
        private readonly IUserNotificationRepo _userNotificationRepo;
        private readonly IMapper _mapper;

        public UserNotificationService(IUserNotificationRepo userNotificationRepo, IMapper mapper)
        {
            _userNotificationRepo = userNotificationRepo;
            _mapper = mapper;
        }

        public async Task<IEnumerable<UserNotificationResponseDto>> GetAllAsync()
        {
            var userNotifications = await _userNotificationRepo.GetAllAsync();
            return _mapper.Map<IEnumerable<UserNotificationResponseDto>>(userNotifications);
        }

        public async Task<UserNotificationResponseDto?> GetByIdAsync(Guid id)
        {
            if(id == Guid.Empty)
                throw new ArgumentException("ID cannot be empty");

            var userNotification = await _userNotificationRepo.GetByIdAsync(id);
            return userNotification == null ? null : _mapper.Map<UserNotificationResponseDto>(userNotification);
        }

        public async Task<IEnumerable<UserNotificationResponseDto>> GetByUserIdAsync(Guid userId)
        {
            var userNotifications = await _userNotificationRepo.GetByUserIdAsync(userId);
            return _mapper.Map<IEnumerable<UserNotificationResponseDto>>(userNotifications);
        }

        public async Task<IEnumerable<UserNotificationResponseDto>> GetUnreadByUserIdAsync(Guid userId)
        {
            var userNotifications = await _userNotificationRepo.GetUnreadByUserIdAsync(userId);
            return _mapper.Map<IEnumerable<UserNotificationResponseDto>>(userNotifications);
        }

        public async Task<UserNotificationResponseDto> CreateAsync(UserNotificationRequestDto requestDto)
        {
            if(requestDto == null)
                throw new ArgumentNullException(nameof(requestDto), "Request DTO cannot be null");

            var userNotification = _mapper.Map<UserNotification>(requestDto);
            var createdUserNotification = await _userNotificationRepo.CreateAsync(userNotification);
            return _mapper.Map<UserNotificationResponseDto>(createdUserNotification);
        }

        public async Task<UserNotificationResponseDto> UpdateAsync(Guid id, UserNotificationRequestDto requestDto)
        {
            var userNotification = _mapper.Map<UserNotification>(requestDto);
            userNotification.Id = id;
            var updatedUserNotification = await _userNotificationRepo.UpdateAsync(userNotification);
            return _mapper.Map<UserNotificationResponseDto>(updatedUserNotification);
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            return await _userNotificationRepo.DeleteAsync(id);
        }

        public async Task<bool> MarkAsReadAsync(Guid id)
        {
            return await _userNotificationRepo.MarkAsReadAsync(id);
        }

        public async Task<bool> MarkAllAsReadByUserIdAsync(Guid userId)
        {
            return await _userNotificationRepo.MarkAllAsReadByUserIdAsync(userId);
        }
    }
}
