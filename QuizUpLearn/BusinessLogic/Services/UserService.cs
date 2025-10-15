using AutoMapper;
using BusinessLogic.DTOs;
using BusinessLogic.Interfaces;
using Repository.Entities;
using Repository.Interfaces;

namespace BusinessLogic.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepo _repo;
        private readonly IMapper _mapper;

        public UserService(IUserRepo repo, IMapper mapper)
        {
            _repo = repo;
            _mapper = mapper;
        }

        public async Task<ResponseUserDto> CreateAsync(RequestUserDto dto)
        {
            var entity = _mapper.Map<User>(dto);
            var created = await _repo.CreateAsync(entity);
            return _mapper.Map<ResponseUserDto>(created);
        }

        public async Task<IEnumerable<ResponseUserDto>> GetAllAsync(bool includeDeleted = false)
        {
            var list = await _repo.GetAllAsync(includeDeleted);
            return _mapper.Map<IEnumerable<ResponseUserDto>>(list);
        }

        public async Task<ResponseUserDto?> GetByIdAsync(Guid id)
        {
            var entity = await _repo.GetByIdAsync(id);
            return entity == null ? null : _mapper.Map<ResponseUserDto>(entity);
        }

        public async Task<ResponseUserDto?> GetByUsernameAsync(string username)
        {
            var entity = await _repo.GetByUsernameAsync(username);
            return entity == null ? null : _mapper.Map<ResponseUserDto>(entity);
        }

        public async Task<ResponseUserDto?> GetByAccountIdAsync(Guid accountId)
        {
            var entity = await _repo.GetByAccountIdAsync(accountId);
            return entity == null ? null : _mapper.Map<ResponseUserDto>(entity);
        }

        public async Task<bool> RestoreAsync(Guid id)
        {
            return await _repo.RestoreAsync(id);
        }

        public async Task<bool> SoftDeleteAsync(Guid id)
        {
            return await _repo.SoftDeleteAsync(id);
        }

        public async Task<ResponseUserDto?> UpdateAsync(Guid id, RequestUserDto dto)
        {
            var entity = _mapper.Map<User>(dto);
            var updated = await _repo.UpdateAsync(id, entity);
            return updated == null ? null : _mapper.Map<ResponseUserDto>(updated);
        }
    }
}
