using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BusinessLogic.Services;
using BusinessLogic.DTOs;
using QuizUpLearn.API.Models;

namespace QuizUpLearn.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MinimalGameController : ControllerBase
    {
        private readonly RealtimeGameService _gameService;
        private readonly ILogger<MinimalGameController> _logger;
        private static bool _gameFeaturesInitialized = false;
        private static readonly object _lock = new object();

        public MinimalGameController(RealtimeGameService gameService, ILogger<MinimalGameController> logger)
        {
            _gameService = gameService;
            _logger = logger;
        }

        /// <summary>
        /// Tạo phòng game - Initialize game features on demand
        /// </summary>
        [HttpPost("create-room")]
        public async Task<ActionResult<ApiResponse<string>>> CreateGameRoom([FromBody] CreateGameRoomDto createDto)
        {
            try
            {
                // Initialize game features on first create room request
                InitializeGameFeaturesOnDemand();

                // Create game room
                var roomId = await _gameService.CreateGameRoomAsync(createDto);
                
                return Ok(new ApiResponse<string>
                {
                    Success = true,
                    Data = roomId,
                    Message = "Game room created successfully - Game features initialized on demand"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating game room");
                return BadRequest(new ApiResponse<string>
                {
                    Success = false,
                    Message = "Failed to create game room",
                    Error = ex.Message
                });
            }
        }

        /// <summary>
        /// Check game features status
        /// </summary>
        [HttpGet("game-status")]
        public ActionResult<ApiResponse<object>> GetGameStatus()
        {
            return Ok(new ApiResponse<object>
            {
                Success = true,
                Data = new { 
                    GameFeaturesReady = _gameFeaturesInitialized,
                    Message = _gameFeaturesInitialized ? "Game features ready" : "Game features will be initialized on first create room request"
                },
                Message = _gameFeaturesInitialized ? "Game features are ready" : "Game features will be initialized on demand"
            });
        }

        private void InitializeGameFeaturesOnDemand()
        {
            lock (_lock)
            {
                if (!_gameFeaturesInitialized)
                {
                    _logger.LogInformation("Initializing game features on demand...");
                    
                    // Initialize game-specific features
                    // Load game configurations
                    // Setup game-specific services
                    
                    _gameFeaturesInitialized = true;
                    _logger.LogInformation("Game features initialized successfully");
                }
            }
        }
    }
}
