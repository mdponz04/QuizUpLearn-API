using Microsoft.EntityFrameworkCore;
using Repository.DBContext;
using Repository.Entities;
using Repository.Interfaces;

namespace Repository.Repositories
{
    public class QuizAttemptRepo : IQuizAttemptRepo
    {
        private readonly MyDbContext _context;

        public QuizAttemptRepo(MyDbContext context)
        {
            _context = context;
        }

        public async Task<QuizAttempt> CreateAsync(QuizAttempt quizAttempt)
        {
            await _context.QuizAttempts.AddAsync(quizAttempt);
            await _context.SaveChangesAsync();
            return quizAttempt;
        }

        public async Task<IEnumerable<QuizAttempt>> GetAllAsync(bool includeDeleted = false)
        {
            return await _context.QuizAttempts
                .AsQueryable()
                .Where(qa => includeDeleted || qa.DeletedAt == null)
                .Include(qa => qa.User)
                .Include(qa => qa.QuizSet)
                .ToListAsync();
        }

        public async Task<QuizAttempt?> GetByIdAsync(Guid id)
        {
            return await _context.QuizAttempts
                .Include(qa => qa.User)
                .Include(qa => qa.QuizSet)
                .Include(qa => qa.QuizAttemptDetails)
                .FirstOrDefaultAsync(qa => qa.Id == id);
        }

        public async Task<IEnumerable<QuizAttempt>> GetByUserIdAsync(Guid userId, bool includeDeleted = false)
        {
            return await _context.QuizAttempts
                .AsQueryable()
                .Where(qa => qa.UserId == userId && (includeDeleted || qa.DeletedAt == null))
                .Include(qa => qa.QuizSet)
                .OrderByDescending(qa => qa.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<QuizAttempt>> GetByQuizSetIdAsync(Guid quizSetId, bool includeDeleted = false)
        {
            return await _context.QuizAttempts
                .AsQueryable()
                .Where(qa => qa.QuizSetId == quizSetId && (includeDeleted || qa.DeletedAt == null))
                .Include(qa => qa.User)
                .OrderByDescending(qa => qa.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<QuizAttempt>> GetByQuizSetIdsAsync(IEnumerable<Guid> quizSetIds, bool includeDeleted = false)
        {
            var ids = quizSetIds.Distinct().ToList();
            if (!ids.Any()) return new List<QuizAttempt>();

            return await _context.QuizAttempts
                .AsQueryable()
                .Where(qa => ids.Contains(qa.QuizSetId) && (includeDeleted || qa.DeletedAt == null))
                .Include(qa => qa.User)
                .OrderByDescending(qa => qa.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> RestoreAsync(Guid id)
        {
            var quizAttempt = await _context.QuizAttempts.FindAsync(id);
            if (quizAttempt == null) return false;
            quizAttempt.DeletedAt = null;
            _context.QuizAttempts.Update(quizAttempt);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> SoftDeleteAsync(Guid id)
        {
            var quizAttempt = await _context.QuizAttempts.FindAsync(id);
            if (quizAttempt == null) return false;
            quizAttempt.DeletedAt = DateTime.UtcNow;
            _context.QuizAttempts.Update(quizAttempt);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<QuizAttempt?> UpdateAsync(Guid id, QuizAttempt quizAttempt)
        {
            var existing = await _context.QuizAttempts.FindAsync(id);
            if (existing == null) return null;

            existing.UserId = quizAttempt.UserId;
            existing.QuizSetId = quizAttempt.QuizSetId;
            existing.AttemptType = quizAttempt.AttemptType;
            existing.TotalQuestions = quizAttempt.TotalQuestions;
            existing.CorrectAnswers = quizAttempt.CorrectAnswers;
            existing.WrongAnswers = quizAttempt.WrongAnswers;
            existing.Score = quizAttempt.Score;
            existing.Accuracy = quizAttempt.Accuracy;
            existing.TimeSpent = quizAttempt.TimeSpent;
            existing.OpponentId = quizAttempt.OpponentId;
            existing.IsWinner = quizAttempt.IsWinner;
            existing.Status = quizAttempt.Status;
            existing.UpdatedAt = DateTime.UtcNow;

            _context.QuizAttempts.Update(existing);
            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<(IEnumerable<QuizAttempt> attempts, int totalCount)> GetUserHistoryPagedAsync(
            Guid userId,
            Guid? quizSetId = null,
            string? status = null,
            string? attemptType = null,
            string sortBy = "CreatedAt",
            string sortOrder = "desc",
            int page = 1,
            int pageSize = 10,
            bool includeDeleted = false)
        {
            var query = _context.QuizAttempts
                .AsQueryable()
                .Where(qa => qa.UserId == userId && (includeDeleted || qa.DeletedAt == null));

            // Apply filters
            if (quizSetId.HasValue)
                query = query.Where(qa => qa.QuizSetId == quizSetId.Value);

            if (!string.IsNullOrEmpty(status))
                query = query.Where(qa => qa.Status == status);

            if (!string.IsNullOrEmpty(attemptType))
                query = query.Where(qa => qa.AttemptType == attemptType);

            // Apply sorting
            query = sortBy.ToLower() switch
            {
                "score" => sortOrder.ToLower() == "asc" 
                    ? query.OrderBy(qa => qa.Score) 
                    : query.OrderByDescending(qa => qa.Score),
                "accuracy" => sortOrder.ToLower() == "asc" 
                    ? query.OrderBy(qa => qa.Accuracy) 
                    : query.OrderByDescending(qa => qa.Accuracy),
                _ => sortOrder.ToLower() == "asc" 
                    ? query.OrderBy(qa => qa.CreatedAt) 
                    : query.OrderByDescending(qa => qa.CreatedAt)
            };

            var totalCount = await query.CountAsync();

            var attempts = await query
                .Include(qa => qa.QuizSet)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (attempts, totalCount);
        }

        public async Task<(IEnumerable<QuizAttempt> attempts, int totalCount)> GetPlacementTestHistoryPagedAsync(
            Guid userId,
            Guid? quizSetId = null,
            string? status = null,
            string sortBy = "CreatedAt",
            string sortOrder = "desc",
            int page = 1,
            int pageSize = 10,
            bool includeDeleted = false)
        {
            var query = _context.QuizAttempts
                .AsQueryable()
                .Where(qa => qa.UserId == userId 
                    && qa.AttemptType == "placement" 
                    && (includeDeleted || qa.DeletedAt == null));

            // Apply filters
            if (quizSetId.HasValue)
                query = query.Where(qa => qa.QuizSetId == quizSetId.Value);

            if (!string.IsNullOrEmpty(status))
                query = query.Where(qa => qa.Status == status);

            // Apply sorting
            query = sortBy.ToLower() switch
            {
                "score" => sortOrder.ToLower() == "asc" 
                    ? query.OrderBy(qa => qa.Score) 
                    : query.OrderByDescending(qa => qa.Score),
                "accuracy" => sortOrder.ToLower() == "asc" 
                    ? query.OrderBy(qa => qa.Accuracy) 
                    : query.OrderByDescending(qa => qa.Accuracy),
                _ => sortOrder.ToLower() == "asc" 
                    ? query.OrderBy(qa => qa.CreatedAt) 
                    : query.OrderByDescending(qa => qa.CreatedAt)
            };

            var totalCount = await query.CountAsync();

            // Eager load QuizAttemptDetails và Quiz để tránh N+1 queries
            var attempts = await query
                .Include(qa => qa.QuizSet)
                .Include(qa => qa.QuizAttemptDetails)
                    .ThenInclude(qad => qad.Quiz)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (attempts, totalCount);
        }
    }
}
