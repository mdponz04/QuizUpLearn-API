using BusinessLogic.DTOs.TournamentDtos;
using BusinessLogic.Interfaces;
using Repository.Entities;
using Repository.Interfaces;

namespace BusinessLogic.Services
{
	public class TournamentService : ITournamentService
	{
		private readonly ITournamentRepo _tournamentRepo;
		private readonly ITournamentParticipantRepo _participantRepo;
		private readonly ITournamentQuizSetRepo _tournamentQuizSetRepo;
		private readonly IQuizSetRepo _quizSetRepo;

		public TournamentService(
			ITournamentRepo tournamentRepo,
			ITournamentParticipantRepo participantRepo,
			ITournamentQuizSetRepo tournamentQuizSetRepo,
			IQuizSetRepo quizSetRepo)
		{
			_tournamentRepo = tournamentRepo;
			_participantRepo = participantRepo;
			_tournamentQuizSetRepo = tournamentQuizSetRepo;
			_quizSetRepo = quizSetRepo;
		}

		public async Task<TournamentResponseDto> CreateAsync(CreateTournamentRequestDto dto)
		{
			if (dto.StartDate.Kind != DateTimeKind.Utc) dto.StartDate = DateTime.SpecifyKind(dto.StartDate, DateTimeKind.Utc);
			if (dto.EndDate.Kind != DateTimeKind.Utc) dto.EndDate = DateTime.SpecifyKind(dto.EndDate, DateTimeKind.Utc);

			var entity = new Tournament
			{
				Name = dto.Name,
				Description = dto.Description,
				StartDate = dto.StartDate,
				EndDate = dto.EndDate,
				MaxParticipants = dto.MaxParticipants,
				Status = "Draft",
				CreatedBy = dto.CreatedBy
			};
			entity = await _tournamentRepo.CreateAsync(entity);

			if (dto.QuizSetIds.Any())
			{
				await AddQuizSetsInternal(entity, dto.QuizSetIds);
			}

			return MapResponse(entity, dto.QuizSetIds.Count());
		}

		public async Task<TournamentResponseDto> AddQuizSetsAsync(Guid tournamentId, IEnumerable<Guid> quizSetIds)
		{
			var tournament = await _tournamentRepo.GetByIdAsync(tournamentId) ?? throw new ArgumentException("Tournament not found");
			await AddQuizSetsInternal(tournament, quizSetIds);
			var all = await _tournamentQuizSetRepo.GetByTournamentAsync(tournamentId);
			return MapResponse(tournament, all.Count());
		}

		public async Task<TournamentResponseDto> StartAsync(Guid tournamentId)
		{
			var tournament = await _tournamentRepo.GetByIdAsync(tournamentId) ?? throw new ArgumentException("Tournament not found");
			tournament.Status = "Active";
			await _tournamentRepo.UpdateAsync(tournament);
			var all = await _tournamentQuizSetRepo.GetByTournamentAsync(tournamentId);
			return MapResponse(tournament, all.Count());
		}

		public async Task<bool> JoinAsync(Guid tournamentId, Guid userId)
		{
			if (await _participantRepo.ExistsAsync(tournamentId, userId)) return true;
			var entity = new TournamentParticipant
			{
				TournamentId = tournamentId,
				ParticipantId = userId,
				JoinAt = DateTime.UtcNow,
				CreatedAt = DateTime.UtcNow
			};
			await _participantRepo.AddAsync(entity);
			return true;
		}

		public async Task<TournamentTodaySetDto?> GetTodaySetAsync(Guid tournamentId)
		{
			var today = DateTime.UtcNow.Date;
			var sets = await _tournamentQuizSetRepo.GetForDateAsync(tournamentId, today);
			var active = sets.FirstOrDefault(x => x.IsActive) ?? sets.FirstOrDefault();
			if (active == null) return null;
			return new TournamentTodaySetDto
			{
				TournamentId = tournamentId,
				Date = today,
				QuizSetId = active.QuizSetId,
				DayNumber = active.DateNumber
			};
		}

		private async Task AddQuizSetsInternal(Tournament tournament, IEnumerable<Guid> quizSetIds)
		{
			var ids = quizSetIds.Distinct().ToList();
			if (!ids.Any()) return;

			// chỉ cho phép quiz set có type Tournament
			var validIds = new List<Guid>();
			foreach (var id in ids)
			{
				var quizSet = await _quizSetRepo.GetQuizSetByIdAsync(id);
				if (quizSet == null) continue;
				if (quizSet.QuizType != Repository.Enums.QuizSetTypeEnum.Tournament) continue;
				validIds.Add(id);
			}
			if (!validIds.Any()) return;

			// số ngày còn lại trong tháng kể từ sau StartDate
			var startDate = tournament.StartDate.Date;
			var daysInMonth = DateTime.DaysInMonth(startDate.Year, startDate.Month);
			var days = daysInMonth - startDate.Day;
			if (days <= 0) return;

			// shuffle ngẫu nhiên danh sách quiz set
			var rnd = new Random();
			var shuffled = validIds.OrderBy(_ => rnd.Next()).ToList();

			// nếu thiếu so với số ngày, lặp lại để đủ; nếu dư, cắt bớt
			var expanded = new List<Guid>();
			while (expanded.Count < days)
			{
				foreach (var q in shuffled)
				{
					if (expanded.Count >= days) break;
					expanded.Add(q);
				}
			}
			if (expanded.Count > days) expanded = expanded.Take(days).ToList();

			var items = new List<TournamentQuizSet>();
			for (int i = 0; i < days; i++)
			{
				items.Add(new TournamentQuizSet
				{
					TournamentId = tournament.Id,
					QuizSetId = expanded[i],
					UnlockDate = startDate.AddDays(i + 1),
					IsActive = false,
					DateNumber = i + 1,
					CreatedAt = DateTime.UtcNow
				});
			}
			await _tournamentQuizSetRepo.AddRangeAsync(items);
		}

		private static TournamentResponseDto MapResponse(Tournament t, int totalQuizSets)
		{
			return new TournamentResponseDto
			{
				Id = t.Id,
				Name = t.Name,
				Description = t.Description,
				StartDate = t.StartDate,
				EndDate = t.EndDate,
				MaxParticipants = t.MaxParticipants,
				Status = t.Status,
				TotalQuizSets = totalQuizSets
			};
		}
	}
}


