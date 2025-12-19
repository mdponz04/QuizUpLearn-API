using Microsoft.EntityFrameworkCore;
using Repository.DBContext;
using Repository.Entities;
using Repository.Interfaces;
using Repository.Enums;

namespace Repository.Repositories
{
    public class QuizSetRepo : IQuizSetRepo
    {
        private readonly MyDbContext _context;

        public QuizSetRepo(MyDbContext context)
        {
            _context = context;
        }

        public async Task<QuizSet> CreateQuizSetAsync(QuizSet quizSet)
        {
            await _context.QuizSets.AddAsync(quizSet);
            await _context.SaveChangesAsync();
            return quizSet;
        }

        public async Task<QuizSet?> GetQuizSetByIdAsync(Guid id)
        {
            return await _context.QuizSets
                .Include(qs => qs.Creator)
                .FirstOrDefaultAsync(qs => qs.Id == id && qs.DeletedAt == null);
        }

        public async Task<IEnumerable<QuizSet>> GetAllQuizSetsAsync(
            string? searchTerm = null, 
            string? sortBy = null, 
            string? sortDirection = null,
            bool? isDeleted = null,
            bool? isPremiumOnly = null,
            bool? isPublished = null,
            QuizSetTypeEnum? quizSetType = null)
        {
            var query = _context.QuizSets
                .Include(qs => qs.Creator)
                .AsQueryable();

            query = ApplyFilters(query, isDeleted, isPremiumOnly, isPublished, quizSetType);
            query = ApplySearch(query, searchTerm);
            query = ApplySorting(query, sortBy, sortDirection);

            return await query.ToListAsync();
        }

        public async Task<IEnumerable<QuizSet>> GetQuizSetsByCreatorAsync(
            Guid creatorId, 
            string? searchTerm = null, 
            string? sortBy = null, 
            string? sortDirection = null,
            bool? isDeleted = null,
            bool? isPremiumOnly = null,
            bool? isPublished = null,
            QuizSetTypeEnum? quizSetType = null)
        {
            var query = _context.QuizSets
                .Include(qs => qs.Creator)
                .Where(qs => qs.CreatedBy == creatorId);

            query = ApplyFilters(query, isDeleted, isPremiumOnly, isPublished, quizSetType);
            query = ApplySearch(query, searchTerm);
            query = ApplySorting(query, sortBy, sortDirection);

            return await query.ToListAsync();
        }

        public async Task<IEnumerable<QuizSet>> GetPublishedQuizSetsAsync(
            string? searchTerm = null,
            string? sortBy = null,
            string? sortDirection = null,
            bool? isPremiumOnly = null,
            QuizSetTypeEnum? quizSetType = null)
        {
            var query = _context.QuizSets
                .Include(qs => qs.Creator)
                .Where(qs => qs.IsPublished && qs.DeletedAt == null);

            query = ApplyFilters(query, false, isPremiumOnly, true, quizSetType);
            query = ApplySearch(query, searchTerm);
            query = ApplySorting(query, sortBy, sortDirection);

            return await query.ToListAsync();
        }

        public async Task<QuizSet?> UpdateQuizSetAsync(Guid id, QuizSet quizSet)
        {
            var existingQuizSet = await _context.QuizSets.FindAsync(id);
            if (existingQuizSet == null || existingQuizSet.DeletedAt != null)
                return null;

            if(!string.IsNullOrEmpty(quizSet.Title))
                existingQuizSet.Title = quizSet.Title;
            if(!string.IsNullOrEmpty(quizSet.Description))
                existingQuizSet.Description = quizSet.Description;
            if (!string.IsNullOrEmpty(quizSet.QuizSetType.ToString()))
                existingQuizSet.QuizSetType = quizSet.QuizSetType;

            existingQuizSet.IsPublished = quizSet.IsPublished;
            existingQuizSet.IsPremiumOnly = quizSet.IsPremiumOnly;
            
            existingQuizSet.UpdatedAt = DateTime.UtcNow;
            _context.QuizSets.Update(existingQuizSet);
            await _context.SaveChangesAsync();
            return existingQuizSet;
        }

