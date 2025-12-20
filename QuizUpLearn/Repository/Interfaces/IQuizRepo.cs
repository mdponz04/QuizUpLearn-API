using Repository.Entities;

namespace Repository.Interfaces
{
    public interface IQuizRepo
    {
        Task<Quiz> CreateQuizAsync(Quiz quiz);
        Task<IEnumerable<Quiz>> CreateQuizzesBatchAsync(IEnumerable<Quiz> quizzes);
        Task<Quiz?> GetQuizByIdAsync(Guid id);
        Task<IEnumerable<Quiz>> GetQuizzesByIdsAsync(IEnumerable<Guid> ids);
        Task<IEnumerable<Quiz>> GetAllQuizzesAsync();
        Task<IEnumerable<Quiz>> GetQuizzesByQuizSetIdAsync(Guid quizSetId);
        Task<Quiz> UpdateQuizAsync(Guid id, Quiz quiz);
        Task<bool> SoftDeleteQuizAsync(Guid id);
        Task<bool> HardDeleteQuizAsync(Guid id);
        Task<bool> HardDeleteQuizzesBatchAsync(IEnumerable<Quiz> quizzes);
        Task<bool> RestoreQuizAsync(Guid id);
        Task<IEnumerable<Quiz>> GetByGrammarIdAndVocabularyId(Guid grammarId, Guid vocabId);
    }
}
