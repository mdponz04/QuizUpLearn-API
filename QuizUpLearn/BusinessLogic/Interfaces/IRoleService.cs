using BusinessLogic.DTOs;

namespace BusinessLogic.Interfaces
{
    public interface IRoleService
    {
        Task<ResponseRoleDto> CreateRoleAsync(RequestRoleDto roleDto);
        Task<ResponseRoleDto> GetRoleByIdAsync(int id);
        Task<IEnumerable<ResponseRoleDto>> GetAllRolesAsync(bool includeDeleted = false);
        Task<ResponseRoleDto> UpdateRoleAsync(int id, RequestRoleDto roleDto);
        Task<bool> SoftDeleteRoleAsync(int id);
        Task<bool> RestoreRoleAsync(int id);
    }
}