        public async Task<bool> SoftDeleteQuizSetAsync(Guid id)
        {
            var quizSet = await _context.QuizSets.FindAsync(id);
            if (quizSet == null)
                return false;

            quizSet.DeletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> HardDeleteQuizSetAsync(Guid id)
        {
            var quizSet = await _context.QuizSets.FindAsync(id);
            if (quizSet == null)
                return false;

            _context.QuizSets.Remove(quizSet);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> QuizSetExistsAsync(Guid id)
        {
            return await _context.QuizSets.AnyAsync(qs => qs.Id == id && qs.DeletedAt == null);
        }

        public async Task<QuizSet?> RestoreQuizSetAsync(Guid id)
        {
            var quizSet = await _context.QuizSets.FindAsync(id);
            if (quizSet == null)
                return null;
            if (quizSet.DeletedAt == null)
                return null;

            quizSet.DeletedAt = null;
            _context.QuizSets.Update(quizSet);
            await _context.SaveChangesAsync();
            return quizSet;
        }

        public async Task<bool> RequestValidateByMod(Guid id)
        {
            var existingQuizSet = await _context.QuizSets.FindAsync(id);
            if(existingQuizSet == null || existingQuizSet.DeletedAt != null)
                return false;
            existingQuizSet.IsRequireValidate = true;
            _context.QuizSets.Update(existingQuizSet);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ValidateQuizSet(Guid id)
        {
            var existingQuizSet = await _context.QuizSets.FindAsync(id);
            if (existingQuizSet == null || existingQuizSet.DeletedAt != null)
                return false;
            existingQuizSet.IsRequireValidate = false;
            existingQuizSet.IsPremiumOnly = true;
            existingQuizSet.ValidatedAt = DateTime.UtcNow;
            _context.QuizSets.Update(existingQuizSet);
            await _context.SaveChangesAsync();
            return true;
        }

        private IQueryable<QuizSet> ApplyFilters(
            IQueryable<QuizSet> query,
            bool? isDeleted = null,
            bool? isPremiumOnly = null,
            bool? isPublished = null,
            QuizSetTypeEnum? quizSetType = null)
        {
            if (isDeleted.HasValue)
            {
                if (isDeleted.Value)
                {
                    query = query.Where(qs => qs.DeletedAt != null);
                }
                else
                {
                    query = query.Where(qs => qs.DeletedAt == null);
                }
            }
            else
            {
                query = query.Where(qs => qs.DeletedAt == null);
            }

            if (isPremiumOnly.HasValue)
            {
                query = query.Where(qs => qs.IsPremiumOnly == isPremiumOnly.Value);
            }

            if (isPublished.HasValue)
            {
                query = query.Where(qs => qs.IsPublished == isPublished.Value);
            }

            if (quizSetType.HasValue)
            {
                query = query.Where(qs => qs.QuizSetType == quizSetType.Value);
            }
            return query;
        }

        private IQueryable<QuizSet> ApplySearch(IQueryable<QuizSet> query, string? searchTerm)
        {
            if (string.IsNullOrEmpty(searchTerm))
                return query;

            var normalizedSearchTerm = searchTerm.ToLower();

            return query.Where(qs => 
                (qs.Title != null && qs.Title.ToLower().Contains(normalizedSearchTerm)) ||
                (qs.Description != null && qs.Description.ToLower().Contains(normalizedSearchTerm))
            );
        }

        private IQueryable<QuizSet> ApplySorting(IQueryable<QuizSet> query, string? sortBy, string? sortDirection)
        {
            if (string.IsNullOrEmpty(sortBy))
                return query.OrderByDescending(qs => qs.CreatedAt);

            var isDescending = !string.IsNullOrEmpty(sortDirection) && 
                              sortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase);

            return sortBy.ToLower() switch
            {
                "title" => isDescending 
                    ? query.OrderByDescending(qs => qs.Title) 
                    : query.OrderBy(qs => qs.Title),
                "createdat" => isDescending 
                    ? query.OrderByDescending(qs => qs.CreatedAt) 
                    : query.OrderBy(qs => qs.CreatedAt),
                "updatedat" => isDescending 
                    ? query.OrderByDescending(qs => qs.UpdatedAt) 
                    : query.OrderBy(qs => qs.UpdatedAt),
                "deletedat" => isDescending 
                    ? query.OrderByDescending(qs => qs.DeletedAt) 
                    : query.OrderBy(qs => qs.DeletedAt),
                "totalattempts" => isDescending 
                    ? query.OrderByDescending(qs => qs.TotalAttempts) 
                    : query.OrderBy(qs => qs.TotalAttempts),
                "averagescore" => isDescending 
                    ? query.OrderByDescending(qs => qs.AverageScore) 
                    : query.OrderBy(qs => qs.AverageScore),
                "quizsettype" => isDescending 
                    ? query.OrderByDescending(qs => qs.QuizSetType) 
                    : query.OrderBy(qs => qs.QuizSetType),
                _ => query.OrderByDescending(qs => qs.CreatedAt)
            };
        }
    }
}
