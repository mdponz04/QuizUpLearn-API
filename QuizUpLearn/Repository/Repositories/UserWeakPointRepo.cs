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

        public async Task<bool> IsWeakPointExisted(string weakPoint, Guid userId)
        {
            if (string.IsNullOrWhiteSpace(weakPoint))
                return false;

            var weakPoints = await _context.UserWeakPoints
                .Where(wp => wp.UserId == userId)
                .Select(wp => wp.WeakPoint)
                .ToListAsync();

            return weakPoints.Any(wp => GetDifference(wp.ToLower().Trim(), weakPoint.ToLower().Trim()) <= 0.2);
        }

        private double GetDifference(string s1, string s2)
        {
            int minAction = Levenshtein(s1, s2); //minimum edit actions find by Levenshtein algorithm = the numbers of different characters
            int maxAction = Math.Max(s1.Length, s2.Length); //worst case: replace all characters (include empty string)
            return (double)minAction / maxAction;
        }
        //Levenshtein Distance Algorithm
        private int Levenshtein(string a, string b)
        {
            //Dynamic programming approach
            var dp = new int[a.Length + 1, b.Length + 1];

            for (int i = 0; i <= a.Length; i++) dp[i, 0] = i;
            for (int j = 0; j <= b.Length; j++) dp[0, j] = j;

            for (int i = 1; i <= a.Length; i++)
                for (int j = 1; j <= b.Length; j++)
                {
                    //calculate the match (if match cost = 0 if it's not then replace => cost =1)
                    int cost = a[i - 1] == b[j - 1] ? 0 : 1;
                    //Choose the best option: deletion, insertion, replacement
                    dp[i, j] = Math.Min(
                        Math.Min(dp[i - 1, j] + 1, dp[i, j - 1] + 1) // Find min between deletion and insertion
                        , dp[i - 1, j - 1] + cost // replacement/keep of a character
                    );
                }   

            return dp[a.Length, b.Length]; // minimum edit actions
        }
    }
}
