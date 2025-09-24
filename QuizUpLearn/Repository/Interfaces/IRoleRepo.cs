using Repository.Entities;

namespace Repository.Interfaces
{
    public interface IRoleRepo
    {
        Task<Role> CreateRoleAsync(Role role);
        Task<Role> GetRoleByIdAsync(int id);
        Task<IEnumerable<Role>> GetAllRolesAsync(bool includeDeleted = false);
        Task<Role> UpdateRoleAsync(int id, Role role);
        Task<bool> SoftDeleteRoleAsync(int id);
        Task<bool> RestoreRoleAsync(int id);
    }
}
