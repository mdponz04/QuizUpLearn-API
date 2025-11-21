using Microsoft.AspNetCore.Mvc;
using BusinessLogic.Interfaces;
using BusinessLogic.DTOs.PaymentTransactionDtos;
using BusinessLogic.DTOs.PaymentTransactionDtos;
using BusinessLogic.DTOs;

namespace QuizUpLearn.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentTransactionController : ControllerBase
    {
        private readonly IPaymentTransactionService _service;

        public PaymentTransactionController(IPaymentTransactionService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<PaginationResponseDto<ResponsePaymentTransactionDto>>> GetPaged([FromQuery] PaginationRequestDto pagination)
        {
            var result = await _service.GetAllAsync(pagination);
            return Ok(result);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<ResponsePaymentTransactionDto>> GetById(Guid id)
        {
            var result = await _service.GetByIdAsync(id);
            if (result == null) return NotFound();
            return Ok(result);
        }
        [HttpGet("by-ordercode/{orderCode:long}")]
        public async Task<ActionResult<ResponsePaymentTransactionDto>> GetByOrderCode(long orderCode)
        {
            var result = await _service.GetByPaymentGatewayTransactionOrderCodeAsync(orderCode.ToString());
            if (result == null) return NotFound();
            return Ok(result);
        }
        [HttpPost]
        public async Task<ActionResult<ResponsePaymentTransactionDto>> Create([FromBody] RequestPaymentTransactionDto dto)
        {
            var created = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id:guid}")]
        public async Task<ActionResult<ResponsePaymentTransactionDto>> Update(Guid id, [FromBody] RequestPaymentTransactionDto dto)
        {
            var updated = await _service.UpdateAsync(id, dto);
            if (updated == null) return NotFound();
            return Ok(updated);
        }

        [HttpDelete("{id:guid}")]
        public async Task<ActionResult> Delete(Guid id)
        {
            var deleted = await _service.DeleteAsync(id);
            if (!deleted) return NotFound();
            return NoContent();
        }
    }
}
