using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BusinessLogic.Interfaces;
using BusinessLogic.DTOs;
using QuizUpLearn.API.Models;

namespace QuizUpLearn.API.Controllers
{
    /// <summary>
    /// API Controller cho game 1vs1
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class OneVsOneController : ControllerBase
    {
        private readonly IOneVsOneGameService _gameService;
        private readonly ILogger<OneVsOneController> _logger;

        public OneVsOneController(IOneVsOneGameService gameService, ILogger<OneVsOneController> logger)
        {
            _gameService = gameService;
            _logger = logger;
        }

        /// <summary>
        /// Player1 tạo phòng 1vs1 và nhận Room PIN
        /// </summary>
        /// <param name="dto">Thông tin phòng (QuizSetId, Player1Name)</param>
        /// <returns>Room PIN (6 chữ số) để share cho Player2</returns>
        [HttpPost("create")]
        public async Task<ActionResult<ApiResponse<CreateOneVsOneRoomResponseDto>>> CreateRoom([FromBody] CreateOneVsOneRoomDto dto)
        {
            try
            {
                var response = await _gameService.CreateRoomAsync(dto);
                
                _logger.LogInformation($"1v1 Room created with PIN: {response.RoomPin} by Player: {dto.Player1Name}");

                return Ok(new ApiResponse<CreateOneVsOneRoomResponseDto>
                {
                    Success = true,
                    Data = response,
                    Message = $"Room created successfully. Share PIN: {response.RoomPin} to your opponent"
                });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid quiz set");
                return BadRequest(new ApiResponse<CreateOneVsOneRoomResponseDto>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating room");
                return StatusCode(500, new ApiResponse<CreateOneVsOneRoomResponseDto>
                {
                    Success = false,
                    Message = "An error occurred while creating the room"
                });
            }
        }

        /// <summary>
        /// Lấy thông tin phòng (cho cả 2 players monitor)
        /// </summary>
        /// <param name="roomPin">Room PIN (6 chữ số)</param>
        /// <returns>Thông tin phòng: players, status, v.v.</returns>
        [HttpGet("room/{roomPin}")]
        [AllowAnonymous] // Cho phép anonymous để players có thể check room tồn tại
        public async Task<ActionResult<ApiResponse<OneVsOneRoomDto>>> GetRoom(string roomPin)
        {
            try
            {
                var room = await _gameService.GetRoomAsync(roomPin);
                if (room == null)
                {
                    return NotFound(new ApiResponse<OneVsOneRoomDto>
                    {
                        Success = false,
                        Message = "Room not found"
                    });
                }

                return Ok(new ApiResponse<OneVsOneRoomDto>
                {
                    Success = true,
                    Data = room
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting room for PIN: {roomPin}");
                return StatusCode(500, new ApiResponse<OneVsOneRoomDto>
                {
                    Success = false,
                    Message = "An error occurred while retrieving room"
                });
            }
        }

        /// <summary>
        /// Kiểm tra Room PIN có tồn tại và có thể join được không
        /// </summary>
        /// <param name="roomPin">Room PIN</param>
        /// <returns>True nếu room tồn tại và đang ở trạng thái Waiting</returns>
        [HttpGet("validate/{roomPin}")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<bool>>> ValidateRoomPin(string roomPin)
        {
            try
            {
                var room = await _gameService.GetRoomAsync(roomPin);
                
                if (room == null)
                {
                    return Ok(new ApiResponse<bool>
                    {
                        Success = false,
                        Data = false,
                        Message = "Room not found"
                    });
                }

                if (room.Status != OneVsOneRoomStatus.Waiting)
                {
                    return Ok(new ApiResponse<bool>
                    {
                        Success = false,
                        Data = false,
                        Message = "Room is not available to join (already started or full)"
                    });
                }

                if (room.Player2 != null)
                {
                    return Ok(new ApiResponse<bool>
                    {
                        Success = false,
                        Data = false,
                        Message = "Room is full"
                    });
                }

                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Data = true,
                    Message = "Room is available to join"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error validating room PIN: {roomPin}");
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Data = false,
                    Message = "An error occurred while validating room"
                });
            }
        }
    }
}

