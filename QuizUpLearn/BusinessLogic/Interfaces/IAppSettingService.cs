using BusinessLogic.DTOs;

namespace BusinessLogic.Interfaces
{
    public interface IAppSettingService
    {
        Task<AppSettingDto> CreateAsync(AppSettingDto appSetting);
        Task<AppSettingDto?> GetByKeyAsync(string key);
        Task<IEnumerable<AppSettingDto>> GetAllAsync();
        Task<AppSettingDto?> UpdateAsync(string key, AppSettingDto appSetting);
        Task<bool> DeleteAsync(string key);
    }
}