using Repository.Entities;

namespace Repository.Interfaces
{
    public interface IUserMistakeRepo
    {
        Task<IEnumerable<UserMistake>> GetAllAsync();
        Task<UserMistake?> GetByIdAsync(Guid id);
        Task<UserMistake?> GetByUserIdAndQuizIdAsync(Guid userId, Guid quizId);
        Task AddAsync(UserMistake userMistake);
        Task UpdateAsync(Guid id, UserMistake userMistake);
        Task DeleteAsync(Guid id);
    }
}
