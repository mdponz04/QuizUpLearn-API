using Microsoft.EntityFrameworkCore;
using Repository.DBContext;
using Repository.Entities;
using Repository.Interfaces;

namespace Repository.Repositories
{
    public class EventParticipantRepo : IEventParticipantRepo
    {
        private readonly MyDbContext _context;

        public EventParticipantRepo(MyDbContext context)
        {
            _context = context;
        }

        public async Task<EventParticipant> CreateAsync(EventParticipant entity)
        {
            await _context.EventParticipants.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<EventParticipant?> GetByIdAsync(Guid id)
        {
            return await _context.EventParticipants
                .Include(ep => ep.Event)
                .Include(ep => ep.Participant)
                .FirstOrDefaultAsync(ep => ep.Id == id && ep.DeletedAt == null);
        }

        public async Task<EventParticipant?> GetByEventAndParticipantAsync(Guid eventId, Guid participantId)
        {
            return await _context.EventParticipants
                .Include(ep => ep.Event)
                .Include(ep => ep.Participant)
                .FirstOrDefaultAsync(ep => ep.EventId == eventId 
                    && ep.ParticipantId == participantId 
                    && ep.DeletedAt == null);
        }

        public async Task<IEnumerable<EventParticipant>> GetByEventIdAsync(Guid eventId)
        {
            return await _context.EventParticipants
                .Include(ep => ep.Participant)
                .Where(ep => ep.EventId == eventId && ep.DeletedAt == null)
                .OrderByDescending(ep => ep.Score)
                .ThenBy(ep => ep.Accuracy)
                .ToListAsync();
        }

        public async Task<IEnumerable<EventParticipant>> GetByParticipantIdAsync(Guid participantId)
        {
            return await _context.EventParticipants
                .Include(ep => ep.Event)
                .Where(ep => ep.ParticipantId == participantId && ep.DeletedAt == null)
                .OrderByDescending(ep => ep.JoinAt)
                .ToListAsync();
        }

        public async Task<long> CountParticipantsByEventIdAsync(Guid eventId)
        {
            return await _context.EventParticipants
                .Where(ep => ep.EventId == eventId && ep.DeletedAt == null)
                .CountAsync();
        }

        public async Task<EventParticipant> UpdateAsync(EventParticipant entity)
        {
            entity.UpdatedAt = DateTime.UtcNow;
            _context.EventParticipants.Update(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var entity = await _context.EventParticipants
                .FirstOrDefaultAsync(ep => ep.Id == id && ep.DeletedAt == null);
            
            if (entity == null)
                return false;

            entity.DeletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> IsParticipantInEventAsync(Guid eventId, Guid participantId)
        {
            return await _context.EventParticipants
                .AnyAsync(ep => ep.EventId == eventId 
                    && ep.ParticipantId == participantId 
                    && ep.DeletedAt == null);
        }
    }
}

