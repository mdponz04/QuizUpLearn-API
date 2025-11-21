using Repository.Entities;

namespace Repository.Interfaces
{
    public interface IEventRepo
    {
        Task<Event> CreateAsync(Event entity);
        Task<Event?> GetByIdAsync(Guid id);
        Task<Event?> GetByIdWithDetailsAsync(Guid id);
        Task<IEnumerable<Event>> GetAllAsync();
        Task<IEnumerable<Event>> GetActiveEventsAsync();
        Task<IEnumerable<Event>> GetUpcomingEventsAsync();
        Task<IEnumerable<Event>> GetEventsByCreatorAsync(Guid creatorId);
        Task<Event> UpdateAsync(Event entity);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> ExistsAsync(Guid id);
    }
}

