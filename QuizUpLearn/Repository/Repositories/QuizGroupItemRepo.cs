using Microsoft.EntityFrameworkCore;
using Repository.DBContext;
using Repository.Entities;
using Repository.Interfaces;

namespace Repository.Repositories
{
    public class QuizGroupItemRepo : IQuizGroupItemRepo
    {
        private readonly MyDbContext _context;

        public QuizGroupItemRepo(MyDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<QuizGroupItem>> GetAllAsync()
        {
            return await _context.QuizGroupItems.ToListAsync();
        }

        public async Task<QuizGroupItem?> GetByIdAsync(Guid id)
        {
            return await _context.QuizGroupItems.FindAsync(id);
        }

        public async Task<QuizGroupItem?> CreateAsync(QuizGroupItem quizGroupItem)
        {
            await _context.QuizGroupItems.AddAsync(quizGroupItem);
            await _context.SaveChangesAsync();

            return quizGroupItem;
        }

        public async Task<QuizGroupItem?> UpdateAsync(Guid id, QuizGroupItem quizGroupItem)
        {
            var existingItem = await _context.QuizGroupItems.FindAsync(id);
            if (existingItem == null)
            {
                throw new KeyNotFoundException($"QuizGroupItem with ID {id} not found.");
            }
            if(existingItem.Name != quizGroupItem.Name)
                existingItem.Name = quizGroupItem.Name;
            if(existingItem.AudioUrl != quizGroupItem.AudioUrl)
                existingItem.AudioUrl = quizGroupItem.AudioUrl;
            if(existingItem.ImageUrl != quizGroupItem.ImageUrl)
                existingItem.ImageUrl = quizGroupItem.ImageUrl;
            if(existingItem.AudioScript != quizGroupItem.AudioScript)
                existingItem.AudioScript = quizGroupItem.AudioScript;
            if(existingItem.ImageDescription != quizGroupItem.ImageDescription)
                existingItem.ImageDescription = quizGroupItem.ImageDescription;
            if(existingItem.PassageText != quizGroupItem.PassageText)
                existingItem.PassageText = quizGroupItem.PassageText;

            _context.QuizGroupItems.Update(existingItem);
            await _context.SaveChangesAsync();

            return existingItem;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var quizGroupItem = await GetByIdAsync(id);
            if (quizGroupItem != null)
            {
                _context.QuizGroupItems.Remove(quizGroupItem);
                await _context.SaveChangesAsync();

                return true;
            }

            return false;
        }
    }
}
