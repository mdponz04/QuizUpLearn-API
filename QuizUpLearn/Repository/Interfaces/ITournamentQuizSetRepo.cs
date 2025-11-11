using Repository.Entities;

namespace Repository.Interfaces
{
	public interface ITournamentQuizSetRepo
	{
		Task AddRangeAsync(IEnumerable<TournamentQuizSet> entities);
		Task<IEnumerable<TournamentQuizSet>> GetByTournamentAsync(Guid tournamentId);
		Task<IEnumerable<TournamentQuizSet>> GetForDateAsync(Guid tournamentId, DateTime dateUtc);
		Task UpdateRangeAsync(IEnumerable<TournamentQuizSet> entities);
		Task RemoveOlderThanAsync(Guid tournamentId, DateTime dateUtc);
	}
}


