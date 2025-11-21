using Repository.Entities;

namespace Repository.Interfaces
{
	public interface ITournamentRepo
	{
		Task<Tournament> CreateAsync(Tournament entity);
		Task<Tournament?> GetByIdAsync(Guid id);
		Task<IEnumerable<Tournament>> GetActiveAsync();
		Task<Tournament> UpdateAsync(Tournament entity);
		Task<bool> ExistsInMonthAsync(int year, int month);
		Task<IEnumerable<Tournament>> GetStartedAsync();
		Task<bool> DeleteAsync(Guid id);
		Task<IEnumerable<Tournament>> GetAllAsync(bool includeDeleted = false);
	}
}


