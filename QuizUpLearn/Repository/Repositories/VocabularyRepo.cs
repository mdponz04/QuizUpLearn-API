using Microsoft.EntityFrameworkCore;
using Repository.DBContext;
using Repository.Entities;
using Repository.Interfaces;

namespace Repository.Repositories
{
    public class VocabularyRepo : IVocabularyRepo
    {
        private readonly MyDbContext _context;

        public VocabularyRepo(MyDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Vocabulary>> GetAllAsync()
        {
            return await _context.Vocabularies.AsNoTracking().ToListAsync();
        }

        public async Task<Vocabulary?> GetByIdAsync(Guid id)
        {
            return await _context.Vocabularies.FindAsync(id);
        }

        public async Task<Vocabulary?> CreateAsync(Vocabulary vocabulary)
        {
            await _context.Vocabularies.AddAsync(vocabulary);
            await _context.SaveChangesAsync();
            return vocabulary;
        }

        public async Task<Vocabulary?> UpdateAsync(Guid id, Vocabulary vocabulary)
        {
            var existing = await _context.Vocabularies.FindAsync(id);
            if (existing == null)
            {
                return null;
            }

            existing.KeyWord = vocabulary.KeyWord;
            existing.VocabularyDifficulty = vocabulary.VocabularyDifficulty;
            existing.ToeicPart = vocabulary.ToeicPart;
            existing.PassageType = vocabulary.PassageType;

            _context.Vocabularies.Update(existing);
            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var existing = await _context.Vocabularies.FindAsync(id);
            if (existing == null)
            {
                return false;
            }

            _context.Vocabularies.Remove(existing);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> HasQuizzesAsync(Guid id)
        {
            return await _context.Quizzes.AsNoTracking().AnyAsync(q => q.VocabularyId == id);
        }
    }
}

