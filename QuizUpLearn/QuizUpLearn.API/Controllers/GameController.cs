using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BusinessLogic.Services;
using BusinessLogic.DTOs;
using QuizUpLearn.API.Models;
using Repository.Enums;

namespace QuizUpLearn.API.Controllers
{
    /// <summary>
    /// API Controller cho Kahoot-style Realtime Quiz Game
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class GameController : ControllerBase
    {
        private readonly RealtimeGameService _gameService;
        private readonly ILogger<GameController> _logger;

        public GameController(RealtimeGameService gameService, ILogger<GameController> logger)
        {
            _gameService = gameService;
            _logger = logger;
        }

        /// <summary>
        /// Host tạo game session mới và nhận Game PIN
        /// </summary>
        /// <param name="dto">Thông tin game (QuizSetId, TimePerQuestion)</param>
        /// <returns>Game PIN (6 chữ số) để players join</returns>
        [HttpPost("create")]
        public async Task<ActionResult<ApiResponse<CreateGameResponseDto>>> CreateGame([FromBody] CreateGameDto dto)
        {
            try
            {
                var response = await _gameService.CreateGameAsync(dto);
                
                _logger.LogInformation($"Game created with PIN: {response.GamePin} by Host: {dto.HostUserName}");

                return Ok(new ApiResponse<CreateGameResponseDto>
                {
                    Success = true,
                    Data = response,
                    Message = $"Game created successfully. Share PIN: {response.GamePin}"
                });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid quiz set");
                return BadRequest(new ApiResponse<CreateGameResponseDto>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating game");
                return StatusCode(500, new ApiResponse<CreateGameResponseDto>
                {
                    Success = false,
                    Message = "An error occurred while creating the game"
                });
            }
        }

        /// <summary>
        /// Lấy thông tin game session (cho Host monitor lobby)
        /// </summary>
        /// <param name="gamePin">Game PIN (6 chữ số)</param>
        /// <returns>Thông tin lobby: số người chơi, trạng thái, v.v.</returns>
        [HttpGet("session/{gamePin}")]
        [AllowAnonymous] // Cho phép anonymous để players có thể check game tồn tại
        public async Task<ActionResult<ApiResponse<GameSessionDto>>> GetGameSession(string gamePin)
        {
            try
            {
                var session = await _gameService.GetGameSessionAsync(gamePin);
                if (session == null)
                {
                    return NotFound(new ApiResponse<GameSessionDto>
                    {
                        Success = false,
                        Message = "Game not found"
                    });
                }

                return Ok(new ApiResponse<GameSessionDto>
                {
                    Success = true,
                    Data = session
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting game session for PIN: {gamePin}");
                return StatusCode(500, new ApiResponse<GameSessionDto>
                {
                    Success = false,
                    Message = "An error occurred while retrieving game session"
                });
            }
        }

        /// <summary>
        /// Kiểm tra Game PIN có tồn tại và có thể join được không
        /// </summary>
        /// <param name="gamePin">Game PIN</param>
        /// <returns>True nếu game tồn tại và đang ở lobby</returns>
        [HttpGet("validate/{gamePin}")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<bool>>> ValidateGamePin(string gamePin)
        {
            try
            {
                var session = await _gameService.GetGameSessionAsync(gamePin);
                
                if (session == null)
                {
                    return Ok(new ApiResponse<bool>
                    {
                        Success = false,
                        Data = false,
                        Message = "Game not found"
                    });
                }

                if (session.Status != GameStatus.Lobby)
                {
                    return Ok(new ApiResponse<bool>
                    {
                        Success = false,
                        Data = false,
                        Message = "Game has already started"
                    });
                }

                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Data = true,
                    Message = "Game is available to join"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error validating game PIN: {gamePin}");
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Data = false,
                    Message = "An error occurred while validating game"
                });
            }
        }
    }
}
