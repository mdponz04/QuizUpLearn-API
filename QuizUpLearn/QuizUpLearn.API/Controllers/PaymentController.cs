using BusinessLogic.DTOs.PaymentDtos;
using BusinessLogic.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizUpLearn.API.Attributes;

namespace QuizUpLearn.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly IBuySubscriptionService _buySubscriptionService;

        public PaymentController(IPaymentService paymentService, IBuySubscriptionService buySubscriptionService)
        {
            _paymentService = paymentService;
            _buySubscriptionService = buySubscriptionService;
        }
        [HttpPost("create-payment")]
        [SubscriptionAndRoleAuthorize("Moderator")]
        public async Task<IActionResult> CreatePayment(int amount, string description)
        {
            var paymentResult = await _paymentService.CreatePaymentLinkAsync(amount, description, null!);
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
        [HttpPost("os/webhook")]
        [AllowAnonymous]
        public async Task<IActionResult> Webhook([FromBody] PayosWebhookDto payload)
        {
            if (!payload.Success || payload.Code != "00")
            {
                await _buySubscriptionService.HandlePaymentCancelAsync(payload.Data.OrderCode);
                return Ok();
            }

            await _buySubscriptionService.HandlePaymentSuccessAsync(payload.Data.OrderCode);
            return Ok();
        }
    }
}
