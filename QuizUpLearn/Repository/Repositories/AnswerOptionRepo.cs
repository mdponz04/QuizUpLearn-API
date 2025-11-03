using Microsoft.EntityFrameworkCore;
using Repository.DBContext;
using Repository.Entities;
using Repository.Interfaces;

namespace Repository.Repositories
{
    public class AnswerOptionRepo : IAnswerOptionRepo
    {
        private readonly MyDbContext _context;

        public AnswerOptionRepo(MyDbContext context)
        {
            _context = context;
        }

        public async Task<AnswerOption> CreateAsync(AnswerOption answerOption)
        {
            await _context.AnswerOptions.AddAsync(answerOption);
            await _context.SaveChangesAsync();
            return answerOption;
        }

        public async Task<IEnumerable<AnswerOption>> GetAllAsync(bool includeDeleted = false)
        {
            return await _context.AnswerOptions
                .AsQueryable()
                .Where(ao => includeDeleted || ao.DeletedAt == null)
                .Include(ao => ao.Question)
                .OrderBy(ao => ao.OrderIndex)
                .ToListAsync();
        }

        public async Task<AnswerOption?> GetByIdAsync(Guid id)
        {
            return await _context.AnswerOptions
                .Include(ao => ao.Question)
                .FirstOrDefaultAsync(ao => ao.Id == id);
        }

        public async Task<IEnumerable<AnswerOption>> GetByQuizIdAsync(Guid quizId, bool includeDeleted = false)
        {
            return await _context.AnswerOptions
                .AsQueryable()
                .Where(ao => ao.QuizId == quizId && (includeDeleted || ao.DeletedAt == null))
                .Include(ao => ao.Question)
                .OrderBy(ao => ao.OrderIndex)
                .ToListAsync();
        }

        public async Task<bool> RestoreAsync(Guid id)
        {
            var answerOption = await _context.AnswerOptions.FindAsync(id);
            if (answerOption == null) return false;
            answerOption.DeletedAt = null;
            _context.AnswerOptions.Update(answerOption);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> SoftDeleteAsync(Guid id)
        {
            var answerOption = await _context.AnswerOptions.FindAsync(id);
            if (answerOption == null) return false;
            answerOption.DeletedAt = DateTime.UtcNow;
            _context.AnswerOptions.Update(answerOption);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<AnswerOption?> UpdateAsync(Guid id, AnswerOption answerOption)
        {
            var existing = await _context.AnswerOptions.FindAsync(id);
            if (existing == null) return null;

            if(!string.IsNullOrEmpty(answerOption.OptionLabel))
                existing.OptionLabel = answerOption.OptionLabel;
            if(!string.IsNullOrEmpty(answerOption.OptionText))
                existing.OptionText = answerOption.OptionText;
            
            existing.OrderIndex = answerOption.OrderIndex;
            existing.IsCorrect = answerOption.IsCorrect;
            existing.UpdatedAt = DateTime.UtcNow;

            _context.AnswerOptions.Update(existing);
            await _context.SaveChangesAsync();
            return existing;
        }
    }
}
