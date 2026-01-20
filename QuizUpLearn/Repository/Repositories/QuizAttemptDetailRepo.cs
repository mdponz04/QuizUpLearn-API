using Microsoft.EntityFrameworkCore;
using Repository.DBContext;
using Repository.Entities;
using Repository.Interfaces;

namespace Repository.Repositories
{
    public class QuizAttemptDetailRepo : IQuizAttemptDetailRepo
    {
        private readonly MyDbContext _context;

        public QuizAttemptDetailRepo(MyDbContext context)
        {
            _context = context;
        }

        public async Task<QuizAttemptDetail> CreateAsync(QuizAttemptDetail quizAttemptDetail)
        {
            // Set the foreign key properties based on the provided IDs
            quizAttemptDetail.QuizAttemptId = quizAttemptDetail.AttemptId;
            quizAttemptDetail.QuizId = quizAttemptDetail.QuestionId;
            
            await _context.QuizAttemptDetails.AddAsync(quizAttemptDetail);
            await _context.SaveChangesAsync();
            return quizAttemptDetail;
        }

        public async Task<int> CreateBatchAsync(IEnumerable<QuizAttemptDetail> quizAttemptDetails)
        {
            var detailsList = quizAttemptDetails.ToList();
            foreach (var detail in detailsList)
            {
                // Set the foreign key properties based on the provided IDs
                detail.QuizAttemptId = detail.AttemptId;
                detail.QuizId = detail.QuestionId;
            }
            
            await _context.QuizAttemptDetails.AddRangeAsync(detailsList);
            return await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<QuizAttemptDetail>> GetAllAsync(bool includeDeleted = false)
        {
            return await _context.QuizAttemptDetails
                .AsQueryable()
                .Where(qad => includeDeleted || qad.DeletedAt == null)
                .Include(qad => qad.QuizAttempt)
                .Include(qad => qad.Quiz)
                .ToListAsync();
        }

        public async Task<QuizAttemptDetail?> GetByIdAsync(Guid id)
        {
            return await _context.QuizAttemptDetails
                .Include(qad => qad.QuizAttempt)
                .Include(qad => qad.Quiz)
                .FirstOrDefaultAsync(qad => qad.Id == id);
        }

        public async Task<IEnumerable<QuizAttemptDetail>> GetByAttemptIdAsync(Guid attemptId, bool includeDeleted = false)
        {
            return await _context.QuizAttemptDetails
                .AsQueryable()
                .Where(qad => qad.QuizAttemptId == attemptId && (includeDeleted || qad.DeletedAt == null))
                .Include(qad => qad.Quiz)
                .OrderBy(qad => qad.CreatedAt)
                .ToListAsync();
        }

        public async Task<(IEnumerable<QuizAttemptDetail> details, int totalCount)> GetByAttemptIdPagedAsync(
            Guid attemptId, 
            int page = 1, 
            int pageSize = 10, 
            bool includeDeleted = false)
        {
            var query = _context.QuizAttemptDetails
                .AsQueryable()
                .Where(qad => qad.QuizAttemptId == attemptId && (includeDeleted || qad.DeletedAt == null))
                .Include(qad => qad.Quiz)
                    .ThenInclude(q => q!.QuizGroupItem)
                .Include(qad => qad.Quiz)
                    .ThenInclude(q => q!.AnswerOptions)
                .Include(qad => qad.QuizAttempt)
                    .ThenInclude(qa => qa!.QuizSet)
                .OrderBy(qad => qad.CreatedAt);

            var totalCount = await query.CountAsync();
            
            var details = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (details, totalCount);
        }

        public async Task<bool> RestoreAsync(Guid id)
        {
            var quizAttemptDetail = await _context.QuizAttemptDetails.FindAsync(id);
            if (quizAttemptDetail == null) return false;
            quizAttemptDetail.DeletedAt = null;
            _context.QuizAttemptDetails.Update(quizAttemptDetail);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> SoftDeleteAsync(Guid id)
        {
            var quizAttemptDetail = await _context.QuizAttemptDetails.FindAsync(id);
            if (quizAttemptDetail == null) return false;
            quizAttemptDetail.DeletedAt = DateTime.UtcNow;
            _context.QuizAttemptDetails.Update(quizAttemptDetail);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<QuizAttemptDetail?> UpdateAsync(Guid id, QuizAttemptDetail quizAttemptDetail)
        {
            var existing = await _context.QuizAttemptDetails.FindAsync(id);
            if (existing == null) return null;

            existing.AttemptId = quizAttemptDetail.AttemptId;
            existing.QuestionId = quizAttemptDetail.QuestionId;
            existing.UserAnswer = quizAttemptDetail.UserAnswer;
            existing.IsCorrect = quizAttemptDetail.IsCorrect;
            existing.TimeSpent = quizAttemptDetail.TimeSpent;
            existing.UpdatedAt = DateTime.UtcNow;
            
            // Update foreign key properties
            existing.QuizAttemptId = quizAttemptDetail.AttemptId;
            existing.QuizId = quizAttemptDetail.QuestionId;

            _context.QuizAttemptDetails.Update(existing);
            await _context.SaveChangesAsync();
            return existing;
        }
    }
}
