using BusinessLogic.Interfaces;
using Microsoft.AspNetCore.Mvc;
using QuizUpLearn.API.Attributes;

namespace QuizUpLearn.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public PaymentController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }
        [HttpPost("create-payment")]
        [SubscriptionAndRoleAuthorize("Moderator")]
        public async Task<IActionResult> CreatePayment(int amount, string description, string successUrl, string cancelUrl)
        {
            var paymentResult = await _paymentService.CreatePaymentLinkAsync(amount, description, null!, successUrl, cancelUrl);
            if (paymentResult != null)
            {
                return Ok(paymentResult);
            }
            return StatusCode(500, "Payment request create failed");
        }
        [HttpPost("cancel-payment")]
        [SubscriptionAndRoleAuthorize("Moderator")]
        public async Task<IActionResult> CancelPayment(long orderCode, string? reason)
        {
            var cancelResult = await _paymentService.CancelPaymentLinkAsync(orderCode, reason);
            if (cancelResult != null)
            {
                return Ok(cancelResult);
            }
            return StatusCode(500, "Payment request cancel failed");
        }
        [HttpGet("get-payment/{orderCode}")]
        [SubscriptionAndRoleAuthorize("Moderator")]
        public async Task<IActionResult> GetPayment(long orderCode)
        {
            var paymentInfo = await _paymentService.GetPaymentInfoAsync(orderCode);
            if (paymentInfo != null)
            {
                return Ok(paymentInfo);
            }
            return NotFound("Payment request not found");
        }
    }
}
