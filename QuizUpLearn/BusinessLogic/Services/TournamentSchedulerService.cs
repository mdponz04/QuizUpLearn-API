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

				// chạy mỗi 1 giờ
				await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
			}
		}

		private async Task TickAsync(CancellationToken ct)
		{
			using var scope = _scopeFactory.CreateScope();
			var tournamentRepo = scope.ServiceProvider.GetRequiredService<ITournamentRepo>();
			var quizSetRepo = scope.ServiceProvider.GetRequiredService<ITournamentQuizSetRepo>();

			var today = DateTime.UtcNow.Date;
			var tournaments = await tournamentRepo.GetActiveAsync();
			foreach (var t in tournaments)
			{
				if (today < t.StartDate.Date || today > t.EndDate.Date) continue;

				var todays = await quizSetRepo.GetForDateAsync(t.Id, today);
				if (!todays.Any()) continue;

				// Chọn 1 set ngẫu nhiên trong ngày (nếu có nhiều), bật nó và tắt các set khác trong tournament
				var rnd = new Random();
				var selected = todays.OrderBy(_ => rnd.Next()).First();
				var all = await quizSetRepo.GetByTournamentAsync(t.Id);
				foreach (var item in all)
				{
					item.IsActive = item.Id == selected.Id;
					item.UpdatedAt = DateTime.UtcNow;
				}
				await quizSetRepo.UpdateRangeAsync(all);
				await quizSetRepo.RemoveOlderThanAsync(t.Id, today);
				_logger.LogInformation("Tournament {TournamentId}: activated QuizSet {QuizSetId} for {Date}", t.Id, selected.QuizSetId, today);
			}
		}
	}
}


