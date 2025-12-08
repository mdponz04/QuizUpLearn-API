using Repository.Entities;

namespace Repository.Interfaces
{
	public interface ITournamentQuizSetRepo
	{
		Task AddRangeAsync(IEnumerable<TournamentQuizSet> entities);
		Task<IEnumerable<TournamentQuizSet>> GetByTournamentAsync(Guid tournamentId);
		Task<IEnumerable<TournamentQuizSet>> GetAllByTournamentAsync(Guid tournamentId, bool includeDeleted = false);
		Task<IEnumerable<TournamentQuizSet>> GetForDateAsync(Guid tournamentId, DateTime dateUtc);
		Task UpdateRangeAsync(IEnumerable<TournamentQuizSet> entities);
		Task RemoveOlderThanAsync(Guid tournamentId, DateTime dateUtc);
		Task RemoveAllByTournamentAsync(Guid tournamentId);
		Task<IEnumerable<TournamentQuizSet>> GetAvailableAsync(Guid tournamentId);
		Task DeleteAsync(Guid tournamentQuizSetId);
		Task<IEnumerable<TournamentQuizSet>> GetActiveByQuizSetIdAsync(Guid quizSetId);
	}
}


