using Repository.Entities;

namespace Repository.Interfaces
{
	public interface ITournamentParticipantRepo
	{
		Task<TournamentParticipant> AddAsync(TournamentParticipant entity);
		Task<bool> ExistsAsync(Guid tournamentId, Guid participantId);
		Task<IEnumerable<TournamentParticipant>> GetByTournamentAsync(Guid tournamentId);
		Task<TournamentParticipant?> GetByTournamentAndUserAsync(Guid tournamentId, Guid userId);
	}
}


