using Repository.Entities;

namespace Repository.Interfaces
{
    public interface IEventParticipantRepo
    {
        Task<EventParticipant> CreateAsync(EventParticipant entity);
        Task<EventParticipant?> GetByIdAsync(Guid id);
        Task<EventParticipant?> GetByEventAndParticipantAsync(Guid eventId, Guid participantId);
        Task<IEnumerable<EventParticipant>> GetByEventIdAsync(Guid eventId);
        Task<IEnumerable<EventParticipant>> GetByParticipantIdAsync(Guid participantId);
        Task<long> CountParticipantsByEventIdAsync(Guid eventId);
        Task<EventParticipant> UpdateAsync(EventParticipant entity);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> IsParticipantInEventAsync(Guid eventId, Guid participantId);
    }
}

