using BusinessLogic.DTOs.NotificationDtos;
using BusinessLogic.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizUpLearn.API.Attributes;

namespace QuizUpLearn.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpGet]
        [SubscriptionAndRoleAuthorize("Administrator", "Moderator")]
        public async Task<IActionResult> GetAll()
        {
            var notifications = await _notificationService.GetAllAsync();
            return Ok(notifications);
        }

        [HttpGet("{id}")]
        [SubscriptionAndRoleAuthorize("Administrator", "Moderator")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var notification = await _notificationService.GetByIdAsync(id);
            if (notification == null)
            {
                return NotFound("Notification not found");
            }
            return Ok(notification);
        }

        [HttpPost]
        [SubscriptionAndRoleAuthorize("Administrator", "Moderator")]
        public async Task<IActionResult> Create([FromBody] NotificationRequestDto requestDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var notification = await _notificationService.CreateAsync(requestDto);
            return CreatedAtAction(nameof(GetById), new { id = notification.Id }, notification);
        }

        [HttpPut("{id}")]
        [SubscriptionAndRoleAuthorize("Administrator", "Moderator")]
        public async Task<IActionResult> Update(Guid id, [FromBody] NotificationRequestDto requestDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var notification = await _notificationService.UpdateAsync(id, requestDto);
                return Ok(notification);
            }
            catch (KeyNotFoundException)
            {
                return NotFound("Notification not found");
            }
        }

        [HttpDelete("{id}")]
        [SubscriptionAndRoleAuthorize("Administrator", "Moderator")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _notificationService.DeleteAsync(id);
            if (!result)
            {
                return NotFound("Notification not found");
            }

            return NoContent();
        }
    }
}
