using BusinessLogic.DTOs.TournamentDtos;

namespace BusinessLogic.Interfaces
{
	public interface ITournamentService
	{
		Task<TournamentResponseDto> CreateAsync(CreateTournamentRequestDto dto);
		Task<TournamentResponseDto> AddQuizSetsAsync(Guid tournamentId, IEnumerable<Guid> quizSetIds);
		Task<TournamentResponseDto> StartAsync(Guid tournamentId);
		Task<TournamentResponseDto> EndAsync(Guid tournamentId);
		Task<bool> JoinAsync(Guid tournamentId, Guid userId);
		Task<TournamentTodaySetDto?> GetTodaySetAsync(Guid tournamentId);
		Task<IEnumerable<TournamentQuizSetItemDto>> GetQuizSetsAsync(Guid tournamentId);
		Task<bool> DeleteAsync(Guid tournamentId);
		Task<IEnumerable<TournamentResponseDto>> GetAllAsync(bool includeDeleted = false);
	}
}


