using Microsoft.EntityFrameworkCore;
using Repository.DBContext;
using Repository.Entities;
using Repository.Interfaces;

namespace Repository.Repositories
{
    public class EventRepo : IEventRepo
    {
        private readonly MyDbContext _context;

        public EventRepo(MyDbContext context)
        {
            _context = context;
        }

        public async Task<Event> CreateAsync(Event entity)
        {
            await _context.Events.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<Event?> GetByIdAsync(Guid id)
        {
            return await _context.Events
                .FirstOrDefaultAsync(e => e.Id == id && e.DeletedAt == null);
        }

        public async Task<Event?> GetByIdWithDetailsAsync(Guid id)
        {
            return await _context.Events
                .Include(e => e.Creator)
                .Include(e => e.QuizSet)
                .FirstOrDefaultAsync(e => e.Id == id && e.DeletedAt == null);
        }

        public async Task<IEnumerable<Event>> GetAllAsync()
        {
            return await _context.Events
                .Include(e => e.Creator)
                .Include(e => e.QuizSet)
                .Where(e => e.DeletedAt == null)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Event>> GetActiveEventsAsync()
        {
            var now = DateTime.UtcNow;
            return await _context.Events
                .Include(e => e.Creator)
                .Include(e => e.QuizSet)
                .Where(e => e.DeletedAt == null 
                    && e.Status == "Active" 
                    && e.StartDate <= now 
                    && e.EndDate >= now)
                .OrderBy(e => e.StartDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Event>> GetUpcomingEventsAsync()
        {
            var now = DateTime.UtcNow;
            return await _context.Events
                .Include(e => e.Creator)
                .Include(e => e.QuizSet)
                .Where(e => e.DeletedAt == null 
                    && e.Status == "Upcoming" 
                    && e.StartDate > now)
                .OrderBy(e => e.StartDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Event>> GetEventsByCreatorAsync(Guid creatorId)
        {
            return await _context.Events
                .Include(e => e.Creator)
                .Include(e => e.QuizSet)
                .Where(e => e.DeletedAt == null && e.CreatedBy == creatorId)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();
        }

        public async Task<Event> UpdateAsync(Event entity)
        {
            entity.UpdatedAt = DateTime.UtcNow;
            _context.Events.Update(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var entity = await GetByIdAsync(id);
            if (entity == null)
                return false;

            entity.DeletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await _context.Events
                .AnyAsync(e => e.Id == id && e.DeletedAt == null);
        }
    }
}

