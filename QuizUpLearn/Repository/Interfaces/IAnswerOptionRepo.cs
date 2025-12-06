using Repository.Entities;

namespace Repository.Interfaces
{
    public interface IAnswerOptionRepo
    {
        Task<AnswerOption> CreateAsync(AnswerOption answerOption);
        Task<IEnumerable<AnswerOption>> CreateBatchAsync(IEnumerable<AnswerOption> answerOptions);
        Task<AnswerOption?> GetByIdAsync(Guid id);
        Task<IEnumerable<AnswerOption>> GetAllAsync(bool includeDeleted = false);
        Task<IEnumerable<AnswerOption>> GetByQuizIdAsync(Guid quizId, bool includeDeleted = false);
        Task<AnswerOption?> UpdateAsync(Guid id, AnswerOption answerOption);
        Task<bool> SoftDeleteAsync(Guid id);
        Task<bool> RestoreAsync(Guid id);
    }
}
