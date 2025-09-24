using AutoMapper;
using BusinessLogic.DTOs;
using BusinessLogic.Interfaces;
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
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<ResponseRoleDto>> GetAllRolesAsync(bool includeDeleted = false)
        {
            return _mapper.Map<IEnumerable<ResponseRoleDto>>(await _repo.GetAllRolesAsync(includeDeleted));
        }

        public Task<ResponseRoleDto> GetRoleByIdAsync(int id)
        {
            throw new NotImplementedException();
        }

        public Task<bool> RestoreRoleAsync(int id)
        {
            throw new NotImplementedException();
        }

        public Task<bool> SoftDeleteRoleAsync(int id)
        {
            throw new NotImplementedException();
        }

        public Task<ResponseRoleDto> UpdateRoleAsync(int id, RequestRoleDto roleDto)
        {
            throw new NotImplementedException();
        }
    }
}
