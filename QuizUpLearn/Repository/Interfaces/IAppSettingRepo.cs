using Repository.Entities;

namespace Repository.Interfaces
{
    public interface IAppSettingRepo
    {
        Task<AppSetting> CreateAsync(AppSetting appSetting);
        Task<AppSetting?> GetByKeyAsync(string key);
        Task<IEnumerable<AppSetting>> GetAllAsync();
        Task<AppSetting?> UpdateAsync(string key, AppSetting appSetting);
        Task<bool> DeleteAsync(string key);
    }
}