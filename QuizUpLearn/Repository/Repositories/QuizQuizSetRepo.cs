using Microsoft.EntityFrameworkCore;
using Repository.DBContext;
using Repository.Entities;
using Repository.Interfaces;

namespace Repository.Repositories
{
    public class QuizQuizSetRepo : IQuizQuizSetRepo
    {
        private readonly MyDbContext _context;

        public QuizQuizSetRepo(MyDbContext context)
        {
            _context = context;
        }

        public async Task<QuizQuizSet> CreateAsync(QuizQuizSet entity)
        {
            await _context.QuizQuizSets.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<QuizQuizSet?> GetByIdAsync(Guid id)
        {
            return await _context.QuizQuizSets
                .Include(qq => qq.Quiz)
                    .ThenInclude(q => q.AnswerOptions)
                .Include(qq => qq.Quiz)
                    .ThenInclude(q => q.QuizGroupItem)
                .Include(qq => qq.QuizSet)
                .FirstOrDefaultAsync(qq => qq.Id == id && qq.DeletedAt == null);
        }

        public async Task<IEnumerable<QuizQuizSet>> GetAllAsync(bool includeDeleted = false)
        {
            var query = _context.QuizQuizSets
                .Include(qq => qq.Quiz)
                    .ThenInclude(q => q.AnswerOptions)
                .Include(qq => qq.Quiz)
                    .ThenInclude(q => q.QuizGroupItem)
                .Include(qq => qq.QuizSet)
                .AsQueryable();

            if (!includeDeleted)
            {
                query = query.Where(qq => qq.DeletedAt == null);
            }

            return await query.ToListAsync();
        }

        public async Task<IEnumerable<QuizQuizSet>> GetByQuizIdAsync(Guid quizId, bool includeDeleted = false)
        {
            var query = _context.QuizQuizSets
                .Include(qq => qq.Quiz)
                    .ThenInclude(q => q.AnswerOptions)
                .Include(qq => qq.Quiz)
                    .ThenInclude(q => q.QuizGroupItem)
                .Include(qq => qq.QuizSet)
                .Where(qq => qq.QuizId == quizId);

            if (!includeDeleted)
            {
                query = query.Where(qq => qq.DeletedAt == null);
            }

            return await query.ToListAsync();
        }

        public async Task<IEnumerable<QuizQuizSet>> GetByQuizSetIdAsync(Guid quizSetId, bool includeDeleted = false)
        {
            var query = _context.QuizQuizSets
                .Include(qq => qq.Quiz)
                    .ThenInclude(q => q.AnswerOptions)
                .Include(qq => qq.Quiz)
                    .ThenInclude(q => q.QuizGroupItem)
                .Include(qq => qq.QuizSet)
                .Where(qq => qq.QuizSetId == quizSetId);

            if (!includeDeleted)
            {
                query = query.Where(qq => qq.DeletedAt == null);
            }

            return await query.ToListAsync();
        }

        public async Task<QuizQuizSet?> GetByQuizAndQuizSetAsync(Guid quizId, Guid quizSetId, bool includeDeleted = false)
        {
            var query = _context.QuizQuizSets
                .Include(qq => qq.Quiz)
                    .ThenInclude(q => q.AnswerOptions)
                .Include(qq => qq.Quiz)
                    .ThenInclude(q => q.QuizGroupItem)
                .Include(qq => qq.QuizSet)
                .Where(qq => qq.QuizId == quizId && qq.QuizSetId == quizSetId);

            if (!includeDeleted)
            {
                query = query.Where(qq => qq.DeletedAt == null);
            }

            return await query.FirstOrDefaultAsync();
        }

        public async Task<QuizQuizSet?> UpdateAsync(Guid id, QuizQuizSet entity)
        {
            var existing = await _context.QuizQuizSets.FirstOrDefaultAsync(qq => qq.Id == id && qq.DeletedAt == null);
            if (existing == null) return null;

            existing.QuizId = entity.QuizId;
            existing.QuizSetId = entity.QuizSetId;
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> SoftDeleteAsync(Guid id)
        {
            var entity = await _context.QuizQuizSets.FirstOrDefaultAsync(qq => qq.Id == id && qq.DeletedAt == null);
            if (entity == null) return false;

            entity.DeletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> HardDeleteAsync(Guid id)
        {
            var entity = await _context.QuizQuizSets.FirstOrDefaultAsync(qq => qq.Id == id);
            if (entity == null) return false;

            _context.QuizQuizSets.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> IsExistedAsync(Guid quizId, Guid quizSetId)
        {
            return await _context.QuizQuizSets
                .AnyAsync(qq => qq.QuizId == quizId && qq.QuizSetId == quizSetId && qq.DeletedAt == null);
        }

        public async Task<int> GetQuizCountByQuizSetAsync(Guid quizSetId)
        {
            return await _context.QuizQuizSets
                .CountAsync(qq => qq.QuizSetId == quizSetId && qq.DeletedAt == null);
        }

        public async Task<bool> DeleteByQuizIdAsync(Guid quizId)
        {
            var entities = await _context.QuizQuizSets
                .Where(qq => qq.QuizId == quizId && qq.DeletedAt == null)
                .ToListAsync();

            if (!entities.Any()) return false;

            foreach (var entity in entities)
            {
                entity.DeletedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteByQuizSetIdAsync(Guid quizSetId)
        {
            var entities = await _context.QuizQuizSets
                .Where(qq => qq.QuizSetId == quizSetId && qq.DeletedAt == null)
                .ToListAsync();

            if (!entities.Any()) return false;

            foreach (var entity in entities)
            {
                entity.DeletedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task AddRangeAsync(IEnumerable<QuizQuizSet> entities)
        {
            await _context.QuizQuizSets.AddRangeAsync(entities);
            await _context.SaveChangesAsync();
        }
    }
}