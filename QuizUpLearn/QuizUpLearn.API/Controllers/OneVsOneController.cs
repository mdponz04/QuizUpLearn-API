using BusinessLogic.DTOs;
using BusinessLogic.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizUpLearn.API.Models;
using Repository.Enums;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace QuizUpLearn.API.Controllers
{
    /// <summary>
    /// API Controller cho game 1vs1 và Multiplayer
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class OneVsOneController : ControllerBase
    {
        private readonly IOneVsOneGameService _gameService;
        private readonly IUserService _userService;
        private readonly ILogger<OneVsOneController> _logger;

        public OneVsOneController(
            IOneVsOneGameService gameService, 
            IUserService userService,
            ILogger<OneVsOneController> logger)
        {
            _gameService = gameService;
            _userService = userService;
            _logger = logger;
        }

        [HttpPost("create")]
        public async Task<ActionResult<ApiResponse<CreateOneVsOneRoomResponseDto>>> CreateRoom([FromBody] CreateOneVsOneRoomDto dto)
        {
            try
            {
                // Lấy Account ID từ JWT token
                var accountIdClaim = User?.FindFirst("UserId")?.Value 
                    ?? User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
                
                if (string.IsNullOrEmpty(accountIdClaim) || !Guid.TryParse(accountIdClaim, out var accountId))
                {
                    return Unauthorized(new ApiResponse<CreateOneVsOneRoomResponseDto>
                    {
                        Success = false,
                        Message = "Invalid user authentication. Account ID not found in token."
                    });
                }

                // Map Account ID → User ID
                var user = await _userService.GetByAccountIdAsync(accountId);
                if (user == null)
                {
                    return BadRequest(new ApiResponse<CreateOneVsOneRoomResponseDto>
                    {
                        Success = false,
                        Message = "User not found for this account."
                    });
                }

                // Set Player1UserId từ JWT token
                dto.Player1UserId = user.Id;

                var response = await _gameService.CreateRoomAsync(dto);
                
                var modeText = dto.Mode == GameModeEnum.OneVsOne ? "1vs1" : "Multiplayer";
                _logger.LogInformation($"{modeText} Room created with PIN: {response.RoomPin} by Player: {dto.Player1Name}");

                return Ok(new ApiResponse<CreateOneVsOneRoomResponseDto>
                {
                    Success = true,
                    Data = response,
                    Message = $"{modeText} room created successfully. Share PIN: {response.RoomPin}"
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

                // Check status
                if (room.Status != OneVsOneRoomStatus.Waiting && room.Status != OneVsOneRoomStatus.Ready)
                {
                    return Ok(new ApiResponse<bool>
                    {
                        Success = false,
                        Data = false,
                        Message = "Room is not available to join (already started or cancelled)"
                    });
                }

                // Check MaxPlayers
                if (room.MaxPlayers.HasValue && room.Players.Count >= room.MaxPlayers.Value)
                {
                    return Ok(new ApiResponse<bool>
                    {
                        Success = false,
                        Data = false,
                        Message = $"Room is full ({room.Players.Count}/{room.MaxPlayers.Value} players)"
                    });
                }

                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Data = true,
                    Message = $"Room is available to join ({room.Players.Count}/{room.MaxPlayers?.ToString() ?? "∞"} players)"
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

