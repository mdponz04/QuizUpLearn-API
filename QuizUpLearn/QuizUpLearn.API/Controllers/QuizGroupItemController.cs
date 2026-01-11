using BusinessLogic.DTOs;
using BusinessLogic.DTOs.QuizGroupItemDtos;
using BusinessLogic.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizUpLearn.API.Attributes;

namespace QuizUpLearn.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class QuizGroupItemController : ControllerBase
    {
        private readonly IQuizGroupItemService _quizGroupItemService;

        public QuizGroupItemController(IQuizGroupItemService quizGroupItemService)
        {
            _quizGroupItemService = quizGroupItemService;
        }

        [HttpGet]
        [SubscriptionAndRoleAuthorize("Moderator")]
        public async Task<IActionResult> GetAll([FromQuery] PaginationRequestDto pagination)
        {
            var quizGroupItems = await _quizGroupItemService.GetAllGroupItemAsync(pagination);
            return Ok(quizGroupItems);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var quizGroupItem = await _quizGroupItemService.GetGroupItemByIdAsync(id);
            if (quizGroupItem == null)
            {
                return NotFound();
            }
            return Ok(quizGroupItem);
        }

        [HttpPost]
        [SubscriptionAndRoleAuthorize("Moderator")]
        public async Task<IActionResult> Create([FromBody] RequestQuizGroupItemDto requestDto)
        {
            var item = await _quizGroupItemService.CreateGroupItemAsync(requestDto);
            if (item == null) {
                return StatusCode(500, "Creation failed");
            }
            return Ok(item);
        }

        [HttpPut("{id:guid}")]
        [SubscriptionAndRoleAuthorize("Moderator")]
        public async Task<IActionResult> Update(Guid id, [FromBody] RequestQuizGroupItemDto requestDto)
        {
            var item = await _quizGroupItemService.UpdateGroupItemAsync(id, requestDto);
            if (item == null)
            {
                return NotFound();
            }
            return Ok(item);
        }

        [HttpDelete("{id:guid}")]
        [SubscriptionAndRoleAuthorize("Moderator")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var existingQuizGroupItem = await _quizGroupItemService.GetGroupItemByIdAsync(id);
            if (existingQuizGroupItem == null)
            {
                return NotFound();
            }

            if(await _quizGroupItemService.DeleteGroupItemAsync(id))
                return Ok();
            return StatusCode(500, "Delete failed");
        }
    }
}
