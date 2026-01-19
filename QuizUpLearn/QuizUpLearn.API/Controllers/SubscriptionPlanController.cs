using BusinessLogic.DTOs;
using BusinessLogic.DTOs.SubscriptionPlanDtos;
using BusinessLogic.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizUpLearn.API.Attributes;

namespace QuizUpLearn.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SubscriptionPlanController : ControllerBase
    {
        private readonly ISubscriptionPlanService _service;
        private readonly ISubscriptionService _subscriptionService;

        public SubscriptionPlanController(ISubscriptionPlanService service, ISubscriptionService subscriptionService)
        {
            _service = service;
            _subscriptionService = subscriptionService;
        }
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<PaginationResponseDto<ResponseSubscriptionPlanDto>>> GetPaged([FromQuery] PaginationRequestDto pagination)
        {
            var result = await _service.GetAllAsync(pagination);
            return Ok(result);
        }

        [HttpGet("{id:guid}")]
        [AllowAnonymous]
        public async Task<ActionResult<ResponseSubscriptionPlanDto>> GetById(Guid id)
        {
            var result = await _service.GetByIdAsync(id);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpPost]
        [Authorize]
        [SubscriptionAndRoleAuthorize("Administrator")]
        public async Task<ActionResult<ResponseSubscriptionPlanDto>> Create([FromBody] RequestSubscriptionPlanDto dto)
        {
            var created = await _service.CreateAsync(dto);
            return StatusCode(201, created);
        }

        [HttpPut("{id:guid}")]
        [Authorize]
        [SubscriptionAndRoleAuthorize("Administrator")]
        public async Task<ActionResult<ResponseSubscriptionPlanDto>> Update(Guid id, [FromBody] RequestSubscriptionPlanDto dto)
        {
            var subscriptions = await _subscriptionService.GetByPlanIdAsync(id);
            if (subscriptions.Any(s => s.DeletedAt == null))
            {
                return BadRequest("Cannot update subscription plan with active subscriptions.");
            }
            var updated = await _service.UpdateAsync(id, dto);
            if (updated == null) return NotFound();
            return Ok(updated);
        }

        [HttpDelete("{id:guid}")]
        [Authorize]
        [SubscriptionAndRoleAuthorize("Administrator")]
        public async Task<ActionResult> Delete(Guid id)
        {
            var subscriptions = await _subscriptionService.GetByPlanIdAsync(id);
            if (subscriptions.Any(s => s.DeletedAt == null))
            {
                return BadRequest("Cannot update subscription plan with active subscriptions.");
            }
            var deleted = await _service.DeleteAsync(id);
            if (!deleted) return NotFound();
            return NoContent();
        }
        [HttpGet("free-plan")]
        [Authorize]
        public async Task<ActionResult<ResponseSubscriptionPlanDto>> GetFreePlan()
        {
            var freePlan = await _service.GetFreeSubscriptionPlanAsync();
            return Ok(freePlan);
        }
        [HttpPut("update-buyable/{id:guid}")]
        [Authorize]
        [SubscriptionAndRoleAuthorize("Administrator")]
        public async Task<ActionResult> UpdateBuyablePlan(Guid id)
        {
            var result = await _service.ChangeIsBuyableAsync(id);
            if (!result)
            {
                return NotFound($"Plan with id: {id} is not found.");
            }
            return Ok("Change IsBuyable success");
        }
    }
}
