using BusinessLogic.DTOs;

namespace BusinessLogic.Interfaces
{
    public interface IRoleService
    {
        Task<ResponseRoleDto> CreateRoleAsync(RequestRoleDto roleDto);
        Task<ResponseRoleDto> GetRoleByIdAsync(Guid id);
        Task<IEnumerable<ResponseRoleDto>> GetAllRolesAsync(bool includeDeleted = false);
        Task<ResponseRoleDto> UpdateRoleAsync(Guid id, RequestRoleDto roleDto);
        Task<bool> SoftDeleteRoleAsync(Guid id);
        Task<bool> RestoreRoleAsync(Guid id);
    }
}
