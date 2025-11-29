using AutoMapper;
using BusinessLogic.DTOs;
using BusinessLogic.Interfaces;
using Repository.Entities;
using Repository.Interfaces;

namespace BusinessLogic.Services
{
    public class AppSettingService : IAppSettingService
    {
        private readonly IAppSettingRepo _appSettingRepo;
        private readonly IMapper _mapper;

        public AppSettingService(IAppSettingRepo appSettingRepo, IMapper mapper)
        {
            _appSettingRepo = appSettingRepo;
            _mapper = mapper;
        }

        public async Task<AppSettingDto> CreateAsync(AppSettingDto appSetting)
        {
            return _mapper.Map<AppSettingDto>(await _appSettingRepo.CreateAsync(_mapper.Map<AppSetting>(appSetting)));
        }

        public async Task<bool> DeleteAsync(string key)
        {
            return await _appSettingRepo.DeleteAsync(key);
        }

        public async Task<IEnumerable<AppSettingDto>> GetAllAsync()
        {
            return _mapper.Map<IEnumerable<AppSettingDto>>(await _appSettingRepo.GetAllAsync());
        }

        public async Task<AppSettingDto?> GetByKeyAsync(string key)
        {
            return _mapper.Map<AppSettingDto?>(await _appSettingRepo.GetByKeyAsync(key));
        }

        public async Task<AppSettingDto?> UpdateAsync(string key, AppSettingDto appSetting)
        {
            return _mapper.Map<AppSettingDto?>(await _appSettingRepo.UpdateAsync(key, _mapper.Map<AppSetting>(appSetting)));
        }
    }
}