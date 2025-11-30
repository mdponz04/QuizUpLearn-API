using Microsoft.EntityFrameworkCore;
using Repository.DBContext;
using Repository.Entities;
using Repository.Interfaces;

namespace Repository.Repositories
{
    public class UserWeakPointRepo : IUserWeakPointRepo
    {
        private readonly MyDbContext _context;

        public UserWeakPointRepo(MyDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<UserWeakPoint>> GetByUserIdAsync(Guid userId)
        {
            return await _context.UserWeakPoints
                .Where(uwp => uwp.UserId == userId)
                .ToListAsync();
        }

        public async Task<UserWeakPoint?> GetByIdAsync(Guid id)
        {
            return await _context.UserWeakPoints
                .FirstOrDefaultAsync(uwp => uwp.Id == id);
        }

        public async Task<UserWeakPoint?> AddAsync(UserWeakPoint userWeakPoint)
        {
            _context.UserWeakPoints.Add(userWeakPoint);
            await _context.SaveChangesAsync();
            return userWeakPoint;
        }

        public async Task<UserWeakPoint?> UpdateAsync(Guid id, UserWeakPoint userWeakPoint)
        {
            var existing = await _context.UserWeakPoints.FindAsync(id);
            if (existing == null) return null;

            existing.WeakPoint = userWeakPoint.WeakPoint;
            if(userWeakPoint.Advice != null)
                existing.Advice = userWeakPoint.Advice;

            existing.IsDone = userWeakPoint.IsDone;
            if(userWeakPoint.CompleteAt != null)
                existing.CompleteAt = userWeakPoint.CompleteAt;
            if(userWeakPoint.QuizSetId != null)
                existing.QuizSetId = userWeakPoint.QuizSetId;
            if(existing.ToeicPart != userWeakPoint.ToeicPart)
                existing.ToeicPart = userWeakPoint.ToeicPart;
            if(userWeakPoint.DifficultyLevel != null)
                existing.DifficultyLevel = userWeakPoint.DifficultyLevel;

            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var existing = await _context.UserWeakPoints
                .Where(uwp => uwp.Id == id)
                .FirstOrDefaultAsync();

            if(existing == null) return false;

            _context.UserWeakPoints.Remove(existing);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> IsWeakPointExisted(string weakPoint)
        {
            if(string.IsNullOrEmpty(weakPoint))
                return false;

            return await _context.UserWeakPoints.AnyAsync(wp => wp.WeakPoint == weakPoint);
        }
    }
}
