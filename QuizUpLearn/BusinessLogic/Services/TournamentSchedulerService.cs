using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Repository.Interfaces;

namespace BusinessLogic.Services
{
	public class TournamentSchedulerService : BackgroundService
	{
		private readonly ILogger<TournamentSchedulerService> _logger;
		private readonly IServiceScopeFactory _scopeFactory;

		public TournamentSchedulerService(
			ILogger<TournamentSchedulerService> logger,
			IServiceScopeFactory scopeFactory)
		{
			_logger = logger;
			_scopeFactory = scopeFactory;
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			_logger.LogInformation("TournamentSchedulerService started");
			while (!stoppingToken.IsCancellationRequested)
			{
				try
				{
					await TickAsync(stoppingToken);
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "TournamentSchedulerService tick failed");
				}

				await Task.Delay(TimeSpan.FromHours(6), stoppingToken);
			}
		}

		private async Task TickAsync(CancellationToken ct)
		{
			using var scope = _scopeFactory.CreateScope();
			var tournamentRepo = scope.ServiceProvider.GetRequiredService<ITournamentRepo>();
			var quizSetRepo = scope.ServiceProvider.GetRequiredService<ITournamentQuizSetRepo>();

			var today = DateTime.UtcNow.Date;
			
			var startedTournaments = await tournamentRepo.GetStartedAsync();
			foreach (var t in startedTournaments)
			{
				var endOfMonth = new DateTime(t.StartDate.Year, t.StartDate.Month, DateTime.DaysInMonth(t.StartDate.Year, t.StartDate.Month));
				if (today > endOfMonth || today > t.EndDate.Date)
				{
					if (t.Status != "Ended")
					{
						t.Status = "Ended";
						await tournamentRepo.UpdateAsync(t);
						_logger.LogInformation("Tournament {TournamentId}: status changed to Ended", t.Id);
					}
				}
			}

			// Xử lý các tournament đang "Started" và trong thời gian hoạt động
			var tournaments = await tournamentRepo.GetActiveAsync();
			foreach (var t in tournaments)
			{
				if (today < t.StartDate.Date || today > t.EndDate.Date) continue;

				// Xóa quiz set đã được dùng hôm qua (có IsActive = true)
				var allSets = await quizSetRepo.GetByTournamentAsync(t.Id);
				var yesterdayActive = allSets.Where(x => x.IsActive && x.UnlockDate.Date < today).ToList();
				foreach (var oldSet in yesterdayActive)
				{
					await quizSetRepo.DeleteAsync(oldSet.Id);
					_logger.LogInformation("Tournament {TournamentId}: removed used QuizSet {QuizSetId} from {Date}", t.Id, oldSet.QuizSetId, oldSet.UnlockDate.Date);
				}

				// Lấy danh sách quiz sets còn lại (chưa được dùng)
				var available = await quizSetRepo.GetAvailableAsync(t.Id);
				if (!available.Any())
				{
					_logger.LogWarning("Tournament {TournamentId}: no available quiz sets for {Date}", t.Id, today);
					continue;
				}

				// Random chọn 1 quiz set từ danh sách còn lại
				var rnd = new Random();
				var selected = available.OrderBy(_ => rnd.Next()).First();
				
				// Set IsActive = true và UnlockDate = today cho quiz set được chọn
				selected.IsActive = true;
				selected.UnlockDate = today;
				selected.UpdatedAt = DateTime.UtcNow;
				await quizSetRepo.UpdateRangeAsync(new[] { selected });
				
				_logger.LogInformation("Tournament {TournamentId}: activated QuizSet {QuizSetId} for {Date} (remaining: {Count})", 
					t.Id, selected.QuizSetId, today, available.Count() - 1);
			}
		}
	}
}


