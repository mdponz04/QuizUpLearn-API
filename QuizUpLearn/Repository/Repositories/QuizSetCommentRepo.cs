using Microsoft.EntityFrameworkCore;
using Repository.DBContext;
using Repository.Entities;
using Repository.Interfaces;

namespace Repository.Repositories
{
    public class QuizSetCommentRepo : IQuizSetCommentRepo
    {
        private readonly MyDbContext _context;

        public QuizSetCommentRepo(MyDbContext context)
        {
            _context = context;
        }

        public async Task<QuizSetComment> CreateAsync(QuizSetComment entity)
        {
            await _context.QuizSetComments.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<QuizSetComment?> GetByIdAsync(Guid id)
        {
            return await _context.QuizSetComments
                .Include(qsc => qsc.User)
                .Include(qsc => qsc.QuizSet)
                .FirstOrDefaultAsync(qsc => qsc.Id == id && qsc.DeletedAt == null);
        }

        public async Task<IEnumerable<QuizSetComment>> GetAllAsync(bool includeDeleted = false)
        {
            var query = _context.QuizSetComments
                .Include(qsc => qsc.User)
                .Include(qsc => qsc.QuizSet)
                .AsQueryable();

            if (!includeDeleted)
            {
                query = query.Where(qsc => qsc.DeletedAt == null);
            }

            return await query.ToListAsync();
        }

        public async Task<IEnumerable<QuizSetComment>> GetByUserIdAsync(Guid userId, bool includeDeleted = false)
        {
            var query = _context.QuizSetComments
                .Include(qsc => qsc.User)
                .Include(qsc => qsc.QuizSet)
                .Where(qsc => qsc.UserId == userId);

            if (!includeDeleted)
            {
                query = query.Where(qsc => qsc.DeletedAt == null);
            }

            return await query.ToListAsync();
        }

        public async Task<IEnumerable<QuizSetComment>> GetByQuizSetIdAsync(Guid quizSetId, bool includeDeleted = false)
        {
            var query = _context.QuizSetComments
                .Include(qsc => qsc.User)
                .Include(qsc => qsc.QuizSet)
                .Where(qsc => qsc.QuizSetId == quizSetId);

            if (!includeDeleted)
            {
                query = query.Where(qsc => qsc.DeletedAt == null);
            }

            return await query.ToListAsync();
        }

        public async Task<QuizSetComment?> UpdateAsync(Guid id, QuizSetComment entity)
        {
            var existing = await _context.QuizSetComments.FirstOrDefaultAsync(qsc => qsc.Id == id && qsc.DeletedAt == null);
            if (existing == null) return null;

            existing.Content = entity.Content;
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> HardDeleteAsync(Guid id)
        {
            var entity = await _context.QuizSetComments.FirstOrDefaultAsync(qsc => qsc.Id == id);
            if (entity == null) return false;

            _context.QuizSetComments.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> GetCommentCountByQuizSetAsync(Guid quizSetId)
        {
            return await _context.QuizSetComments
                .CountAsync(qsc => qsc.QuizSetId == quizSetId && qsc.DeletedAt == null);
        }
    }
}
