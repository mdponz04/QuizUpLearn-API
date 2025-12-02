using Microsoft.AspNetCore.Mvc;
using BusinessLogic.DTOs.UserMistakeDtos;
using BusinessLogic.Interfaces;
using BusinessLogic.DTOs;
using Microsoft.AspNetCore.Authorization;
using QuizUpLearn.API.Attributes;

namespace QuizUpLearn.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UserMistakeController : ControllerBase
    {
        private readonly IUserMistakeService _userMistakeService;

        public UserMistakeController(IUserMistakeService userMistakeService)
        {
            _userMistakeService = userMistakeService;
        }
        [HttpGet]
        [SubscriptionAndRoleAuthorize("Administrator", "Mod")]
        public async Task<IActionResult> GetAll([FromQuery] PaginationRequestDto pagination)
        {
            var userMistakes = await _userMistakeService.GetAllAsync(pagination);
            return Ok(userMistakes);
        }
        [HttpGet("user/{userId:guid}")]
        [SubscriptionAndRoleAuthorize]
        public async Task<IActionResult> GetAllByUserId([FromQuery] PaginationRequestDto pagination)
        {
            var userId = (Guid)HttpContext.Items["UserId"]!;

            var userMistakes = await _userMistakeService.GetAllByUserIdAsync(userId, pagination);
            return Ok(userMistakes);
        }
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var userMistake = await _userMistakeService.GetByIdAsync(id);
            if (userMistake == null)
            {
                return NotFound();
            }
            return Ok(userMistake);
        }
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] RequestUserMistakeDto requestDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            await _userMistakeService.AddAsync(requestDto);
            return CreatedAtAction(nameof(GetById), new { id = requestDto.UserId }, requestDto);
        }
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] RequestUserMistakeDto requestDto)
        {
            var existingUserMistake = await _userMistakeService.GetByIdAsync(id);
            if (existingUserMistake == null)
            {
                return NotFound();
            }

            await _userMistakeService.UpdateAsync(id, requestDto);
            return NoContent();
        }
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var existingUserMistake = await _userMistakeService.GetByIdAsync(id);
            if (existingUserMistake == null)
            {
                return NotFound();
            }

            await _userMistakeService.DeleteAsync(id);
            return NoContent();
        }
        [HttpGet("mistake-quizzes/user")]
        [SubscriptionAndRoleAuthorize]
        public async Task<IActionResult> GetMistakeQuizzes(PaginationRequestDto paginationDto)
        {
            var userId = (Guid)HttpContext.Items["UserId"]!;

            var mistakeQuizzes = await _userMistakeService.GetMistakeQuizzesByUserId(userId, paginationDto);
            return Ok(mistakeQuizzes);
        }
    }
}
