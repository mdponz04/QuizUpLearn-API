using Microsoft.EntityFrameworkCore;
using Repository.DBContext;
using Repository.Entities;
using Repository.Interfaces;
using Repository.Enums;

namespace Repository.Repositories
{
    public class QuizSetRepo : IQuizSetRepo
    {
        private readonly MyDbContext _context;

        public QuizSetRepo(MyDbContext context)
        {
            _context = context;
        }

        public async Task<QuizSet> CreateQuizSetAsync(QuizSet quizSet)
        {
            await _context.QuizSets.AddAsync(quizSet);
            await _context.SaveChangesAsync();
            return quizSet;
        }

        public async Task<QuizSet?> GetQuizSetByIdAsync(Guid id)
        {
            return await _context.QuizSets
                .Include(qs => qs.Creator)
                .FirstOrDefaultAsync(qs => qs.Id == id && qs.DeletedAt == null);
        }

        public async Task<IEnumerable<QuizSet>> GetAllQuizSetsAsync()
        {
            return await _context.QuizSets
                .Include(qs => qs.Creator)
                .ToListAsync();
        }

        public async Task<IEnumerable<QuizSet>> GetQuizSetsByCreatorAsync(Guid creatorId)
        {
            return await _context.QuizSets
                .Include(qs => qs.Creator)
                .Where(qs => qs.CreatedBy == creatorId)
                .ToListAsync();
        }

        public async Task<IEnumerable<QuizSet>> GetPublishedQuizSetsAsync()
        {
            return await _context.QuizSets
                .Include(qs => qs.Creator)
                .Where(qs => qs.IsPublished && qs.DeletedAt == null)
                .ToListAsync();
        }

        public async Task<QuizSet?> UpdateQuizSetAsync(Guid id, QuizSet quizSet)
        {
            var existingQuizSet = await _context.QuizSets.FindAsync(id);
            if (existingQuizSet == null || existingQuizSet.DeletedAt != null)
                return null;

            if(!string.IsNullOrEmpty(quizSet.Title))
                existingQuizSet.Title = quizSet.Title;
            if(!string.IsNullOrEmpty(quizSet.Description))
                existingQuizSet.Description = quizSet.Description;
            if (!string.IsNullOrEmpty(quizSet.QuizSetType.ToString()))
                existingQuizSet.QuizSetType = quizSet.QuizSetType;

            existingQuizSet.IsPublished = quizSet.IsPublished;
            existingQuizSet.IsPremiumOnly = quizSet.IsPremiumOnly;
            
            existingQuizSet.UpdatedAt = DateTime.UtcNow;
            _context.QuizSets.Update(existingQuizSet);
            await _context.SaveChangesAsync();
            return existingQuizSet;
        }

        public async Task<bool> SoftDeleteQuizSetAsync(Guid id)
        {
            var quizSet = await _context.QuizSets.FindAsync(id);
            if (quizSet == null)
                return false;

            quizSet.DeletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> HardDeleteQuizSetAsync(Guid id)
        {
            var quizSet = await _context.QuizSets.FindAsync(id);
            if (quizSet == null)
                return false;

            _context.QuizSets.Remove(quizSet);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> QuizSetExistsAsync(Guid id)
        {
            return await _context.QuizSets.AnyAsync(qs => qs.Id == id && qs.DeletedAt == null);
        }

        public async Task<QuizSet?> RestoreQuizSetAsync(Guid id)
        {
            var quizSet = await _context.QuizSets.FindAsync(id);
            if (quizSet == null)
                return null;
            if (quizSet.DeletedAt == null)
                return null;

            quizSet.DeletedAt = null;
            _context.QuizSets.Update(quizSet);
            await _context.SaveChangesAsync();
            return quizSet;
        }

        public async Task<bool> RequestValidateByMod(Guid id)
        {
            var existingQuizSet = await _context.QuizSets.FindAsync(id);
            if(existingQuizSet == null || existingQuizSet.DeletedAt != null)
                return false;
            existingQuizSet.IsRequireValidate = true;
            _context.QuizSets.Update(existingQuizSet);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ValidateQuizSet(Guid id)
        {
            var existingQuizSet = await _context.QuizSets.FindAsync(id);
            if (existingQuizSet == null || existingQuizSet.DeletedAt != null)
                return false;
            existingQuizSet.IsRequireValidate = false;
            existingQuizSet.IsPremiumOnly = true;
            existingQuizSet.ValidatedAt = DateTime.UtcNow;
            _context.QuizSets.Update(existingQuizSet);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
