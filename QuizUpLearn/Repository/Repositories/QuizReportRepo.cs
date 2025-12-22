using Microsoft.EntityFrameworkCore;
using Repository.DBContext;
using Repository.Entities;
using Repository.Interfaces;

namespace Repository.Repositories
{
    public class QuizReportRepo : IQuizReportRepo
    {
        private readonly MyDbContext _context;

        public QuizReportRepo(MyDbContext context)
        {
            _context = context;
        }

        public async Task<QuizReport> CreateAsync(QuizReport entity)
        {
            if (entity.QuizId == Guid.Empty)
                throw new ArgumentException("QuizId is required");

            var quizExists = await _context.Quizzes
                .AnyAsync(q => q.Id == entity.QuizId);

            if (!quizExists)
                throw new ArgumentException("Quiz not found");

            await _context.QuizReports.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<QuizReport?> GetByIdAsync(Guid id)
        {
            return await _context.QuizReports
                .Include(qr => qr.User)
                .Include(qr => qr.Quiz)
                .FirstOrDefaultAsync(qr => qr.Id == id && qr.DeletedAt == null);
        }

        public async Task<IEnumerable<QuizReport>> GetAllAsync(bool includeDeleted = false)
        {
            var query = _context.QuizReports
                .Include(qr => qr.User)
                .Include(qr => qr.Quiz)
                .AsQueryable();

            if (!includeDeleted)
            {
                query = query.Where(qr => qr.DeletedAt == null);
            }

            return await query.ToListAsync();
        }

        public async Task<IEnumerable<QuizReport>> GetByUserIdAsync(Guid userId, bool includeDeleted = false)
        {
            var query = _context.QuizReports
                .Include(qr => qr.User)
                .Include(qr => qr.Quiz)
                .Where(qr => qr.UserId == userId);

            if (!includeDeleted)
            {
                query = query.Where(qr => qr.DeletedAt == null);
            }

            return await query.ToListAsync();
        }

        public async Task<IEnumerable<QuizReport>> GetByQuizIdAsync(Guid quizId, bool includeDeleted = false)
        {
            var query = _context.QuizReports
                .Include(qr => qr.User)
                .Include(qr => qr.Quiz)
                .Where(qr => qr.QuizId == quizId);

            if (!includeDeleted)
            {
                query = query.Where(qr => qr.DeletedAt == null);
            }

            return await query.ToListAsync();
        }

        public async Task<QuizReport?> GetByUserAndQuizAsync(Guid userId, Guid quizId, bool includeDeleted = false)
        {
            var query = _context.QuizReports
                .Include(qr => qr.User)
                .Include(qr => qr.Quiz)
                .Where(qr => qr.UserId == userId && qr.QuizId == quizId);

            if (!includeDeleted)
            {
                query = query.Where(qr => qr.DeletedAt == null);
            }

            return await query.FirstOrDefaultAsync();
        }

        public async Task<bool> HardDeleteAsync(Guid id)
        {
            var entity = await _context.QuizReports.FirstOrDefaultAsync(qr => qr.Id == id);
            if (entity == null) return false;

            _context.QuizReports.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> IsExistAsync(Guid userId, Guid quizId)
        {
            return await _context.QuizReports
                .AnyAsync(qr => qr.UserId == userId && qr.QuizId == quizId && qr.DeletedAt == null);
        }
    }
}
