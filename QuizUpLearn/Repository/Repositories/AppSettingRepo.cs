using Microsoft.EntityFrameworkCore;
using Repository.DBContext;
using Repository.Entities;
using Repository.Interfaces;

namespace Repository.Repositories
{
    public class AppSettingRepo : IAppSettingRepo
    {
        private readonly MyDbContext _context;

        public AppSettingRepo(MyDbContext context)
        {
            _context = context;
        }

        public async Task<AppSetting> CreateAsync(AppSetting appSetting)
        {
            _context.Set<AppSetting>().Add(appSetting);
            await _context.SaveChangesAsync();
            return appSetting;
        }

        public async Task<AppSetting?> GetByKeyAsync(string key)
        {
            return await _context.Set<AppSetting>().FindAsync(key);
        }

        public async Task<IEnumerable<AppSetting>> GetAllAsync()
        {
            return await _context.Set<AppSetting>().ToListAsync();
        }

        public async Task<AppSetting?> UpdateAsync(string key, AppSetting appSetting)
        {
            var existing = await _context.Set<AppSetting>().FindAsync(key);
            if (existing == null) return null;
            
            existing.Value = appSetting.Value;
            _context.Set<AppSetting>().Update(existing);
            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteAsync(string key)
        {
            var entity = await _context.Set<AppSetting>().FindAsync(key);
            if (entity == null) return false;
            _context.Set<AppSetting>().Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}