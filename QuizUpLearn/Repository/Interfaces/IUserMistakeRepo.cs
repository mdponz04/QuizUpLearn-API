using Repository.Entities;

namespace Repository.Interfaces
{
    public interface IUserMistakeRepo
    {
        Task<IEnumerable<UserMistake>> GetAllAsync();
        Task<IEnumerable<UserMistake>> GetAlByUserIdAsync(Guid userId);
        Task<UserMistake?> GetByIdAsync(Guid id);
        Task<UserMistake?> GetByUserIdAndQuizIdAsync(Guid userId, Guid quizId);
        Task<IEnumerable<UserMistake>> GetByUserWeakPointIdAsync(Guid userWeakPointId);
        Task AddAsync(UserMistake userMistake);
        Task UpdateAsync(Guid id, UserMistake userMistake);
        Task DeleteAsync(Guid id);
    }
}
