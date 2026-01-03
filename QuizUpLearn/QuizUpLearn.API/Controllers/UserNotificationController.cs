using Microsoft.AspNetCore.Mvc;
using BusinessLogic.DTOs.UserNotificationDtos;
using BusinessLogic.Interfaces;
using Microsoft.AspNetCore.Authorization;
using QuizUpLearn.API.Attributes;

namespace QuizUpLearn.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UserNotificationController : ControllerBase
    {
        private readonly IUserNotificationService _userNotificationService;

        public UserNotificationController(IUserNotificationService userNotificationService)
        {
            _userNotificationService = userNotificationService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var userNotifications = await _userNotificationService.GetAllAsync();
            return Ok(userNotifications);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var userNotification = await _userNotificationService.GetByIdAsync(id);
            if (userNotification == null)
            {
                return NotFound("User notification not found");
            }
            return Ok(userNotification);
        }

        [HttpGet("user")]
        [SubscriptionAndRoleAuthorize]
        public async Task<IActionResult> GetByUserId([FromQuery] Guid? userId)
        {
            if (HttpContext.Items["UserRole"] == "User" && userId != null)
            {
                throw new UnauthorizedAccessException("Users can only access their own notifications.");
            }
            if (userId == null || userId == Guid.Empty)
            {
                userId = (Guid)HttpContext.Items["UserId"]!;
            }

            var userNotifications = await _userNotificationService.GetByUserIdAsync(userId.Value);
            return Ok(userNotifications);
        }

        [HttpGet("user/unread")]
        [SubscriptionAndRoleAuthorize]
        public async Task<IActionResult> GetUnreadByUserId([FromQuery] Guid? userId)
        {
            if(HttpContext.Items["UserRole"] == "User" && userId != null)
            {
                throw new UnauthorizedAccessException("Users can only access their own notifications.");
            }
            if (userId == null || userId == Guid.Empty)
            {
                userId = (Guid)HttpContext.Items["UserId"]!;
                
            }
            var userNotifications = await _userNotificationService.GetUnreadByUserIdAsync(userId.Value);
            return Ok(userNotifications);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] UserNotificationRequestDto requestDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userNotification = await _userNotificationService.CreateAsync(requestDto);
            return CreatedAtAction(nameof(GetById), new { id = userNotification.Id }, userNotification);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UserNotificationRequestDto requestDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var userNotification = await _userNotificationService.UpdateAsync(id, requestDto);
                return Ok(userNotification);
            }
            catch (KeyNotFoundException)
            {
                return NotFound("User notification not found");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _userNotificationService.DeleteAsync(id);
            if (!result)
            {
                return NotFound("User notification not found");
            }

            return NoContent();
        }

        [HttpPatch("{id}/mark-read")]
        public async Task<IActionResult> MarkAsRead(Guid id)
        {
            var result = await _userNotificationService.MarkAsReadAsync(id);
            if (!result)
            {
                return NotFound("User notification not found");
            }

            return NoContent();
        }

        [HttpPatch("user/mark-all-read")]
        [SubscriptionAndRoleAuthorize]
        public async Task<IActionResult> MarkAllAsRead([FromQuery] Guid? userId)
        {
            if (HttpContext.Items["UserRole"] == "User" && userId != null)
            {
                throw new UnauthorizedAccessException("Users can only read their own notifications.");
            }
            if (userId == null || userId == Guid.Empty)
            {
                userId = (Guid)HttpContext.Items["UserId"]!;
            }

            var result = await _userNotificationService.MarkAllAsReadByUserIdAsync(userId.Value);
            if (!result)
            {
                return NotFound("No unread notifications found for user");
            }

            return NoContent();
        }
    }
}
