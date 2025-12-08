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
		private readonly IQuizAttemptRepo _quizAttemptRepo;
		private readonly IUserRepo _userRepo;

		public TournamentService(
			ITournamentRepo tournamentRepo,
			ITournamentParticipantRepo participantRepo,
			ITournamentQuizSetRepo tournamentQuizSetRepo,
			IQuizSetRepo quizSetRepo,
			IQuizAttemptRepo quizAttemptRepo,
			IUserRepo userRepo)
		{
			_tournamentRepo = tournamentRepo;
			_participantRepo = participantRepo;
			_tournamentQuizSetRepo = tournamentQuizSetRepo;
			_quizSetRepo = quizSetRepo;
			_quizAttemptRepo = quizAttemptRepo;
			_userRepo = userRepo;
		}

	public async Task<TournamentResponseDto> CreateAsync(CreateTournamentRequestDto dto)
	{
		if (dto.StartDate.Kind != DateTimeKind.Utc) dto.StartDate = DateTime.SpecifyKind(dto.StartDate, DateTimeKind.Utc);
		if (dto.EndDate.Kind != DateTimeKind.Utc) dto.EndDate = DateTime.SpecifyKind(dto.EndDate, DateTimeKind.Utc);

		// Validate dates are not in the past
		var today = DateTime.UtcNow.Date;
		if (dto.StartDate.Date < today)
		{
			throw new ArgumentException("Không thể tạo giải đấu với ngày bắt đầu trong quá khứ.");
		}
		
		if (dto.EndDate.Date < today)
		{
			throw new ArgumentException("Không thể tạo giải đấu với ngày kết thúc trong quá khứ.");
		}
		
		if (dto.StartDate.Date > dto.EndDate.Date)
		{
			throw new ArgumentException("Ngày bắt đầu phải trước ngày kết thúc.");
		}

		// Kiểm tra nếu đã có tournament "Started" trong tháng thì không cho tạo mới
		// Use EndDate to determine the target month, as StartDate might shift to previous month due to timezone (e.g. 00:00 Local -> 17:00 Prev Day UTC)
		var checkDate = dto.EndDate.Date;
		if (await _tournamentRepo.ExistsStartedInMonthAsync(checkDate.Year, checkDate.Month))
		{
			throw new ArgumentException($"Đã tồn tại tournament đang 'Started' trong tháng {checkDate.Month}/{checkDate.Year}. Không thể tạo tournament mới khi đã có tournament đang chạy.");
		}			var entity = new Tournament
			{
				Name = dto.Name,
				Description = dto.Description,
				StartDate = dto.StartDate,
				EndDate = dto.EndDate,
				MaxParticipants = dto.MaxParticipants,
				Status = "Created",
				CreatedBy = dto.CreatedBy
			};
			var scheduledQuizSetIds = dto.QuizSetIds.Any()
				? await BuildScheduledQuizSetIdsAsync(entity, dto.QuizSetIds)
				: new List<Guid>();

			entity = await _tournamentRepo.CreateAsync(entity);

			if (scheduledQuizSetIds.Any())
			{
				await SaveTournamentQuizSetsAsync(entity, scheduledQuizSetIds);
			}

			return MapResponse(entity, scheduledQuizSetIds.Count);
		}

		public async Task<TournamentResponseDto> AddQuizSetsAsync(Guid tournamentId, IEnumerable<Guid> quizSetIds)
		{
			var tournament = await _tournamentRepo.GetByIdAsync(tournamentId) ?? throw new ArgumentException("Tournament not found");
			var scheduledIds = await BuildScheduledQuizSetIdsAsync(tournament, quizSetIds);
			await SaveTournamentQuizSetsAsync(tournament, scheduledIds);
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

			// Kiểm tra nếu đã có tournament "Started" trong tháng thì không cho start
			// Mỗi tháng chỉ được 1 tournament có status "Started"
			var checkDate = tournament.EndDate.Date;
			if (await _tournamentRepo.ExistsStartedInMonthAsync(checkDate.Year, checkDate.Month))
			{
				throw new ArgumentException($"Đã tồn tại tournament đang 'Started' trong tháng {checkDate.Month}/{checkDate.Year}. Mỗi tháng chỉ được có 1 tournament đang chạy.");
			}

			tournament.Status = "Started";
			await _tournamentRepo.UpdateAsync(tournament);
			var all = await _tournamentQuizSetRepo.GetByTournamentAsync(tournamentId);
			return MapResponse(tournament, all.Count());
		}

		public async Task<TournamentResponseDto> EndAsync(Guid tournamentId)
		{
			var tournament = await _tournamentRepo.GetByIdAsync(tournamentId) ?? throw new ArgumentException("Tournament not found");
			if (tournament.Status != "Started")
			{
				throw new ArgumentException($"Chỉ có thể end tournament khi status là 'Started'. Trạng thái hiện tại: {tournament.Status}");
			}

			tournament.Status = "Ended";
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
			var startOfDay = today;
			var endOfDay = today.AddDays(1).AddTicks(-1);
			return new TournamentTodaySetDto
			{
				TournamentId = tournamentId,
				StartDate = startOfDay,
				EndDate = endOfDay,
				QuizSetId = active.QuizSetId,
				DayNumber = active.DateNumber
			};
		}

		public async Task<IEnumerable<TournamentQuizSetItemDto>> GetQuizSetsAsync(Guid tournamentId)
		{
			var tournament = await _tournamentRepo.GetByIdAsync(tournamentId) ?? throw new ArgumentException("Tournament not found");
			if (tournament.Status != "Created")
			{
				throw new ArgumentException($"Chỉ có thể xem quiz set khi tournament đang ở trạng thái 'Created'. Trạng thái hiện tại: {tournament.Status}");
			}

			var items = await _tournamentQuizSetRepo.GetByTournamentAsync(tournamentId);
			return items.OrderBy(x => x.DateNumber)
				.Select(x => new TournamentQuizSetItemDto
				{
					Id = x.Id,
					TournamentId = x.TournamentId,
					QuizSetId = x.QuizSetId,
					UnlockDate = x.UnlockDate,
					IsActive = x.IsActive,
					DayNumber = x.DateNumber
				});
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

		private async Task<List<Guid>> BuildScheduledQuizSetIdsAsync(Tournament tournament, IEnumerable<Guid> quizSetIds)
		{
			var ids = quizSetIds.Distinct().ToList();
			if (!ids.Any())
			{
				throw new ArgumentException("Phải cung cấp ít nhất một quiz set cho tournament.");
			}

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

		var startDate = tournament.StartDate.Date;
		var endDate = tournament.EndDate.Date;
		var today = DateTime.UtcNow.Date;
		
		// Determine effective start date
		var effectiveStartDate = startDate;
		
		// Fix for timezone shift: If startDate is in the previous month of endDate, bump it to 1st of endDate's month.
		// This handles cases where 00:00 Local becomes 17:00 Previous Day UTC (e.g., Jan 1 00:00 Local -> Dec 31 17:00 UTC)
		if (effectiveStartDate.Month != endDate.Month || effectiveStartDate.Year != endDate.Year)
		{
			var firstDayOfEndMonth = new DateTime(endDate.Year, endDate.Month, 1);
			if (effectiveStartDate < firstDayOfEndMonth) effectiveStartDate = firstDayOfEndMonth;
		}
		
		// For current month tournaments, use today as the effective start if startDate is in the past
		if (effectiveStartDate < today && effectiveStartDate.Month == endDate.Month && effectiveStartDate.Year == endDate.Year)
		{
			effectiveStartDate = today;
		}
		
		// Calculate days from effective start date to end date
		var days = (int)(endDate - effectiveStartDate).TotalDays + 1;

		if (days <= 0)
		{
			throw new ArgumentException("Không còn ngày nào trong tháng để thêm quiz set");
		}

		if (validIds.Count != days)
		{
			if (validIds.Count > days)
			{
				throw new ArgumentException($"Chỉ có thể thêm được tối đa {days} quiz set(s) (số ngày còn lại trong tháng từ {effectiveStartDate:dd/MM/yyyy} đến cuối tháng). Bạn đang cố thêm {validIds.Count} quiz set(s).");
			}

			throw new ArgumentException($"Cần truyền đúng {days} quiz set(s) tương ứng số ngày còn lại trong tháng (từ {effectiveStartDate:dd/MM/yyyy} đến cuối tháng). Bạn hiện chỉ truyền {validIds.Count} quiz set(s).");
		}			var rnd = new Random();
			return validIds.OrderBy(_ => rnd.Next()).ToList();
		}

		private async Task SaveTournamentQuizSetsAsync(Tournament tournament, IList<Guid> scheduledQuizSetIds)
		{
			await _tournamentQuizSetRepo.RemoveAllByTournamentAsync(tournament.Id);

			var startDate = tournament.StartDate.Date;
			var items = scheduledQuizSetIds.Select((quizSetId, index) => new TournamentQuizSet
			{
				TournamentId = tournament.Id,
				QuizSetId = quizSetId,
				// UnlockDate bắt đầu từ StartDate (index 0 = StartDate, index 1 = StartDate + 1, ...)
				UnlockDate = startDate.AddDays(index),
				IsActive = false,
				DateNumber = index + 1,
				CreatedAt = DateTime.UtcNow
			}).ToList();

			await _tournamentQuizSetRepo.AddRangeAsync(items);
		}

		public async Task<IEnumerable<TournamentResponseDto>> GetAllAsync(bool includeDeleted = false)
		{
			var tournaments = await _tournamentRepo.GetAllAsync(includeDeleted);
			var result = new List<TournamentResponseDto>();
			
			foreach (var t in tournaments)
			{
				var quizSets = await _tournamentQuizSetRepo.GetByTournamentAsync(t.Id);
				result.Add(MapResponse(t, quizSets.Count()));
			}
			
			return result;
		}

		public async Task<IEnumerable<TournamentResponseDto>> GetByMonthAsync(int year, int month, bool includeDeleted = false)
		{
			var tournaments = await _tournamentRepo.GetByMonthAsync(year, month, includeDeleted);
			var result = new List<TournamentResponseDto>();
			
			foreach (var t in tournaments)
			{
				var quizSets = await _tournamentQuizSetRepo.GetByTournamentAsync(t.Id);
				result.Add(MapResponse(t, quizSets.Count()));
			}
			
			return result;
		}

		public async Task<bool> IsUserJoinedAsync(Guid tournamentId, Guid userId)
		{
			return await _participantRepo.ExistsAsync(tournamentId, userId);
		}

		public async Task<IEnumerable<TournamentLeaderboardItemDto>> GetLeaderboardAsync(Guid tournamentId)
		{
			var tournament = await _tournamentRepo.GetByIdAsync(tournamentId) ?? throw new ArgumentException("Tournament not found");

			// Lấy tất cả participants của tournament
			var participants = await _participantRepo.GetByTournamentAsync(tournamentId);
			var participantList = participants.ToList();

			if (!participantList.Any())
			{
				return new List<TournamentLeaderboardItemDto>();
			}

			// Lấy tất cả quiz sets của tournament (bao gồm cả đã bị soft delete để tính điểm từ quiz attempts cũ)
			var tournamentQuizSets = await _tournamentQuizSetRepo.GetAllByTournamentAsync(tournamentId, includeDeleted: true);
			var quizSetIds = tournamentQuizSets.Select(tqs => tqs.QuizSetId).Distinct().ToList();

			// Khai báo result ở đây để dùng cho cả 2 trường hợp
			var result = new List<TournamentLeaderboardItemDto>();

			if (!quizSetIds.Any())
			{
				// Nếu không có quiz set nào, trả về leaderboard với điểm 0 cho tất cả participants
				foreach (var participant in participantList)
				{
					var user = await _userRepo.GetByIdAsync(participant.ParticipantId);
					if (user == null) continue;

					result.Add(new TournamentLeaderboardItemDto
					{
						UserId = participant.ParticipantId,
						Username = user.Username,
						FullName = user.FullName,
						AvatarUrl = user.AvatarUrl,
						Score = 0,
						Date = participant.JoinAt
					});
				}

				for (int i = 0; i < result.Count; i++)
				{
					result[i].Rank = i + 1;
				}

				return result;
			}

		// Tính điểm theo từng ngày trong tournament
		// Logic: Mỗi ngày user chỉ được làm 1 lần, lấy attempt mới nhất của ngày đó
		// Nếu không làm ngày nào thì tính 0 điểm cho ngày đó
		// Tổng điểm = tổng điểm của tất cả các ngày
		var tournamentStartDate = tournament.StartDate.Date;
		var tournamentEndDate = tournament.EndDate.Date;
		var now = DateTime.UtcNow.Date;
		var effectiveEndDate = now < tournamentEndDate ? now : tournamentEndDate;

			// Lấy tất cả attempts liên quan đến các quiz set của tournament một lần để giảm số query
			var allAttempts = await _quizAttemptRepo.GetByQuizSetIdsAsync(quizSetIds, includeDeleted: false);
			var attemptsByUser = allAttempts
				.Where(a => a.Status == "completed" && a.DeletedAt == null)
				.GroupBy(a => a.UserId)
				.ToDictionary(g => g.Key, g => g.ToList());

		// Tạo leaderboard items từ participants
		foreach (var participant in participantList)
		{
			var user = await _userRepo.GetByIdAsync(participant.ParticipantId);
			if (user == null) continue;

			var totalScore = 0;
			var participantJoinDate = participant.JoinAt.Date;
			var startDate = participantJoinDate > tournamentStartDate ? participantJoinDate : tournamentStartDate;

				if (!attemptsByUser.TryGetValue(participant.ParticipantId, out var userAttempts))
				{
					userAttempts = new List<Repository.Entities.QuizAttempt>();
				}

				// Lọc attempts chỉ tính những attempts được hoàn thành trong thời gian tournament và sau khi join
				var validAttempts = userAttempts
				.Where(a =>
				{
					var attemptDate = (a.UpdatedAt ?? a.CreatedAt).Date;
						return attemptDate >= tournamentStartDate
							&& attemptDate <= tournamentEndDate
							&& a.CreatedAt >= participant.JoinAt;
				})
				.ToList();

			// Nhóm attempts theo ngày và lấy attempt mới nhất của mỗi ngày
			var dailyAttempts = validAttempts
				.GroupBy(a => (a.UpdatedAt ?? a.CreatedAt).Date)
				.Select(g => g.OrderByDescending(a => a.UpdatedAt ?? a.CreatedAt).First())
				.ToList();

			// Tính tổng điểm từ các attempts đã được nhóm theo ngày
			totalScore = dailyAttempts.Sum(a => a.Score);

			result.Add(new TournamentLeaderboardItemDto
			{
				UserId = participant.ParticipantId,
				Username = user.Username,
				FullName = user.FullName,
				AvatarUrl = user.AvatarUrl,
				Score = totalScore,
				Date = participant.JoinAt
			});
		}

			// Sắp xếp theo điểm giảm dần và gán rank
			result = result
				.OrderByDescending(x => x.Score)
				.ThenBy(x => x.Date)
				.ToList();

			for (int i = 0; i < result.Count; i++)
			{
				result[i].Rank = i + 1;
			}

			return result;
		}

		public async Task<TournamentResponseDto?> GetByIdAsync(Guid tournamentId)
		{
			var tournament = await _tournamentRepo.GetByIdAsync(tournamentId);
			if (tournament == null) return null;

			var quizSets = await _tournamentQuizSetRepo.GetByTournamentAsync(tournamentId);
			return MapResponse(tournament, quizSets.Count());
		}

		public async Task<List<object>> GetUserDailyScoresAsync(Guid tournamentId, Guid userId, DateTime startDate, DateTime endDate)
		{
			var tournament = await _tournamentRepo.GetByIdAsync(tournamentId);
			if (tournament == null) return new List<object>();

			var participant = (await _participantRepo.GetByTournamentAsync(tournamentId))
				.FirstOrDefault(p => p.ParticipantId == userId);
			if (participant == null) return new List<object>();

			var tournamentQuizSets = await _tournamentQuizSetRepo.GetAllByTournamentAsync(tournamentId, includeDeleted: true);
			var dailyScores = new List<object>();
			var totalScore = 0;
			var participantJoinDate = participant.JoinAt.Date;
			var effectiveStartDate = participantJoinDate > startDate ? participantJoinDate : startDate;

			var allQuizSetIds = tournamentQuizSets.Select(tqs => tqs.QuizSetId).Distinct().ToList();
			var attempts = await _quizAttemptRepo.GetByQuizSetIdsAsync(allQuizSetIds, includeDeleted: false);
			var validAttempts = attempts
				.Where(a => 
					a.UserId == userId
					&& a.Status == "completed"
					&& a.DeletedAt == null
					&& a.CreatedAt >= participant.JoinAt
				)
				.Where(a =>
				{
					var attemptDate = (a.UpdatedAt ?? a.CreatedAt).Date;
					return attemptDate >= tournament.StartDate.Date && attemptDate <= tournament.EndDate.Date;
				})
				.ToList();

			// Nhóm attempts theo ngày và lấy attempt mới nhất của mỗi ngày
			var dailyAttemptsDict = validAttempts
				.GroupBy(a => (a.UpdatedAt ?? a.CreatedAt).Date)
				.ToDictionary(g => g.Key, g => g.OrderByDescending(a => a.UpdatedAt ?? a.CreatedAt).First());

			for (var currentDate = effectiveStartDate; currentDate <= endDate; currentDate = currentDate.AddDays(1))
			{
				var dayScore = 0;
				if (dailyAttemptsDict.TryGetValue(currentDate, out var dayAttempt))
				{
					dayScore = dayAttempt.Score;
				}

				totalScore += dayScore;
				dailyScores.Add(new
				{
					Date = currentDate,
					DayScore = dayScore,
					CumulativeScore = totalScore
				});
			}

			return dailyScores;
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


