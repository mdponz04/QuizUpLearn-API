using Repository.Entities;

namespace Repository.Interfaces
{
    public interface IVocabularyRepo
    {
        Task<IEnumerable<Vocabulary>> GetAllAsync();
        Task<Vocabulary?> GetByIdAsync(Guid id);
        Task<Vocabulary?> CreateAsync(Vocabulary vocabulary);
        Task<Vocabulary?> UpdateAsync(Guid id, Vocabulary vocabulary);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> HasQuizzesAsync(Guid id);
        Task<bool> ExistsByKeyWordAndPartAsync(string keyWord, string? toeicPart, Guid? excludeId = null);
    }
}

