using Microsoft.EntityFrameworkCore;
using Repository.DBContext;
using Repository.Entities;
using Repository.Interfaces;

namespace Repository.Repositories
{
    public class UserQuizSetLikeRepo : IUserQuizSetLikeRepo
    {
        private readonly MyDbContext _context;

        public UserQuizSetLikeRepo(MyDbContext context)
        {
            _context = context;
        }

        public async Task<UserQuizSetLike> CreateAsync(UserQuizSetLike entity)
        {
            await _context.UserQuizSetLikes.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<UserQuizSetLike?> GetByIdAsync(Guid id)
        {
            return await _context.UserQuizSetLikes
                .Include(uqsl => uqsl.User)
                .Include(uqsl => uqsl.QuizSet)
                .FirstOrDefaultAsync(uqsl => uqsl.Id == id && uqsl.DeletedAt == null);
        }

        public async Task<IEnumerable<UserQuizSetLike>> GetAllAsync(bool includeDeleted = false)
        {
            var query = _context.UserQuizSetLikes
                .Include(uqsl => uqsl.User)
                .Include(uqsl => uqsl.QuizSet)
                .AsQueryable();

            if (!includeDeleted)
            {
                query = query.Where(uqsl => uqsl.DeletedAt == null);
            }

            return await query.ToListAsync();
        }

        public async Task<IEnumerable<UserQuizSetLike>> GetByUserIdAsync(Guid userId, bool includeDeleted = false)
        {
            var query = _context.UserQuizSetLikes
                .Include(uqsl => uqsl.User)
                .Include(uqsl => uqsl.QuizSet)
                .Where(uqsl => uqsl.UserId == userId);

            if (!includeDeleted)
            {
                query = query.Where(uqsl => uqsl.DeletedAt == null);
            }

            return await query.ToListAsync();
        }

        public async Task<IEnumerable<UserQuizSetLike>> GetByQuizSetIdAsync(Guid quizSetId, bool includeDeleted = false)
        {
            var query = _context.UserQuizSetLikes
                .Include(uqsl => uqsl.User)
                .Include(uqsl => uqsl.QuizSet)
                .Where(uqsl => uqsl.QuizSetId == quizSetId);

            if (!includeDeleted)
            {
                query = query.Where(uqsl => uqsl.DeletedAt == null);
            }

            return await query.ToListAsync();
        }

        public async Task<UserQuizSetLike?> GetByUserAndQuizSetAsync(Guid userId, Guid quizSetId, bool includeDeleted = false)
        {
            var query = _context.UserQuizSetLikes
                .Include(uqsl => uqsl.User)
                .Include(uqsl => uqsl.QuizSet)
                .Where(uqsl => uqsl.UserId == userId && uqsl.QuizSetId == quizSetId);

            if (!includeDeleted)
            {
                query = query.Where(uqsl => uqsl.DeletedAt == null);
            }

            return await query.FirstOrDefaultAsync();
        }

        public async Task<bool> HardDeleteAsync(Guid id)
        {
            var entity = await _context.UserQuizSetLikes.FirstOrDefaultAsync(uqsl => uqsl.Id == id);
            if (entity == null) return false;

            _context.UserQuizSetLikes.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> IsExistAsync(Guid userId, Guid quizSetId)
        {
            return await _context.UserQuizSetLikes
                .AnyAsync(uqsl => uqsl.UserId == userId && uqsl.QuizSetId == quizSetId && uqsl.DeletedAt == null);
        }

        public async Task<int> GetLikeCountByQuizSetAsync(Guid quizSetId)
        {
            return await _context.UserQuizSetLikes
                .CountAsync(uqsl => uqsl.QuizSetId == quizSetId && uqsl.DeletedAt == null);
        }
    }
}
