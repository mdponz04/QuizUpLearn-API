using Microsoft.EntityFrameworkCore;
using Repository.DBContext;
using Repository.Entities;
using Repository.Interfaces;

namespace Repository.Repositories
{
    public class RoleRepo : IRoleRepo
    {
        private readonly MyDbContext _context;

        public RoleRepo(MyDbContext context)
        {
            _context = context;
        }

        public async Task<Role> CreateRoleAsync(Role role)
        {
            await _context.Roles.AddAsync(role);
            await _context.SaveChangesAsync();
            return role;
        }

        public async Task<IEnumerable<Role>> GetAllRolesAsync(bool includeDeleted = false)
        {
            var roleList = await _context.Roles
                .AsQueryable()
                .Where(r => includeDeleted || r.IsActive)
                .ToListAsync();
            if (roleList == null || !roleList.Any())
            {
                return Enumerable.Empty<Role>();
            }
            return roleList;
        }

        public async Task<Role> GetRoleByIdAsync(Guid id)
        {
            return await _context.Roles.FindAsync(id);
        }

        public async Task<bool> RestoreRoleAsync(Guid id)
        {
            var role = await _context.Roles.FindAsync(id);
            if (role == null) return false;
            role.IsActive = true;
            _context.Roles.Update(role);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> SoftDeleteRoleAsync(Guid id)
        {
            var role = await _context.Roles.FindAsync(id);
            if (role == null) return false;
            role.IsActive = false;
            _context.Roles.Update(role);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<Role> UpdateRoleAsync(Guid id, Role role)
        {
            var existingRole = await _context.Roles.FindAsync(id);
            if (existingRole == null) return null;

            if (!string.IsNullOrEmpty(role.RoleName))
                existingRole.RoleName = role.RoleName;
            if (!string.IsNullOrEmpty(role.DisplayName))
                existingRole.DisplayName = role.DisplayName;
            if (!string.IsNullOrEmpty(role.Description))
                existingRole.Description = role.Description;
            if (!string.IsNullOrEmpty(role.Permissions))
                existingRole.Permissions = role.Permissions;
            existingRole.IsActive = role.IsActive;

            _context.Roles.Update(existingRole);
            await _context.SaveChangesAsync();
            return existingRole;
        }
    }
}
