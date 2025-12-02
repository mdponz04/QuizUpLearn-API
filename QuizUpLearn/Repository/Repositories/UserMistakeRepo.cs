using Microsoft.EntityFrameworkCore;
using Repository.DBContext;
using Repository.Entities;
using Repository.Interfaces;

namespace Repository.Repositories
{
    public class UserMistakeRepo : IUserMistakeRepo
    {
        private readonly MyDbContext _context;

        public UserMistakeRepo(MyDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<UserMistake>> GetAllAsync()
        {
            return await _context.UserMistakes.ToListAsync();
        }

        public async Task<UserMistake?> GetByIdAsync(Guid id)
        {
            return await _context.UserMistakes.FindAsync(id);
        }

        public async Task<UserMistake?> GetByUserIdAndQuizIdAsync(Guid userId, Guid quizId)
        {
            return await _context.UserMistakes
                .FirstOrDefaultAsync(um => um.UserId == userId && um.QuizId == quizId);
        }

        public async Task AddAsync(UserMistake userMistake)
        {
            await _context.UserMistakes.AddAsync(userMistake);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Guid id, UserMistake userMistake)
        {
            var existingUserMistake = await _context.UserMistakes.FindAsync(id);
            if (existingUserMistake == null)
            {
                throw new ArgumentException("UserMistake not found");
            }
            if(userMistake.TimesAttempted > 0)
            {
                existingUserMistake.TimesAttempted = userMistake.TimesAttempted;
            }
            if(userMistake.TimesWrong > 0)
            {
                existingUserMistake.TimesWrong = userMistake.TimesWrong;
            }
            if(userMistake.IsAnalyzed)
            {
                existingUserMistake.IsAnalyzed = userMistake.IsAnalyzed;
            }
            if(userMistake.UserWeakPointId != null)
            {
                existingUserMistake.UserWeakPointId = userMistake.UserWeakPointId;
            }
            if(userMistake.UserAnswer != null)
            {
                existingUserMistake.UserAnswer = userMistake.UserAnswer;
            }
            existingUserMistake.LastAttemptedAt = userMistake.LastAttemptedAt;
            existingUserMistake.UpdatedAt = DateTime.UtcNow;

            _context.UserMistakes.Update(existingUserMistake);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var userMistake = await GetByIdAsync(id);
            if (userMistake != null)
            {
                _context.UserMistakes.Remove(userMistake);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<UserMistake>> GetAlByUserIdAsync(Guid userId)
        {
            return await _context.UserMistakes
                .Where(um => um.UserId == userId)
                .Include(um => um.Quiz)
                    .ThenInclude(q => q!.AnswerOptions)
                .Include(um => um.Quiz)
                    .ThenInclude(q => q!.QuizGroupItem)
                .ToListAsync();
        }
    }
}
