using BusinessLogic.DTOs;
using BusinessLogic.DTOs.EventDtos;
using BusinessLogic.DTOs.NotificationDtos;
using BusinessLogic.DTOs.UserNotificationDtos;
using Microsoft.AspNetCore.SignalR;
using QuizUpLearn.API.Hubs;
using Repository.Enums;
using BusinessLogic.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizUpLearn.API.Attributes;
using QuizUpLearn.API.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace QuizUpLearn.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class EventController : ControllerBase
    {
        private readonly IEventService _eventService;
        private readonly IUserService _userService;
        private readonly IEventSchedulerService _schedulerService;
        private readonly ILogger<EventController> _logger;
        private readonly IWorkerService _workerService;

        public EventController(
            IEventService eventService,
            IUserService userService,
            IEventSchedulerService schedulerService,
            ILogger<EventController> logger,
            IWorkerService workerService)
        {
            _eventService = eventService;
            _userService = userService;
            _schedulerService = schedulerService;
            _logger = logger;
            _workerService = workerService;
        }

        /// <summary>
        /// Tạo Event mới (chỉ với QuizSet có QuizSetType = Event)
        /// </summary>
        [HttpPost("create")]
        [SubscriptionAndRoleAuthorize("Moderator")]
        public async Task<ActionResult<ApiResponse<EventResponseDto>>> CreateEvent([FromBody] CreateEventRequestDto dto)
        {
            try
            {
                var userId = await GetUserIdFromToken();
                if (userId == Guid.Empty)
                    return Unauthorized(new ApiResponse<EventResponseDto> { Success = false, Message = "Người dùng chưa được xác thực" });

                var result = await _eventService.CreateEventAsync(userId, dto);
                return Ok(new ApiResponse<EventResponseDto> 
                { 
                    Success = true, 
                    Data = result, 
                    Message = "Tạo sự kiện thành công" 
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
                    Message = "Đã xảy ra lỗi khi tạo sự kiện" 
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
                    return NotFound(new ApiResponse<EventResponseDto> { Success = false, Message = "Không tìm thấy sự kiện" });

                return Ok(new ApiResponse<EventResponseDto> { Success = true, Data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Get event {id} failed");
                return StatusCode(500, new ApiResponse<EventResponseDto> 
                { 
                    Success = false, 
                    Message = "Đã xảy ra lỗi khi lấy thông tin sự kiện" 
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
                    Message = "Đã xảy ra lỗi khi lấy danh sách sự kiện" 
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
                    Message = "Đã xảy ra lỗi khi lấy danh sách sự kiện đang diễn ra" 
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
                    Message = "Đã xảy ra lỗi khi lấy danh sách sự kiện sắp diễn ra" 
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
                    return Unauthorized(new ApiResponse<IEnumerable<EventResponseDto>> { Success = false, Message = "Người dùng chưa được xác thực" });

                var result = await _eventService.GetMyEventsAsync(userId);
                return Ok(new ApiResponse<IEnumerable<EventResponseDto>> { Success = true, Data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Get my events failed");
                return StatusCode(500, new ApiResponse<IEnumerable<EventResponseDto>> 
                { 
                    Success = false, 
                    Message = "Đã xảy ra lỗi khi lấy danh sách sự kiện của bạn" 
                });
            }
        }

        /// <summary>
        /// Cập nhật Event
        /// </summary>
        [HttpPut("{id:guid}")]
        [SubscriptionAndRoleAuthorize("Moderator")]
        public async Task<ActionResult<ApiResponse<EventResponseDto>>> UpdateEvent([FromRoute] Guid id, [FromBody] UpdateEventRequestDto dto)
        {
            try
            {
                var result = await _eventService.UpdateEventAsync(id, dto);
                if (result == null)
                    return NotFound(new ApiResponse<EventResponseDto> { Success = false, Message = "Không tìm thấy sự kiện" });

                return Ok(new ApiResponse<EventResponseDto> 
                { 
                    Success = true, 
                    Data = result, 
                    Message = "Cập nhật sự kiện thành công" 
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
                    Message = "Đã xảy ra lỗi khi cập nhật sự kiện" 
                });
            }
        }

        /// <summary>
        /// Xóa Event
        /// </summary>
        [HttpDelete("{id:guid}")]
        [SubscriptionAndRoleAuthorize("Moderator")]
        public async Task<ActionResult<ApiResponse<object>>> DeleteEvent([FromRoute] Guid id)
        {
            try
            {
                var result = await _eventService.DeleteEventAsync(id);
                if (!result)
                    return NotFound(new ApiResponse<object> { Success = false, Message = "Không tìm thấy sự kiện" });

                return Ok(new ApiResponse<object> { Success = true, Message = "Xóa sự kiện thành công" });
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
                    Message = "Đã xảy ra lỗi khi xóa sự kiện" 
                });
            }
        }

        /// <summary>
        /// ✨ START EVENT - Tạo GameRoom trong GameHub
        /// Trả về GamePin để participants có thể join
        /// </summary>
        [HttpPost("start")]
        [SubscriptionAndRoleAuthorize("Moderator")]
        public async Task<ActionResult<ApiResponse<StartEventResponseDto>>> StartEvent([FromBody] StartEventRequestDto dto)
        {
            try
            {
                var userId = await GetUserIdFromToken();
                if (userId == Guid.Empty)
                    return Unauthorized(new ApiResponse<StartEventResponseDto> { Success = false, Message = "Người dùng chưa được xác thực" });

                var result = await _eventService.StartEventAsync(userId, dto);
                var eventParticipants = await _eventService.GetEventParticipantsAsync(result.EventId);

                _ = _workerService.EnqueueJob(async (sp, token) =>
                {
                    var logger = sp.GetRequiredService<ILogger<EventController>>();
                    var notificationService = sp.GetRequiredService<INotificationService>();
                    var userNotificationService = sp.GetRequiredService<IUserNotificationService>();
                    var hubContext = sp.GetRequiredService<IHubContext<BackgroundJobHub>>();
                    
                    try
                    {
                        var notification = await notificationService.CreateAsync(new NotificationRequestDto
                        {
                            Title = $"Event {result.EventName}",
                            Message = $"Event {result.EventName} đã bắt đầu! Sử dụng GamePin: {result.GamePin} để tham gia ngay.",
                            Type = NotificationType.Event
                        });

                        var successfulNotifications = new List<Guid>();
                        var failedNotifications = new List<Guid>();

                        foreach (var participant in eventParticipants)
                        {
                            try
                            {
                                await userNotificationService.CreateAsync(new UserNotificationRequestDto
                                {
                                    UserId = participant.ParticipantId,
                                    NotificationId = notification.Id
                                });
                                
                                successfulNotifications.Add(participant.ParticipantId);

                                logger.LogInformation($"Notification sent successfully to user {participant.ParticipantId}");

                                await hubContext.Clients.Group($"user:{participant.ParticipantId}").SendAsync("NotificationCreated", new
                                {
                                    Message = "Notification send successfully"
                                });
                            }
                            catch (Exception ex)
                            {
                                failedNotifications.Add(participant.ParticipantId);
                                logger.LogError(ex, $"Failed to send notification to user {participant.ParticipantId}");
                            }
                        }

                        logger.LogInformation($"Notification completed. Success: {successfulNotifications.Count}, Failed: {failedNotifications.Count}");
                    }
                    catch (Exception ex)
                    {
                        logger.LogError($"Notification failed completely: " + ex.Message);
                    }
                });

                _logger.LogInformation($"Event {dto.EventId} started successfully with GamePin: {result.GamePin}");

                return Ok(new ApiResponse<StartEventResponseDto>
                { 
                    Success = true, 
                    Data = result, 
                    Message = $"Sự kiện đã bắt đầu thành công! GamePin: {result.GamePin}"
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
                    Message = "Đã xảy ra lỗi khi bắt đầu sự kiện" 
                });
            }
        }

        /// <summary>
        /// ✨ END EVENT - Kết thúc Event và tính toán rank cho participants
        /// </summary>
        [HttpPost("end")]
        [SubscriptionAndRoleAuthorize("Moderator")]
        public async Task<ActionResult<ApiResponse<EndEventResponseDto>>> EndEvent([FromBody] EndEventRequestDto dto)
        {
            try
            {
                var userId = await GetUserIdFromToken();
                if (userId == Guid.Empty)
                    return Unauthorized(new ApiResponse<EndEventResponseDto> { Success = false, Message = "Người dùng chưa được xác thực" });

                var result = await _eventService.EndEventAsync(userId, dto);
                
                _logger.LogInformation($"Event {dto.EventId} ended successfully");

                return Ok(new ApiResponse<EndEventResponseDto> 
                { 
                    Success = true, 
                    Data = result, 
                    Message = $"Sự kiện đã kết thúc thành công! Tổng số người tham gia: {result.TotalParticipants}" 
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, $"Unauthorized end event attempt");
                return Unauthorized(new ApiResponse<EndEventResponseDto> { Success = false, Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, $"End event {dto.EventId} operation invalid");
                return BadRequest(new ApiResponse<EndEventResponseDto> { Success = false, Message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, $"End event {dto.EventId} validation failed");
                return BadRequest(new ApiResponse<EndEventResponseDto> { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"End event {dto.EventId} failed");
                return StatusCode(500, new ApiResponse<EndEventResponseDto> 
                { 
                    Success = false, 
                    Message = "Đã xảy ra lỗi khi kết thúc sự kiện" 
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
                    Message = "Đã xảy ra lỗi khi lấy danh sách người tham gia" 
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
                    Message = "Lấy bảng xếp hạng thành công" 
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
                    Message = "Đã xảy ra lỗi khi lấy bảng xếp hạng" 
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
                    return Unauthorized(new ApiResponse<object> { Success = false, Message = "Người dùng chưa được xác thực" });

                var result = await _eventService.JoinEventAsync(id, userId);
                return Ok(new ApiResponse<object> { Success = true, Message = "Tham gia sự kiện thành công" });
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
                    Message = "Đã xảy ra lỗi khi tham gia sự kiện" 
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
                    return Unauthorized(new ApiResponse<object> { Success = false, Message = "Người dùng chưa được xác thực" });

                var isJoined = await _eventService.IsUserJoinedAsync(id, userId);
                return Ok(new ApiResponse<object> 
                { 
                    Success = true, 
                    Data = new { IsJoined = isJoined },
                    Message = isJoined ? "Bạn đã tham gia sự kiện này" : "Bạn chưa tham gia sự kiện này"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Check joined status for event {id} failed");
                return StatusCode(500, new ApiResponse<object> 
                { 
                    Success = false, 
                    Message = "Đã xảy ra lỗi khi kiểm tra trạng thái tham gia" 
                });
            }
        }

        /// <summary>
        /// 📊 Lấy statistics của Event Scheduler (Admin only)
        /// </summary>
        [HttpGet("scheduler/statistics")]
        [SubscriptionAndRoleAuthorize("Administrator")]
        public async Task<ActionResult<ApiResponse<SchedulerStatistics>>> GetSchedulerStatistics()
        {
            try
            {
                var stats = await _schedulerService.GetStatisticsAsync();
                return Ok(new ApiResponse<SchedulerStatistics>
                {
                    Success = true,
                    Data = stats,
                    Message = "Lấy thống kê lịch trình thành công"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get scheduler statistics");
                return StatusCode(500, new ApiResponse<SchedulerStatistics>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi lấy thống kê lịch trình"
                });
            }
        }

        /// <summary>
        /// ⚡ Force trigger scheduler check ngay lập tức (Admin only)
        /// Useful for testing or manual intervention
        /// </summary>
        [HttpPost("scheduler/trigger")]
        [SubscriptionAndRoleAuthorize("Administrator")]
        public async Task<ActionResult<ApiResponse<object>>> TriggerSchedulerCheck()
        {
            try
            {
                await _schedulerService.TriggerCheckNowAsync();
                
                _logger.LogInformation("Scheduler manual trigger initiated by admin");

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Kích hoạt kiểm tra lịch trình thành công. Kiểm tra sẽ chạy trong giây lát."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to trigger scheduler check");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi kích hoạt kiểm tra lịch trình"
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

