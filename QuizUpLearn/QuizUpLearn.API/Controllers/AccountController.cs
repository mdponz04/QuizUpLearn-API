using BusinessLogic.DTOs;
using BusinessLogic.Interfaces;
using Microsoft.AspNetCore.Mvc;
using QuizUpLearn.API.Attributes;

namespace QuizUpLearn.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly IAccountService _service;

        public AccountController(IAccountService service)
        {
            _service = service;
        }

        [HttpGet]
        [SubscriptionAndRoleAuthorize("Administrator")]
        public async Task<IActionResult> GetAll([FromQuery] bool isDeleted = false)
        {
            var accounts = await _service.GetAllAsync(isDeleted);
            return Ok(accounts);
        }

        [HttpPost("search")]
        [SubscriptionAndRoleAuthorize("Administrator")]
        public async Task<ActionResult<PaginationResponseDto<ResponseAccountDto>>> GetAll(
            [FromBody] PaginationRequestDto pagination, 
            [FromQuery] bool includeDeleted = false)
        {
            var result = await _service.GetAllAsync(pagination, includeDeleted);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById([FromRoute] Guid id)
        {
            var account = await _service.GetByIdAsync(id);
            if (account == null) return NotFound();
            return Ok(account);
        }

        [HttpPost]
        [SubscriptionAndRoleAuthorize("Administrator")]
        public async Task<IActionResult> Create([FromBody] RequestAccountDto dto)
        {
            var created = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id}")]
        [SubscriptionAndRoleAuthorize("Administrator")]
        public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] RequestAccountDto dto)
        {
            var updated = await _service.UpdateAsync(id, dto);
            if (updated == null) return NotFound();
            return Ok(updated);
        }

        [HttpDelete("{id}")]
        [SubscriptionAndRoleAuthorize("Administrator")]
        public async Task<IActionResult> SoftDelete([FromRoute] Guid id)
        {
            var ok = await _service.SoftDeleteAsync(id);
            if (!ok) return NotFound();
            return NoContent();
        }

        [HttpPost("{id}/restore")]
        [SubscriptionAndRoleAuthorize("Administrator")]
        public async Task<IActionResult> Restore([FromRoute] Guid id)
        {
            var ok = await _service.RestoreAsync(id);
            if (!ok) return NotFound();
            return Ok();
        }
    }
}


