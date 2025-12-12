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
            return await _context.Vocabularies
                .AsNoTracking()
                .Include(v => v.Quizzes)
                .ToListAsync();
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

        public async Task<bool> ExistsByKeyWordAndPartAsync(string keyWord, string? toeicPart, Guid? excludeId = null)
        {
            // Normalize input: trim và lowercase
            var normalizedKeyWord = (keyWord?.Trim() ?? string.Empty).ToLower();
            var normalizedToeicPart = string.IsNullOrWhiteSpace(toeicPart) ? null : toeicPart.Trim().ToLower();

            // Load all và filter in memory để hỗ trợ Trim() (vì EF Core không hỗ trợ Trim trong query)
            var allVocabularies = await _context.Vocabularies.AsNoTracking().ToListAsync();

            var exists = allVocabularies.Any(v =>
            {
                var vKeyWord = (v.KeyWord?.Trim() ?? string.Empty).ToLower();
                var vToeicPart = string.IsNullOrWhiteSpace(v.ToeicPart) ? null : v.ToeicPart.Trim().ToLower();

                // So sánh KeyWord
                if (vKeyWord != normalizedKeyWord)
                    return false;

                // So sánh ToeicPart
                if (normalizedToeicPart == null)
                {
                    if (vToeicPart != null)
                        return false;
                }
                else
                {
                    if (vToeicPart != normalizedToeicPart)
                        return false;
                }

                // Exclude current record nếu có
                if (excludeId.HasValue && v.Id == excludeId.Value)
                    return false;

                return true;
            });

            return exists;
        }
    }
}

