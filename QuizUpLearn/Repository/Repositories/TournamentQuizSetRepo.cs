using Microsoft.EntityFrameworkCore;
using Repository.DBContext;
using Repository.Entities;
using Repository.Interfaces;

namespace Repository.Repositories
{
	public class TournamentQuizSetRepo : ITournamentQuizSetRepo
	{
		private readonly MyDbContext _context;
		public TournamentQuizSetRepo(MyDbContext context)
		{
			_context = context;
		}

		public async Task AddRangeAsync(IEnumerable<TournamentQuizSet> entities)
		{
			await _context.TournamentQuizSets.AddRangeAsync(entities);
			await _context.SaveChangesAsync();
		}

		public async Task<IEnumerable<TournamentQuizSet>> GetByTournamentAsync(Guid tournamentId)
		{
			return await _context.TournamentQuizSets
				.Where(x => x.TournamentId == tournamentId && x.DeletedAt == null)
				.OrderBy(x => x.DateNumber)
				.ToListAsync();
		}

		public async Task<IEnumerable<TournamentQuizSet>> GetForDateAsync(Guid tournamentId, DateTime dateUtc)
		{
			var date = dateUtc.Date;
			return await _context.TournamentQuizSets
				.Where(x => x.TournamentId == tournamentId && x.DeletedAt == null && x.UnlockDate.Date == date)
				.ToListAsync();
		}

		public async Task UpdateRangeAsync(IEnumerable<TournamentQuizSet> entities)
		{
			_context.TournamentQuizSets.UpdateRange(entities);
			await _context.SaveChangesAsync();
		}

		public async Task RemoveOlderThanAsync(Guid tournamentId, DateTime dateUtc)
		{
			var threshold = dateUtc.Date;
			var items = await _context.TournamentQuizSets
				.Where(x => x.TournamentId == tournamentId && x.DeletedAt == null && x.UnlockDate.Date < threshold)
				.ToListAsync();
			if (!items.Any()) return;
			foreach (var item in items)
			{
				item.DeletedAt = DateTime.UtcNow;
				item.IsActive = false;
			}
			_context.TournamentQuizSets.UpdateRange(items);
			await _context.SaveChangesAsync();
		}

		public async Task RemoveAllByTournamentAsync(Guid tournamentId)
		{
			var items = await _context.TournamentQuizSets
				.Where(x => x.TournamentId == tournamentId && x.DeletedAt == null)
				.ToListAsync();
			if (!items.Any()) return;
			foreach (var item in items)
			{
				item.DeletedAt = DateTime.UtcNow;
				item.IsActive = false;
			}
			_context.TournamentQuizSets.UpdateRange(items);
			await _context.SaveChangesAsync();
		}

		public async Task<IEnumerable<TournamentQuizSet>> GetAvailableAsync(Guid tournamentId)
		{
			return await _context.TournamentQuizSets
				.Where(x => x.TournamentId == tournamentId && x.DeletedAt == null && !x.IsActive)
				.ToListAsync();
		}

		public async Task DeleteAsync(Guid tournamentQuizSetId)
		{
			var item = await _context.TournamentQuizSets
				.FirstOrDefaultAsync(x => x.Id == tournamentQuizSetId && x.DeletedAt == null);
			if (item == null) return;
			
			item.DeletedAt = DateTime.UtcNow;
			item.IsActive = false;
			await _context.SaveChangesAsync();
		}
	}
}


