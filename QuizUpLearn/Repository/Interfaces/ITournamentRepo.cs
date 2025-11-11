using Repository.Entities;

namespace Repository.Interfaces
{
	public interface ITournamentRepo
	{
		Task<Tournament> CreateAsync(Tournament entity);
		Task<Tournament?> GetByIdAsync(Guid id);
		Task<IEnumerable<Tournament>> GetActiveAsync();
		Task<Tournament> UpdateAsync(Tournament entity);
	}
}


