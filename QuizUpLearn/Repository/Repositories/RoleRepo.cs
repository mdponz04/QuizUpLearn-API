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

        public async Task<Role> GetRoleByIdAsync(int id)
        {
            return await _context.Roles.FindAsync(id);
        }

        public async Task<bool> RestoreRoleAsync(int id)
        {
            var role = await _context.Roles.FindAsync(id);
            if (role == null) return false;
            role.IsActive = true;
            _context.Roles.Update(role);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> SoftDeleteRoleAsync(int id)
        {
            var role = await _context.Roles.FindAsync(id);
            if (role == null) return false;
            role.IsActive = false;
            _context.Roles.Update(role);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<Role> UpdateRoleAsync(int id, Role role)
        {
            var existingRole = await _context.Roles.FindAsync(id);
            if (existingRole == null) return null;
            existingRole = role;
            _context.Roles.Update(existingRole);
            await _context.SaveChangesAsync();
            return existingRole;
        }
    }
}
