using BusinessLogic.DTOs;
using BusinessLogic.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizUpLearn.API.Models;

namespace QuizUpLearn.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class BuySubscriptionController : ControllerBase
    {
        private readonly IBuySubscriptionService _buySubscriptionService;
        private readonly ILogger<BuySubscriptionController> _logger;

        public BuySubscriptionController(
            IBuySubscriptionService buySubscriptionService,
            ILogger<BuySubscriptionController> logger)
        {
            _buySubscriptionService = buySubscriptionService;
            _logger = logger;
        }

        [HttpPost("purchase")]
        public async Task<IActionResult> StartBuyingSubscription([FromBody] BuySubscriptionRequestDtos dto)
        {
            try
            {
                var result = await _buySubscriptionService.StartSubscriptionPurchaseAsync(dto);
                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Data = new { orderCode = result.Item1, QrCodeUrl = result.Item2 },
                    Message = "Subscription purchase initiated successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting subscription purchase for plan {PlanId}", dto.planId);
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Failed to start subscription purchase"
                });
            }
        }

        [HttpPost("payment-success")]
        public async Task<IActionResult> PaymentSuccess([FromQuery] long orderCode)
        {
            try
            {
                await _buySubscriptionService.HandlePaymentSuccessAsync(orderCode);
                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Payment processed successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payment success for order {OrderCode}", orderCode);
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Failed to process payment"
                });
            }
        }

        [HttpPost("payment-cancel")]
        public async Task<IActionResult> PaymentCancel([FromQuery] long orderCode)
        {
            try
            {
                await _buySubscriptionService.HandlePaymentCancelAsync(orderCode);
                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Payment cancellation processed"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payment cancellation for order {OrderCode}", orderCode);
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Failed to process payment cancellation"
                });
            }
        }
    }
}
