using Repository.Entities;

namespace Repository.Interfaces
{
    public interface IQuizGroupItemRepo
    {
        Task<IEnumerable<QuizGroupItem>> GetAllAsync();
        Task<QuizGroupItem?> GetByIdAsync(Guid id);
        Task<QuizGroupItem?> CreateAsync(QuizGroupItem quizGroupItem);
        Task<QuizGroupItem?> UpdateAsync(Guid id, QuizGroupItem quizGroupItem);
        Task<bool> DeleteAsync(Guid id);
    }
}
