using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BusinessLogic.Interfaces;
using BusinessLogic.DTOs.EventDtos;
using QuizUpLearn.API.Models;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace QuizUpLearn.API.Controllers
{
    /// <summary>
    /// API Controller cho Event Management
    /// Event sử dụng QuizSet với QuizSetType = Event
    /// Khi Start Event sẽ tạo GameRoom trong GameHub
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class EventController : ControllerBase
    {
        private readonly IEventService _eventService;
        private readonly IUserService _userService;
        private readonly IEventSchedulerService _schedulerService;
        private readonly ILogger<EventController> _logger;

        public EventController(
            IEventService eventService,
            IUserService userService,
            IEventSchedulerService schedulerService,
            ILogger<EventController> logger)
        {
            _eventService = eventService;
            _userService = userService;
            _schedulerService = schedulerService;
            _logger = logger;
        }

        /// <summary>
        /// Tạo Event mới (chỉ với QuizSet có QuizSetType = Event)
        /// </summary>
        [HttpPost("create")]
        public async Task<ActionResult<ApiResponse<EventResponseDto>>> CreateEvent([FromBody] CreateEventRequestDto dto)
        {
            try
            {
                var userId = await GetUserIdFromToken();
                if (userId == Guid.Empty)
                    return Unauthorized(new ApiResponse<EventResponseDto> { Success = false, Message = "User not authenticated" });

                var result = await _eventService.CreateEventAsync(userId, dto);
                return Ok(new ApiResponse<EventResponseDto> 
                { 
                    Success = true, 
                    Data = result, 
                    Message = "Event created successfully" 
                });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Create event validation failed");
                return BadRequest(new ApiResponse<EventResponseDto> { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Create event failed");
                return StatusCode(500, new ApiResponse<EventResponseDto> 
                { 
                    Success = false, 
                    Message = "An error occurred while creating event" 
                });
            }
        }

        /// <summary>
        /// Lấy thông tin Event theo ID
        /// </summary>
        [HttpGet("{id:guid}")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<EventResponseDto>>> GetEventById([FromRoute] Guid id)
        {
            try
            {
                var result = await _eventService.GetEventByIdAsync(id);
                if (result == null)
                    return NotFound(new ApiResponse<EventResponseDto> { Success = false, Message = "Event not found" });

                return Ok(new ApiResponse<EventResponseDto> { Success = true, Data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Get event {id} failed");
                return StatusCode(500, new ApiResponse<EventResponseDto> 
                { 
                    Success = false, 
                    Message = "An error occurred while retrieving event" 
                });
            }
        }

        /// <summary>
        /// Lấy tất cả Events
        /// </summary>
        [HttpGet("all")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<IEnumerable<EventResponseDto>>>> GetAllEvents()
        {
            try
            {
                var result = await _eventService.GetAllEventsAsync();
                return Ok(new ApiResponse<IEnumerable<EventResponseDto>> { Success = true, Data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Get all events failed");
                return StatusCode(500, new ApiResponse<IEnumerable<EventResponseDto>> 
                { 
                    Success = false, 
                    Message = "An error occurred while retrieving events" 
                });
            }
        }

        /// <summary>
        /// Lấy các Events đang Active
        /// </summary>
        [HttpGet("active")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<IEnumerable<EventResponseDto>>>> GetActiveEvents()
        {
            try
            {
                var result = await _eventService.GetActiveEventsAsync();
                return Ok(new ApiResponse<IEnumerable<EventResponseDto>> { Success = true, Data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Get active events failed");
                return StatusCode(500, new ApiResponse<IEnumerable<EventResponseDto>> 
                { 
                    Success = false, 
                    Message = "An error occurred while retrieving active events" 
                });
            }
        }

        /// <summary>
        /// Lấy các Events sắp diễn ra
        /// </summary>
        [HttpGet("upcoming")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<IEnumerable<EventResponseDto>>>> GetUpcomingEvents()
        {
            try
            {
                var result = await _eventService.GetUpcomingEventsAsync();
                return Ok(new ApiResponse<IEnumerable<EventResponseDto>> { Success = true, Data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Get upcoming events failed");
                return StatusCode(500, new ApiResponse<IEnumerable<EventResponseDto>> 
                { 
                    Success = false, 
                    Message = "An error occurred while retrieving upcoming events" 
                });
            }
        }

        /// <summary>
        /// Lấy các Events của tôi (đã tạo)
        /// </summary>
        [HttpGet("my-events")]
        public async Task<ActionResult<ApiResponse<IEnumerable<EventResponseDto>>>> GetMyEvents()
        {
            try
            {
                var userId = await GetUserIdFromToken();
                if (userId == Guid.Empty)
                    return Unauthorized(new ApiResponse<IEnumerable<EventResponseDto>> { Success = false, Message = "User not authenticated" });

                var result = await _eventService.GetMyEventsAsync(userId);
                return Ok(new ApiResponse<IEnumerable<EventResponseDto>> { Success = true, Data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Get my events failed");
                return StatusCode(500, new ApiResponse<IEnumerable<EventResponseDto>> 
                { 
                    Success = false, 
                    Message = "An error occurred while retrieving your events" 
                });
            }
        }

        /// <summary>
        /// Cập nhật Event
        /// </summary>
        [HttpPut("{id:guid}")]
        public async Task<ActionResult<ApiResponse<EventResponseDto>>> UpdateEvent([FromRoute] Guid id, [FromBody] UpdateEventRequestDto dto)
        {
            try
            {
                var result = await _eventService.UpdateEventAsync(id, dto);
                if (result == null)
                    return NotFound(new ApiResponse<EventResponseDto> { Success = false, Message = "Event not found" });

                return Ok(new ApiResponse<EventResponseDto> 
                { 
                    Success = true, 
                    Data = result, 
                    Message = "Event updated successfully" 
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, $"Update event {id} operation invalid");
                return BadRequest(new ApiResponse<EventResponseDto> { Success = false, Message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, $"Update event {id} validation failed");
                return BadRequest(new ApiResponse<EventResponseDto> { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Update event {id} failed");
                return StatusCode(500, new ApiResponse<EventResponseDto> 
                { 
                    Success = false, 
                    Message = "An error occurred while updating event" 
                });
            }
        }

        /// <summary>
        /// Xóa Event
        /// </summary>
        [HttpDelete("{id:guid}")]
        public async Task<ActionResult<ApiResponse<object>>> DeleteEvent([FromRoute] Guid id)
        {
            try
            {
                var result = await _eventService.DeleteEventAsync(id);
                if (!result)
                    return NotFound(new ApiResponse<object> { Success = false, Message = "Event not found" });

                return Ok(new ApiResponse<object> { Success = true, Message = "Event deleted successfully" });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, $"Delete event {id} operation invalid");
                return BadRequest(new ApiResponse<object> { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Delete event {id} failed");
                return StatusCode(500, new ApiResponse<object> 
                { 
                    Success = false, 
                    Message = "An error occurred while deleting event" 
                });
            }
        }

        /// <summary>
        /// ✨ START EVENT - Tạo GameRoom trong GameHub
        /// Trả về GamePin để participants có thể join
        /// </summary>
        [HttpPost("start")]
        public async Task<ActionResult<ApiResponse<StartEventResponseDto>>> StartEvent([FromBody] StartEventRequestDto dto)
        {
            try
            {
                var userId = await GetUserIdFromToken();
                if (userId == Guid.Empty)
                    return Unauthorized(new ApiResponse<StartEventResponseDto> { Success = false, Message = "User not authenticated" });

                var result = await _eventService.StartEventAsync(userId, dto);
                
                _logger.LogInformation($"Event {dto.EventId} started successfully with GamePin: {result.GamePin}");

                return Ok(new ApiResponse<StartEventResponseDto> 
                { 
                    Success = true, 
                    Data = result, 
                    Message = $"Event started successfully! GamePin: {result.GamePin}" 
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, $"Unauthorized start event attempt");
                return Unauthorized(new ApiResponse<StartEventResponseDto> { Success = false, Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, $"Start event {dto.EventId} operation invalid");
                return BadRequest(new ApiResponse<StartEventResponseDto> { Success = false, Message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, $"Start event {dto.EventId} validation failed");
                return BadRequest(new ApiResponse<StartEventResponseDto> { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Start event {dto.EventId} failed");
                return StatusCode(500, new ApiResponse<StartEventResponseDto> 
                { 
                    Success = false, 
                    Message = "An error occurred while starting event" 
                });
            }
        }

        /// <summary>
        /// Lấy danh sách participants của Event
        /// </summary>
        [HttpGet("{id:guid}/participants")]
        public async Task<ActionResult<ApiResponse<IEnumerable<EventParticipantResponseDto>>>> GetEventParticipants([FromRoute] Guid id)
        {
            try
            {
                var result = await _eventService.GetEventParticipantsAsync(id);
                return Ok(new ApiResponse<IEnumerable<EventParticipantResponseDto>> { Success = true, Data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Get event {id} participants failed");
                return StatusCode(500, new ApiResponse<IEnumerable<EventParticipantResponseDto>> 
                { 
                    Success = false, 
                    Message = "An error occurred while retrieving participants" 
                });
            }
        }

        /// <summary>
        /// 🏆 Lấy Leaderboard của Event - Bảng xếp hạng participants
        /// </summary>
        [HttpGet("{id:guid}/leaderboard")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<EventLeaderboardResponseDto>>> GetEventLeaderboard([FromRoute] Guid id)
        {
            try
            {
                var result = await _eventService.GetEventLeaderboardAsync(id);
                return Ok(new ApiResponse<EventLeaderboardResponseDto> 
                { 
                    Success = true, 
                    Data = result,
                    Message = "Leaderboard retrieved successfully" 
                });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, $"Get leaderboard for event {id} validation failed");
                return NotFound(new ApiResponse<EventLeaderboardResponseDto> 
                { 
                    Success = false, 
                    Message = ex.Message 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Get leaderboard for event {id} failed");
                return StatusCode(500, new ApiResponse<EventLeaderboardResponseDto> 
                { 
                    Success = false, 
                    Message = "An error occurred while retrieving leaderboard" 
                });
            }
        }

        /// <summary>
        /// Join Event (đăng ký tham gia)
        /// </summary>
        [HttpPost("{id:guid}/join")]
        public async Task<ActionResult<ApiResponse<object>>> JoinEvent([FromRoute] Guid id)
        {
            try
            {
                var userId = await GetUserIdFromToken();
                if (userId == Guid.Empty)
                    return Unauthorized(new ApiResponse<object> { Success = false, Message = "User not authenticated" });

                var result = await _eventService.JoinEventAsync(id, userId);
                return Ok(new ApiResponse<object> { Success = true, Message = "Joined event successfully" });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, $"Join event {id} operation invalid");
                return BadRequest(new ApiResponse<object> { Success = false, Message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, $"Join event {id} validation failed");
                return BadRequest(new ApiResponse<object> { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Join event {id} failed");
                return StatusCode(500, new ApiResponse<object> 
                { 
                    Success = false, 
                    Message = "An error occurred while joining event" 
                });
            }
        }

        /// <summary>
        /// Check if current user has joined the Event
        /// </summary>
        [HttpGet("{id:guid}/joined")]
        public async Task<ActionResult<ApiResponse<object>>> IsJoined([FromRoute] Guid id)
        {
            try
            {
                var userId = await GetUserIdFromToken();
                if (userId == Guid.Empty)
                    return Unauthorized(new ApiResponse<object> { Success = false, Message = "User not authenticated" });

                var isJoined = await _eventService.IsUserJoinedAsync(id, userId);
                return Ok(new ApiResponse<object> 
                { 
                    Success = true, 
                    Data = new { IsJoined = isJoined },
                    Message = isJoined ? "User has joined this event" : "User has not joined this event"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Check joined status for event {id} failed");
                return StatusCode(500, new ApiResponse<object> 
                { 
                    Success = false, 
                    Message = "An error occurred while checking joined status" 
                });
            }
        }

        /// <summary>
        /// 📊 Lấy statistics của Event Scheduler (Admin only)
        /// </summary>
        [HttpGet("scheduler/statistics")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<SchedulerStatistics>>> GetSchedulerStatistics()
        {
            try
            {
                var stats = await _schedulerService.GetStatisticsAsync();
                return Ok(new ApiResponse<SchedulerStatistics>
                {
                    Success = true,
                    Data = stats,
                    Message = "Scheduler statistics retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get scheduler statistics");
                return StatusCode(500, new ApiResponse<SchedulerStatistics>
                {
                    Success = false,
                    Message = "An error occurred while retrieving scheduler statistics"
                });
            }
        }

        /// <summary>
        /// ⚡ Force trigger scheduler check ngay lập tức (Admin only)
        /// Useful for testing or manual intervention
        /// </summary>
        [HttpPost("scheduler/trigger")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<object>>> TriggerSchedulerCheck()
        {
            try
            {
                await _schedulerService.TriggerCheckNowAsync();
                
                _logger.LogInformation("Scheduler manual trigger initiated by admin");

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Scheduler check triggered successfully. Check will run shortly."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to trigger scheduler check");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while triggering scheduler check"
                });
            }
        }

        /// <summary>
        /// Helper method để lấy UserId từ JWT token
        /// </summary>
        private async Task<Guid> GetUserIdFromToken()
        {
            try
            {
                var accountIdClaim = User?.FindFirst("UserId")?.Value
                    ?? User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

                if (string.IsNullOrEmpty(accountIdClaim) || !Guid.TryParse(accountIdClaim, out var accountId))
                    return Guid.Empty;

                var user = await _userService.GetByAccountIdAsync(accountId);
                return user?.Id ?? Guid.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get user ID from token");
                return Guid.Empty;
            }
        }
    }
}

