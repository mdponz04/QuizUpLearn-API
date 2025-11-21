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

			// Kiểm tra mỗi tháng chỉ được tạo 1 tournament
			var startDate = dto.StartDate.Date;
			if (await _tournamentRepo.ExistsInMonthAsync(startDate.Year, startDate.Month))
			{
				throw new ArgumentException($"Đã tồn tại tournament trong tháng {startDate.Month}/{startDate.Year}. Mỗi tháng chỉ được tạo 1 tournament.");
			}

			var entity = new Tournament
			{
				Name = dto.Name,
				Description = dto.Description,
				StartDate = dto.StartDate,
				EndDate = dto.EndDate,
				MaxParticipants = dto.MaxParticipants,
				Status = "Created",
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
			if (tournament.Status != "Created")
			{
				throw new ArgumentException($"Tournament phải ở trạng thái 'Created' mới có thể start. Trạng thái hiện tại: {tournament.Status}");
			}
			tournament.Status = "Started";
			await _tournamentRepo.UpdateAsync(tournament);
			var all = await _tournamentQuizSetRepo.GetByTournamentAsync(tournamentId);
			return MapResponse(tournament, all.Count());
		}

		public async Task<bool> JoinAsync(Guid tournamentId, Guid userId)
		{
			// Kiểm tra tournament tồn tại
			var tournament = await _tournamentRepo.GetByIdAsync(tournamentId);
			if (tournament == null)
			{
				throw new ArgumentException("Tournament not found");
			}

			// Chỉ cho phép join khi tournament đã Started
			if (tournament.Status != "Started")
			{
				throw new ArgumentException($"Chỉ có thể join tournament khi status là 'Started'. Trạng thái hiện tại: {tournament.Status}");
			}

			// Kiểm tra đã join chưa
			if (await _participantRepo.ExistsAsync(tournamentId, userId)) return true;

			// Kiểm tra số lượng participants
			var currentParticipants = await _participantRepo.GetByTournamentAsync(tournamentId);
			if (currentParticipants.Count() >= tournament.MaxParticipants)
			{
				throw new ArgumentException($"Tournament đã đạt số lượng participants tối đa ({tournament.MaxParticipants})");
			}

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

		public async Task<bool> DeleteAsync(Guid tournamentId)
		{
			var tournament = await _tournamentRepo.GetByIdAsync(tournamentId) ?? throw new ArgumentException("Tournament not found");
			
			if (tournament.Status != "Created")
			{
				throw new ArgumentException($"Chỉ có thể xóa tournament khi status là 'Created'. Trạng thái hiện tại: {tournament.Status}");
			}

			return await _tournamentRepo.DeleteAsync(tournamentId);
		}

		private async Task AddQuizSetsInternal(Tournament tournament, IEnumerable<Guid> quizSetIds)
		{
			var ids = quizSetIds.Distinct().ToList();
			if (!ids.Any()) return;

			// chỉ cho phép quiz set có type Tournament
			var validIds = new List<Guid>();
			var invalidIds = new List<Guid>();
			foreach (var id in ids)
			{
				var quizSet = await _quizSetRepo.GetQuizSetByIdAsync(id);
				if (quizSet == null)
				{
					invalidIds.Add(id);
					continue;
				}
				if (quizSet.QuizSetType != Repository.Enums.QuizSetTypeEnum.Tournament)
				{
					invalidIds.Add(id);
					continue;
				}
				validIds.Add(id);
			}
			
			if (invalidIds.Any())
			{
				throw new ArgumentException($"Các quiz set sau không phải loại Tournament hoặc không tồn tại: {string.Join(", ", invalidIds)}");
			}
			
			if (!validIds.Any()) 
			{
				throw new ArgumentException("Không có quiz set hợp lệ để thêm vào tournament");
			}

			// số ngày còn lại trong tháng kể từ sau StartDate
			var startDate = tournament.StartDate.Date;
			var daysInMonth = DateTime.DaysInMonth(startDate.Year, startDate.Month);
			var days = daysInMonth - startDate.Day;
			
			if (days <= 0)
			{
				throw new ArgumentException("Không còn ngày nào trong tháng để thêm quiz set");
			}

			// Kiểm tra số quiz sets muốn add không được vượt quá số ngày còn lại
			if (validIds.Count > days)
			{
				throw new ArgumentException($"Chỉ có thể thêm được tối đa {days} quiz set(s) (số ngày còn lại trong tháng từ ngày tạo tournament đến cuối tháng). Bạn đang cố thêm {validIds.Count} quiz set(s).");
			}

			// Xóa các quiz sets cũ nếu có (để add lại từ đầu)
			await _tournamentQuizSetRepo.RemoveAllByTournamentAsync(tournament.Id);

			// shuffle ngẫu nhiên danh sách quiz set
			var rnd = new Random();
			var shuffled = validIds.OrderBy(_ => rnd.Next()).ToList();

			// Nếu thiếu so với số ngày, lặp lại để đủ; nếu đủ thì dùng hết
			var expanded = new List<Guid>();
			while (expanded.Count < days)
			{
				foreach (var q in shuffled)
				{
					if (expanded.Count >= days) break;
					expanded.Add(q);
				}
			}

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

		public async Task<IEnumerable<TournamentResponseDto>> GetAllAsync(bool includeDeleted = false)
		{
			var tournaments = await _tournamentRepo.GetAllAsync(includeDeleted);
			var responses = new List<TournamentResponseDto>();

			foreach (var tournament in tournaments)
			{
				var quizSets = await _tournamentQuizSetRepo.GetByTournamentAsync(tournament.Id);
				responses.Add(MapResponse(tournament, quizSets.Count()));
			}

			return responses;
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


