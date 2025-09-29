using Repository.Entities;

namespace Repository.Interfaces
{
    public interface IRoleRepo
    {
        Task<Role> CreateRoleAsync(Role role);
        Task<Role> GetRoleByIdAsync(Guid id);
        Task<IEnumerable<Role>> GetAllRolesAsync(bool includeDeleted = false);
        Task<Role> UpdateRoleAsync(Guid id, Role role);
        Task<bool> SoftDeleteRoleAsync(Guid id);
        Task<bool> RestoreRoleAsync(Guid id);
    }
}
