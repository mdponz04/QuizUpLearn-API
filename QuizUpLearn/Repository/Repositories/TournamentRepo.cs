using Microsoft.EntityFrameworkCore;
using Repository.DBContext;
using Repository.Entities;
using Repository.Interfaces;

namespace Repository.Repositories
{
	public class TournamentRepo : ITournamentRepo
	{
		private readonly MyDbContext _context;
		public TournamentRepo(MyDbContext context)
		{
			_context = context;
		}

		public async Task<Tournament> CreateAsync(Tournament entity)
		{
			await _context.Tournaments.AddAsync(entity);
			await _context.SaveChangesAsync();
			return entity;
		}

		public async Task<Tournament?> GetByIdAsync(Guid id)
		{
			return await _context.Tournaments
				.Include(t => t.TournamentQuizSets)
				.FirstOrDefaultAsync(t => t.Id == id && t.DeletedAt == null);
		}

		public async Task<IEnumerable<Tournament>> GetActiveAsync()
		{
			return await _context.Tournaments
				.Where(t => t.DeletedAt == null && t.Status == "Started" && t.StartDate <= DateTime.UtcNow && t.EndDate >= DateTime.UtcNow)
				.ToListAsync();
		}

		public async Task<Tournament> UpdateAsync(Tournament entity)
		{
			_context.Tournaments.Update(entity);
			await _context.SaveChangesAsync();
			return entity;
		}

		public async Task<bool> ExistsInMonthAsync(int year, int month)
		{
			return await _context.Tournaments
				.AnyAsync(t => t.DeletedAt == null 
					&& t.StartDate.Year == year 
					&& t.StartDate.Month == month);
		}

		public async Task<IEnumerable<Tournament>> GetStartedAsync()
		{
			return await _context.Tournaments
				.Where(t => t.DeletedAt == null && t.Status == "Started")
				.ToListAsync();
		}

		public async Task<bool> DeleteAsync(Guid id)
		{
			var tournament = await _context.Tournaments
				.FirstOrDefaultAsync(t => t.Id == id && t.DeletedAt == null);
			if (tournament == null) return false;
			
			tournament.DeletedAt = DateTime.UtcNow;
			await _context.SaveChangesAsync();
			return true;
		}

		public async Task<IEnumerable<Tournament>> GetAllAsync(bool includeDeleted = false)
		{
			return await _context.Tournaments
				.Where(t => includeDeleted || t.DeletedAt == null)
				.OrderByDescending(t => t.CreatedAt)
				.ToListAsync();
		}
	}
}


