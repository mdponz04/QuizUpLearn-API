using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BusinessLogic.Interfaces;
using BusinessLogic.DTOs.TournamentDtos;
using QuizUpLearn.API.Models;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

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

		[HttpPost("create")]
		public async Task<ActionResult<ApiResponse<TournamentResponseDto>>> Create([FromBody] CreateTournamentRequestDto dto)
		{
			try
			{
				var res = await _tournamentService.CreateAsync(dto);
				return Ok(new ApiResponse<TournamentResponseDto> { Success = true, Data = res, Message = "Tournament created" });
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
				return Ok(new ApiResponse<TournamentResponseDto> { Success = true, Data = res, Message = "Quiz sets added" });
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
				return Ok(new ApiResponse<TournamentResponseDto> { Success = true, Data = res, Message = "Tournament started" });
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
				return Ok(new ApiResponse<TournamentResponseDto> { Success = true, Data = res, Message = "Tournament ended" });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "End tournament failed");
				return BadRequest(new ApiResponse<TournamentResponseDto> { Success = false, Message = ex.Message });
			}
		}

		[HttpPost("{id:guid}/join")]
		public async Task<ActionResult<ApiResponse<object>>> Join([FromRoute] Guid id)
		{
			try
			{
				var accountIdClaim = User?.FindFirst("UserId")?.Value
					?? User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
					?? User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
				if (string.IsNullOrEmpty(accountIdClaim) || !Guid.TryParse(accountIdClaim, out var accountId))
				{
					return Unauthorized(new ApiResponse<object> { Success = false, Message = "Invalid user authentication." });
				}

				// Lấy UserId từ AccountId
				var user = await _userService.GetByAccountIdAsync(accountId);
				if (user == null)
				{
					return Unauthorized(new ApiResponse<object> { Success = false, Message = "User not found for this account." });
				}

				var ok = await _tournamentService.JoinAsync(id, user.Id);
				if (!ok) return BadRequest(new ApiResponse<object> { Success = false, Message = "Join failed" });
				return Ok(new ApiResponse<object> { Success = true, Message = "Joined tournament" });
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

		[HttpDelete("{id:guid}")]
		public async Task<ActionResult<ApiResponse<object>>> Delete([FromRoute] Guid id)
		{
			try
			{
				var res = await _tournamentService.DeleteAsync(id);
				if (!res)
				{
					return BadRequest(new ApiResponse<object> { Success = false, Message = "Delete failed" });
				}
				return Ok(new ApiResponse<object> { Success = true, Message = "Tournament deleted successfully" });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Delete tournament failed");
				return BadRequest(new ApiResponse<object> { Success = false, Message = ex.Message });
			}
		}
	}
}


