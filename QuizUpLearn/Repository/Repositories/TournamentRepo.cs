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
				.Where(t => t.DeletedAt == null && t.Status == "Active" && t.StartDate <= DateTime.UtcNow && t.EndDate >= DateTime.UtcNow)
				.ToListAsync();
		}

		public async Task<Tournament> UpdateAsync(Tournament entity)
		{
			_context.Tournaments.Update(entity);
			await _context.SaveChangesAsync();
			return entity;
		}
	}
}


