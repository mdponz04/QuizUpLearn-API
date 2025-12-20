using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BusinessLogic.Interfaces;
using BusinessLogic.DTOs.TournamentDtos;
using QuizUpLearn.API.Models;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using QuizUpLearn.API.Attributes;

namespace QuizUpLearn.API.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	[Authorize]
	public class TournamentController : ControllerBase
	{
		private readonly ITournamentService _tournamentService;
		private readonly IUserService _userService;
		private readonly ILogger<TournamentController> _logger;

		public TournamentController(
			ITournamentService tournamentService, 
			IUserService userService,
			ILogger<TournamentController> logger)
		{
			_tournamentService = tournamentService;
			_userService = userService;
			_logger = logger;
		}

		[HttpGet]
		[AllowAnonymous]
		public async Task<ActionResult<ApiResponse<IEnumerable<TournamentResponseDto>>>> GetAll([FromQuery] bool includeDeleted = false)
		{
			try
			{
				var res = await _tournamentService.GetAllAsync(includeDeleted);
				return Ok(new ApiResponse<IEnumerable<TournamentResponseDto>> { Success = true, Data = res, Message = "OK" });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Get all tournaments failed");
				return BadRequest(new ApiResponse<IEnumerable<TournamentResponseDto>> { Success = false, Message = ex.Message });
			}
		}

		[HttpGet("month/{year:int}/{month:int}")]
		[AllowAnonymous]
		public async Task<ActionResult<ApiResponse<IEnumerable<TournamentResponseDto>>>> GetByMonth(
			[FromRoute] int year, 
			[FromRoute] int month,
			[FromQuery] bool includeDeleted = false)
		{
			try
			{
				if (month < 1 || month > 12)
				{
					return BadRequest(new ApiResponse<IEnumerable<TournamentResponseDto>> 
					{ 
						Success = false, 
						Message = "Tháng phải từ 1 đến 12" 
					});
				}

				var res = await _tournamentService.GetByMonthAsync(year, month, includeDeleted);
				return Ok(new ApiResponse<IEnumerable<TournamentResponseDto>> { Success = true, Data = res, Message = "OK" });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Get tournaments by month failed");
				return BadRequest(new ApiResponse<IEnumerable<TournamentResponseDto>> { Success = false, Message = ex.Message });
			}
		}

		[HttpPost("create")]
		public async Task<ActionResult<ApiResponse<TournamentResponseDto>>> Create([FromBody] CreateTournamentRequestDto dto)
		{
			try
			{
				var res = await _tournamentService.CreateAsync(dto);
				return Ok(new ApiResponse<TournamentResponseDto> { Success = true, Data = res, Message = "Đã tạo giải đấu" });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Create tournament failed");
				return BadRequest(new ApiResponse<TournamentResponseDto> { Success = false, Message = ex.Message });
			}
		}

		public class AddQuizSetsRequest
		{
			public IEnumerable<Guid> QuizSetIds { get; set; } = new List<Guid>();
		}

		[HttpPut("{id:guid}/quizsets")]
		public async Task<ActionResult<ApiResponse<TournamentResponseDto>>> AddQuizSets([FromRoute] Guid id, [FromBody] AddQuizSetsRequest body)
		{
			try
			{
				var res = await _tournamentService.AddQuizSetsAsync(id, body.QuizSetIds);
				return Ok(new ApiResponse<TournamentResponseDto> { Success = true, Data = res, Message = "Đã thêm bộ câu hỏi" });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Add quiz sets failed");
				return BadRequest(new ApiResponse<TournamentResponseDto> { Success = false, Message = ex.Message });
			}
		}

		[HttpGet("{id:guid}/quizsets")]
		public async Task<ActionResult<ApiResponse<IEnumerable<TournamentQuizSetItemDto>>>> GetQuizSets([FromRoute] Guid id)
		{
			try
			{
				var res = await _tournamentService.GetQuizSetsAsync(id);
				return Ok(new ApiResponse<IEnumerable<TournamentQuizSetItemDto>> { Success = true, Data = res, Message = "OK" });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Get quiz sets failed");
				return BadRequest(new ApiResponse<IEnumerable<TournamentQuizSetItemDto>> { Success = false, Message = ex.Message });
			}
		}

		[HttpPost("{id:guid}/start")]
		public async Task<ActionResult<ApiResponse<TournamentResponseDto>>> Start([FromRoute] Guid id)
		{
			try
			{
				var res = await _tournamentService.StartAsync(id);
				return Ok(new ApiResponse<TournamentResponseDto> { Success = true, Data = res, Message = "Đã bắt đầu giải đấu" });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Start tournament failed");
				return BadRequest(new ApiResponse<TournamentResponseDto> { Success = false, Message = ex.Message });
			}
		}

		[HttpPost("{id:guid}/end")]
		public async Task<ActionResult<ApiResponse<TournamentResponseDto>>> End([FromRoute] Guid id)
		{
			try
			{
				var res = await _tournamentService.EndAsync(id);
				return Ok(new ApiResponse<TournamentResponseDto> { Success = true, Data = res, Message = "Đã kết thúc giải đấu" });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "End tournament failed");
				return BadRequest(new ApiResponse<TournamentResponseDto> { Success = false, Message = ex.Message });
			}
		}

		[HttpPost("{id:guid}/join")]
		[SubscriptionAndRoleAuthorize("User", RequirePremiumContent = true)]
		public async Task<ActionResult<ApiResponse<object>>> Join([FromRoute] Guid id)
		{
			try
			{
				var accountIdClaim = User?.FindFirst("UserId")?.Value
					?? User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
					?? User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
				if (string.IsNullOrEmpty(accountIdClaim) || !Guid.TryParse(accountIdClaim, out var accountId))
				{
					return Unauthorized(new ApiResponse<object> { Success = false, Message = "Xác thực người dùng không hợp lệ" });
				}

				// Lấy UserId từ AccountId
				var user = await _userService.GetByAccountIdAsync(accountId);
				if (user == null)
				{
					return Unauthorized(new ApiResponse<object> { Success = false, Message = "Không tìm thấy người dùng cho tài khoản này" });
				}

				var ok = await _tournamentService.JoinAsync(id, user.Id);
				if (!ok) return BadRequest(new ApiResponse<object> { Success = false, Message = "Tham gia thất bại" });
				return Ok(new ApiResponse<object> { Success = true, Message = "Đã tham gia giải đấu" });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Join tournament failed");
				return BadRequest(new ApiResponse<object> { Success = false, Message = ex.Message });
			}
		}

		[HttpGet("{id:guid}/today")]
		[AllowAnonymous]
		public async Task<ActionResult<ApiResponse<TournamentTodaySetDto>>> GetToday([FromRoute] Guid id)
		{
			try
			{
				var res = await _tournamentService.GetTodaySetAsync(id);
				return Ok(new ApiResponse<TournamentTodaySetDto> { Success = true, Data = res, Message = "OK" });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Get today set failed");
				return BadRequest(new ApiResponse<TournamentTodaySetDto> { Success = false, Message = ex.Message });
			}
		}

		[HttpGet("{id:guid}/joined")]
		public async Task<ActionResult<ApiResponse<object>>> IsJoined([FromRoute] Guid id)
		{
			try
			{
				var accountIdClaim = User?.FindFirst("UserId")?.Value
					?? User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
					?? User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
				if (string.IsNullOrEmpty(accountIdClaim) || !Guid.TryParse(accountIdClaim, out var accountId))
				{
					return Unauthorized(new ApiResponse<object> { Success = false, Message = "Xác thực người dùng không hợp lệ" });
				}

				// Lấy UserId từ AccountId
				var user = await _userService.GetByAccountIdAsync(accountId);
				if (user == null)
				{
					return Unauthorized(new ApiResponse<object> { Success = false, Message = "Không tìm thấy người dùng cho tài khoản này" });
				}

				var isJoined = await _tournamentService.IsUserJoinedAsync(id, user.Id);
				return Ok(new ApiResponse<object> 
				{ 
					Success = true, 
					Data = new { IsJoined = isJoined },
					Message = isJoined ? "Người dùng đã tham gia giải đấu này" : "Người dùng chưa tham gia giải đấu này"
				});
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Check joined status failed");
				return BadRequest(new ApiResponse<object> { Success = false, Message = ex.Message });
			}
		}

		[HttpGet("{id:guid}/leaderboard")]
		[AllowAnonymous]
		public async Task<ActionResult<ApiResponse<object>>> GetLeaderboard([FromRoute] Guid id)
		{
			try
			{
				var leaderboardItems = await _tournamentService.GetLeaderboardAsync(id);
				var tournament = await _tournamentService.GetByIdAsync(id);
				if (tournament == null)
				{
					return BadRequest(new ApiResponse<object> { Success = false, Message = "Không tìm thấy giải đấu" });
				}

				// Tính điểm tích lũy theo từng ngày cho mỗi user
				var detailedLeaderboard = new List<object>();
				var tournamentStartDate = tournament.StartDate.Date;
				var tournamentEndDate = tournament.EndDate.Date;
				var now = DateTime.UtcNow.Date;
				var effectiveEndDate = now < tournamentEndDate ? now : tournamentEndDate;

				// leaderboardItems đã được sắp xếp theo điểm giảm dần và có Rank từ service
				// Chỉ cần thêm dailyScores vào response
				foreach (var item in leaderboardItems)
				{
					var dailyScores = await _tournamentService.GetUserDailyScoresAsync(id, item.UserId, tournamentStartDate, effectiveEndDate);
					
					detailedLeaderboard.Add(new
					{
						Rank = item.Rank, // Rank đã được tính từ service (sắp xếp theo TotalScore giảm dần)
						UserId = item.UserId,
						Username = item.Username,
						FullName = item.FullName,
						AvatarUrl = item.AvatarUrl,
						TotalScore = item.Score, // Tổng điểm (đã được sắp xếp giảm dần)
						JoinDate = item.Date,
						DailyScores = dailyScores // Điểm tích lũy theo từng ngày
					});
				}

				// Response đã được sắp xếp theo TotalScore giảm dần từ service
				// Nếu điểm bằng nhau, sắp xếp theo JoinDate (join sớm hơn xếp trước)
				return Ok(new ApiResponse<object> { Success = true, Data = detailedLeaderboard, Message = "OK" });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Get leaderboard failed");
				return BadRequest(new ApiResponse<object> { Success = false, Message = ex.Message });
			}
		}

		[HttpDelete("{id:guid}")]
		public async Task<ActionResult<ApiResponse<object>>> Delete([FromRoute] Guid id)
		{
			try
			{
				var res = await _tournamentService.DeleteAsync(id);
				if (!res)
				{
					return BadRequest(new ApiResponse<object> { Success = false, Message = "Xóa thất bại" });
				}
				return Ok(new ApiResponse<object> { Success = true, Message = "Đã xóa giải đấu thành công" });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Delete tournament failed");
				return BadRequest(new ApiResponse<object> { Success = false, Message = ex.Message });
			}
		}
	}
}


