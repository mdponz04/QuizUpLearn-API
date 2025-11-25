using Microsoft.AspNetCore.Mvc;
using BusinessLogic.DTOs.QuizGroupItemDtos;
using BusinessLogic.Interfaces;
using BusinessLogic.DTOs;
using Microsoft.AspNetCore.Authorization;

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
        public async Task<IActionResult> GetAll([FromQuery] PaginationRequestDto pagination)
        {
            var quizGroupItems = await _quizGroupItemService.GetAllAsync(pagination);
            return Ok(quizGroupItems);
        }

        [HttpGet("quizset/{quizSetId:guid}")]
        public async Task<IActionResult> GetAllByQuizSetId(Guid quizSetId, [FromQuery] PaginationRequestDto pagination)
        {
            var quizGroupItems = await _quizGroupItemService.GetAllByQuizSetIdAsync(quizSetId, pagination);
            return Ok(quizGroupItems);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var quizGroupItem = await _quizGroupItemService.GetByIdAsync(id);
            if (quizGroupItem == null)
            {
                return NotFound();
            }
            return Ok(quizGroupItem);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] RequestQuizGroupItemDto requestDto)
        {
            var item = await _quizGroupItemService.CreateAsync(requestDto);
            if (item == null) {
                return StatusCode(500, "Creation failed");
            }
            return Ok(item);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] RequestQuizGroupItemDto requestDto)
        {
            var item = await _quizGroupItemService.UpdateAsync(id, requestDto);
            if (item == null)
            {
                return NotFound();
            }
            return Ok(item);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var existingQuizGroupItem = await _quizGroupItemService.GetByIdAsync(id);
            if (existingQuizGroupItem == null)
            {
                return NotFound();
            }

            if(await _quizGroupItemService.DeleteAsync(id))
                return Ok();
            return StatusCode(500, "Delete failed");
        }
    }
}
