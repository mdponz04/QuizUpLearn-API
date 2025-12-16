using Microsoft.EntityFrameworkCore;
using Repository.DBContext;
using Repository.Entities;
using Repository.Interfaces;

namespace Repository.Repositories
{
    public class UserQuizSetFavoriteRepo : IUserQuizSetFavoriteRepo
    {
        private readonly MyDbContext _context;

        public UserQuizSetFavoriteRepo(MyDbContext context)
        {
            _context = context;
        }

        public async Task<UserQuizSetFavorite> CreateAsync(UserQuizSetFavorite entity)
        {
            await _context.UserQuizSetFavorites.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<UserQuizSetFavorite?> GetByIdAsync(Guid id)
        {
            return await _context.UserQuizSetFavorites
                .Include(uqsf => uqsf.User)
                .Include(uqsf => uqsf.QuizSet)
                .FirstOrDefaultAsync(uqsf => uqsf.Id == id && uqsf.DeletedAt == null);
        }

        public async Task<IEnumerable<UserQuizSetFavorite>> GetAllAsync(bool includeDeleted = false)
        {
            var query = _context.UserQuizSetFavorites
                .Include(uqsf => uqsf.User)
                .Include(uqsf => uqsf.QuizSet)
                .AsQueryable();

            if (!includeDeleted)
            {
                query = query.Where(uqsf => uqsf.DeletedAt == null);
            }

            return await query.ToListAsync();
        }

        public async Task<IEnumerable<UserQuizSetFavorite>> GetByUserIdAsync(Guid userId, bool includeDeleted = false)
        {
            var query = _context.UserQuizSetFavorites
                .Include(uqsf => uqsf.User)
                .Include(uqsf => uqsf.QuizSet)
                .Where(uqsf => uqsf.UserId == userId);

            if (!includeDeleted)
            {
                query = query.Where(uqsf => uqsf.DeletedAt == null);
            }

            return await query.ToListAsync();
        }

        public async Task<IEnumerable<UserQuizSetFavorite>> GetByQuizSetIdAsync(Guid quizSetId, bool includeDeleted = false)
        {
            var query = _context.UserQuizSetFavorites
                .Include(uqsf => uqsf.User)
                .Include(uqsf => uqsf.QuizSet)
                .Where(uqsf => uqsf.QuizSetId == quizSetId);

            if (!includeDeleted)
            {
                query = query.Where(uqsf => uqsf.DeletedAt == null);
            }

            return await query.ToListAsync();
        }

        public async Task<UserQuizSetFavorite?> GetByUserAndQuizSetAsync(Guid userId, Guid quizSetId, bool includeDeleted = false)
        {
            var query = _context.UserQuizSetFavorites
                .Include(uqsf => uqsf.User)
                .Include(uqsf => uqsf.QuizSet)
                .Where(uqsf => uqsf.UserId == userId && uqsf.QuizSetId == quizSetId);

            if (!includeDeleted)
            {
                query = query.Where(uqsf => uqsf.DeletedAt == null);
            }

            return await query.FirstOrDefaultAsync();
        }

        public async Task<bool> HardDeleteAsync(Guid id)
        {
            var entity = await _context.UserQuizSetFavorites.FirstOrDefaultAsync(uqsf => uqsf.Id == id);
            if (entity == null) return false;

            _context.UserQuizSetFavorites.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> IsExistAsync(Guid userId, Guid quizSetId)
        {
            return await _context.UserQuizSetFavorites
                .AnyAsync(uqsf => uqsf.UserId == userId && uqsf.QuizSetId == quizSetId && uqsf.DeletedAt == null);
        }

        public async Task<int> GetFavoriteCountByQuizSetAsync(Guid quizSetId)
        {
            return await _context.UserQuizSetFavorites
                .CountAsync(uqsf => uqsf.QuizSetId == quizSetId && uqsf.DeletedAt == null);
        }
    }
}
