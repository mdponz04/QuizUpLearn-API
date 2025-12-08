using Microsoft.EntityFrameworkCore;
using Repository.DBContext;
using Repository.Entities;
using Repository.Interfaces;

namespace Repository.Repositories
{
	public class TournamentParticipantRepo : ITournamentParticipantRepo
	{
		private readonly MyDbContext _context;
		public TournamentParticipantRepo(MyDbContext context)
		{
			_context = context;
		}

		public async Task<TournamentParticipant> AddAsync(TournamentParticipant entity)
		{
			await _context.TournamentParticipants.AddAsync(entity);
			await _context.SaveChangesAsync();
			return entity;
		}

		public async Task<bool> ExistsAsync(Guid tournamentId, Guid participantId)
		{
			return await _context.TournamentParticipants
				.AnyAsync(p => p.TournamentId == tournamentId && p.ParticipantId == participantId && p.DeletedAt == null);
		}

		public async Task<IEnumerable<TournamentParticipant>> GetByTournamentAsync(Guid tournamentId)
		{
			return await _context.TournamentParticipants
				.Where(p => p.TournamentId == tournamentId && p.DeletedAt == null)
				.ToListAsync();
		}

		public async Task<TournamentParticipant?> GetByTournamentAndUserAsync(Guid tournamentId, Guid userId)
		{
			return await _context.TournamentParticipants
				.FirstOrDefaultAsync(p => p.TournamentId == tournamentId && p.ParticipantId == userId && p.DeletedAt == null);
		}
	}
}


