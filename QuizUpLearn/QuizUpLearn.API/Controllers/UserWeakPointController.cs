using BusinessLogic.DTOs;
using BusinessLogic.DTOs.UserWeakPointDtos;
using BusinessLogic.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizUpLearn.API.Attributes;

namespace QuizUpLearn.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UserWeakPointController : ControllerBase
    {
        private readonly IUserWeakPointService _service;

        public UserWeakPointController(IUserWeakPointService service)
        {
            _service = service;
        }

        [HttpGet("user")]
        [SubscriptionAndRoleAuthorize]
        public async Task<ActionResult<PaginationResponseDto<ResponseUserWeakPointDto>>> GetByUserId(
            [FromQuery] PaginationRequestDto pagination)
        {
            var userId = (Guid)HttpContext.Items["UserId"]!;

            var result = await _service.GetByUserIdAsync(userId, pagination);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ResponseUserWeakPointDto>> GetById(Guid id)
        {
            var result = await _service.GetByIdAsync(id);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult<ResponseUserWeakPointDto>> Add(RequestUserWeakPointDto dto)
        {
            var result = await _service.AddAsync(dto);
            if (result == null) return BadRequest();
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ResponseUserWeakPointDto>> Update(Guid id, RequestUserWeakPointDto dto)
        {
            var result = await _service.UpdateAsync(id, dto);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpDelete]
        public async Task<ActionResult> Delete(Guid id)
        {
            var success = await _service.DeleteAsync(id);
            if (!success) return NotFound();
            return NoContent();
        }
    }
}
