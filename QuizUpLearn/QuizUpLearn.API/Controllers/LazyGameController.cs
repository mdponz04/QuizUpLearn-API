using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BusinessLogic.Services;
using BusinessLogic.DTOs;
using QuizUpLearn.API.Models;
using QuizUpLearn.API.Services;

namespace QuizUpLearn.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class LazyGameController : ControllerBase
    {
        private readonly LazyServerService _lazyServerService;
        private readonly RealtimeGameService _gameService;
        private readonly ILogger<LazyGameController> _logger;

        public LazyGameController(
            LazyServerService lazyServerService,
            RealtimeGameService gameService,
            ILogger<LazyGameController> logger)
        {
            _lazyServerService = lazyServerService;
            _gameService = gameService;
            _logger = logger;
        }

        /// <summary>
        /// Tạo phòng game - Chỉ tạo server khi user bấm create room
        /// </summary>
        [HttpPost("create-room")]
        public async Task<ActionResult<ApiResponse<string>>> CreateGameRoom([FromBody] CreateGameRoomDto createDto)
        {
            try
            {
                // 1. Initialize server on demand
                var serverReady = await _lazyServerService.InitializeServerOnDemand();
                if (!serverReady)
                {
                    return BadRequest(new ApiResponse<string>
                    {
                        Success = false,
                        Message = "Failed to initialize server components"
                    });
                }

                // 2. Create game room
                var roomId = await _gameService.CreateGameRoomAsync(createDto);
                
                return Ok(new ApiResponse<string>
                {
                    Success = true,
                    Data = roomId,
                    Message = "Game room created successfully - Server initialized on demand"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating game room with lazy server initialization");
                return BadRequest(new ApiResponse<string>
                {
                    Success = false,
                    Message = "Failed to create game room",
                    Error = ex.Message
                });
            }
        }

        /// <summary>
        /// Check server status
        /// </summary>
        [HttpGet("server-status")]
        public ActionResult<ApiResponse<object>> GetServerStatus()
        {
            var isReady = _lazyServerService.IsServerReady();
            return Ok(new ApiResponse<object>
            {
                Success = true,
                Data = new { ServerReady = isReady, Message = isReady ? "Server ready" : "Server not initialized" },
                Message = isReady ? "Server is ready" : "Server will be initialized on first create room request"
            });
        }
    }
}
