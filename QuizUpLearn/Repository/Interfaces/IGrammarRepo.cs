using Repository.Entities;

namespace Repository.Interfaces
{
    public interface IGrammarRepo
    {
        Task<IEnumerable<Grammar>> GetAllAsync();
        Task<Grammar?> GetByIdAsync(Guid id);
        Task<Grammar?> CreateAsync(Grammar grammar);
        Task<Grammar?> UpdateAsync(Guid id, Grammar grammar);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> HasQuizzesAsync(Guid id);
        Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null);
    }
}

