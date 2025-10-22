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
        /// Tạo phòng game mới
        /// </summary>
        [HttpPost("create-room")]
        public async Task<ActionResult<ApiResponse<string>>> CreateGameRoom([FromBody] CreateGameRoomDto createDto)
        {
            try
            {
                var roomId = await _gameService.CreateGameRoomAsync(createDto);
                return Ok(new ApiResponse<string>
                {
                    Success = true,
                    Data = roomId,
                    Message = "Game room created successfully"
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
        /// Tham gia phòng game
        /// </summary>
        [HttpPost("join-room")]
        public async Task<ActionResult<ApiResponse<bool>>> JoinGameRoom([FromBody] JoinGameRoomDto joinDto)
        {
            try
            {
                var success = await _gameService.JoinGameRoomAsync(joinDto);
                if (success)
                {
                    return Ok(new ApiResponse<bool>
                    {
                        Success = true,
                        Data = true,
                        Message = "Successfully joined the game room"
                    });
                }
                else
                {
                    return BadRequest(new ApiResponse<bool>
                    {
                        Success = false,
                        Data = false,
                        Message = "Failed to join the game room"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error joining game room");
                return BadRequest(new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Failed to join game room",
                    Error = ex.Message
                });
            }
        }

        /// <summary>
        /// Rời khỏi phòng game
        /// </summary>
        [HttpPost("leave-room")]
        public async Task<ActionResult<ApiResponse<bool>>> LeaveGameRoom([FromBody] LeaveGameDto leaveDto)
        {
            try
            {
                var success = await _gameService.LeaveGameRoomAsync(leaveDto);
                if (success)
                {
                    return Ok(new ApiResponse<bool>
                    {
                        Success = true,
                        Data = true,
                        Message = "Successfully left the game room"
                    });
                }
                else
                {
                    return BadRequest(new ApiResponse<bool>
                    {
                        Success = false,
                        Data = false,
                        Message = "Failed to leave the game room"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error leaving game room");
                return BadRequest(new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Failed to leave game room",
                    Error = ex.Message
                });
            }
        }

        /// <summary>
        /// Lấy thông tin phòng game
        /// </summary>
        [HttpGet("room/{roomId}")]
        public async Task<ActionResult<ApiResponse<GameRoomInfoDto>>> GetGameRoomInfo(string roomId)
        {
            try
            {
                var roomInfo = await _gameService.GetGameRoomInfoAsync(roomId);
                if (roomInfo != null)
                {
                    return Ok(new ApiResponse<GameRoomInfoDto>
                    {
                        Success = true,
                        Data = roomInfo,
                        Message = "Game room information retrieved successfully"
                    });
                }
                else
                {
                    return NotFound(new ApiResponse<GameRoomInfoDto>
                    {
                        Success = false,
                        Message = "Game room not found"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting game room info");
                return BadRequest(new ApiResponse<GameRoomInfoDto>
                {
                    Success = false,
                    Message = "Failed to get game room information",
                    Error = ex.Message
                });
            }
        }

        /// <summary>
        /// Lấy danh sách phòng có sẵn
        /// </summary>
        [HttpGet("available-rooms")]
        public async Task<ActionResult<ApiResponse<List<GameRoomInfoDto>>>> GetAvailableRooms()
        {
            try
            {
                var rooms = await _gameService.GetAvailableRoomsAsync();
                return Ok(new ApiResponse<List<GameRoomInfoDto>>
                {
                    Success = true,
                    Data = rooms,
                    Message = "Available rooms retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available rooms");
                return BadRequest(new ApiResponse<List<GameRoomInfoDto>>
                {
                    Success = false,
                    Message = "Failed to get available rooms",
                    Error = ex.Message
                });
            }
        }

        /// <summary>
        /// Bắt đầu game
        /// </summary>
        [HttpPost("start-game")]
        public async Task<ActionResult<ApiResponse<bool>>> StartGame([FromBody] StartGameDto startDto)
        {
            try
            {
                var success = await _gameService.StartGameAsync(startDto);
                if (success)
                {
                    return Ok(new ApiResponse<bool>
                    {
                        Success = true,
                        Data = true,
                        Message = "Game started successfully"
                    });
                }
                else
                {
                    return BadRequest(new ApiResponse<bool>
                    {
                        Success = false,
                        Data = false,
                        Message = "Failed to start the game"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting game");
                return BadRequest(new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Failed to start game",
                    Error = ex.Message
                });
            }
        }

        /// <summary>
        /// Gửi câu trả lời
        /// </summary>
        [HttpPost("submit-answer")]
        public async Task<ActionResult<ApiResponse<bool>>> SubmitAnswer([FromBody] SubmitAnswerDto answerDto)
        {
            try
            {
                var success = await _gameService.SubmitAnswerAsync(answerDto);
                if (success)
                {
                    return Ok(new ApiResponse<bool>
                    {
                        Success = true,
                        Data = true,
                        Message = "Answer submitted successfully"
                    });
                }
                else
                {
                    return BadRequest(new ApiResponse<bool>
                    {
                        Success = false,
                        Data = false,
                        Message = "Failed to submit answer"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting answer");
                return BadRequest(new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Failed to submit answer",
                    Error = ex.Message
                });
            }
        }

        /// <summary>
        /// Lấy kết quả game
        /// </summary>
        [HttpGet("game-result/{roomId}")]
        public async Task<ActionResult<ApiResponse<GameResultDto>>> GetGameResult(string roomId)
        {
            try
            {
                var result = await _gameService.GetGameResultAsync(roomId);
                if (result != null)
                {
                    return Ok(new ApiResponse<GameResultDto>
                    {
                        Success = true,
                        Data = result,
                        Message = "Game result retrieved successfully"
                    });
                }
                else
                {
                    return NotFound(new ApiResponse<GameResultDto>
                    {
                        Success = false,
                        Message = "Game result not found or game not completed"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting game result");
                return BadRequest(new ApiResponse<GameResultDto>
                {
                    Success = false,
                    Message = "Failed to get game result",
                    Error = ex.Message
                });
            }
        }
    }
}
