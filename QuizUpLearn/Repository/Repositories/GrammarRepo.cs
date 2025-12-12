using Microsoft.EntityFrameworkCore;
using Repository.DBContext;
using Repository.Entities;
using Repository.Interfaces;

namespace Repository.Repositories
{
    public class GrammarRepo : IGrammarRepo
    {
        private readonly MyDbContext _context;

        public GrammarRepo(MyDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Grammar>> GetAllAsync()
        {
            return await _context.Grammars
                .AsNoTracking()
                .Include(g => g.Quizzes)
                .ToListAsync();
        }

        public async Task<Grammar?> GetByIdAsync(Guid id)
        {
            return await _context.Grammars.FindAsync(id);
        }

        public async Task<Grammar?> CreateAsync(Grammar grammar)
        {
            await _context.Grammars.AddAsync(grammar);
            await _context.SaveChangesAsync();
            return grammar;
        }

        public async Task<Grammar?> UpdateAsync(Guid id, Grammar grammar)
        {
            var existing = await _context.Grammars.FindAsync(id);
            if (existing == null)
            {
                return null;
            }

            existing.Name = grammar.Name;
            existing.Tense = grammar.Tense;
            existing.GrammarDifficulty = grammar.GrammarDifficulty;

            _context.Grammars.Update(existing);
            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var existing = await _context.Grammars.FindAsync(id);
            if (existing == null)
            {
                return false;
            }

            _context.Grammars.Remove(existing);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> HasQuizzesAsync(Guid id)
        {
            return await _context.Quizzes.AsNoTracking().AnyAsync(q => q.GrammarId == id);
        }

        public async Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null)
        {
            var query = _context.Grammars.AsNoTracking()
                .Where(g => g.Name.ToLower() == name.ToLower());

            if (excludeId.HasValue)
            {
                query = query.Where(g => g.Id != excludeId.Value);
            }

            return await query.AnyAsync();
        }
    }
}

