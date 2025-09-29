using AutoMapper;
using BusinessLogic.DTOs;
using BusinessLogic.Interfaces;
using Repository.Entities;
using Repository.Interfaces;

namespace BusinessLogic.Services
{
    public class RoleService : IRoleService
    {
        private readonly IRoleRepo _repo;
        private readonly IMapper _mapper;

        public RoleService(IRoleRepo repo, IMapper mapper)
        {
            _repo = repo;
            _mapper = mapper;
        }

        public async Task<ResponseRoleDto> CreateRoleAsync(RequestRoleDto roleDto)
        {
            var role = await _repo.CreateRoleAsync(_mapper.Map<Role>(roleDto));
            return _mapper.Map<ResponseRoleDto>(role);
        }

        public async Task<IEnumerable<ResponseRoleDto>> GetAllRolesAsync(bool includeDeleted = false)
        {
            return _mapper.Map<IEnumerable<ResponseRoleDto>>(await _repo.GetAllRolesAsync(includeDeleted));
        }

        public async Task<ResponseRoleDto> GetRoleByIdAsync(Guid id)
        {
            if (!await IsRoleExists(id))
                throw new KeyNotFoundException($"Role with id {id} not found.");
            return _mapper.Map<ResponseRoleDto>(await _repo.GetRoleByIdAsync(id));
        }

        public async Task<bool> RestoreRoleAsync(Guid id)
        {
            if (!IsRoleExists(id).Result)
                throw new KeyNotFoundException($"Role with id {id} not found.");
            return await _repo.RestoreRoleAsync(id);
        }

        public async Task<bool> SoftDeleteRoleAsync(Guid id)
        {
            if(!IsRoleExists(id).Result)
                throw new KeyNotFoundException($"Role with id {id} not found.");
            return await _repo.SoftDeleteRoleAsync(id);
        }

        public async Task<ResponseRoleDto> UpdateRoleAsync(Guid id, RequestRoleDto roleDto)
        {
            if(!IsRoleExists(id).Result)
                throw new KeyNotFoundException($"Role with id {id} not found.");

            var role = await _repo.UpdateRoleAsync(id, _mapper.Map<Role>(roleDto));

            return _mapper.Map<ResponseRoleDto>(role);
        }

        private async Task<bool> IsRoleExists(Guid id)
        {
            var role = await _repo.GetRoleByIdAsync(id);
            if(role != null)
                return true;
            return false;
        }
    }
}
