using Microsoft.EntityFrameworkCore;
using Repository.DBContext;
using Repository.Entities;
using Repository.Interfaces;

namespace Repository.Repositories
{
    public class UserReportRepo : IUserReportRepo
    {
        private readonly MyDbContext _context;

        public UserReportRepo(MyDbContext context)
        {
            _context = context;
        }

        public async Task<UserReport> CreateAsync(UserReport entity)
        {
            if (entity.UserId == Guid.Empty)
                throw new ArgumentException("UserId is required");

            var userExists = await _context.Users
                .AnyAsync(u => u.Id == entity.UserId);

            if (!userExists)
                throw new ArgumentException("User not found");

            // Validate CommentId if provided
            if (entity.CommentId.HasValue && entity.CommentId.Value != Guid.Empty)
            {
                var commentExists = await _context.QuizSetComments
                    .AnyAsync(c => c.Id == entity.CommentId.Value);

                if (!commentExists)
                    throw new ArgumentException("Comment not found");
            }

            await _context.UserReports.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<UserReport?> GetByIdAsync(Guid id)
        {
            return await _context.UserReports
                .Include(ur => ur.User)
                .FirstOrDefaultAsync(ur => ur.Id == id && ur.DeletedAt == null);
        }

        public async Task<IEnumerable<UserReport>> GetAllAsync(bool includeDeleted = false)
        {
            var query = _context.UserReports
                .Include(ur => ur.User)
                .AsQueryable();

            if (!includeDeleted)
            {
                query = query.Where(ur => ur.DeletedAt == null);
            }

            return await query.ToListAsync();
        }

        public async Task<IEnumerable<UserReport>> GetByUserIdAsync(Guid userId, bool includeDeleted = false)
        {
            var query = _context.UserReports
                .Include(ur => ur.User)
                .Where(ur => ur.UserId == userId);

            if (!includeDeleted)
            {
                query = query.Where(ur => ur.DeletedAt == null);
            }

            return await query.ToListAsync();
        }

        public async Task<bool> HardDeleteAsync(Guid id)
        {
            var entity = await _context.UserReports.FirstOrDefaultAsync(ur => ur.Id == id);
            if (entity == null) return false;

            _context.UserReports.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> IsExistAsync(Guid userId)
        {
            return await _context.UserReports
                .AnyAsync(ur => ur.UserId == userId && ur.DeletedAt == null);
        }
    }
}


