using BusinessLogic.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizUpLearn.API.Attributes;
using QuizUpLearn.API.Models;

namespace QuizUpLearn.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class BuySubscriptionController : ControllerBase
    {
        private readonly IBuySubscriptionService _buySubscriptionService;
        private readonly ISubscriptionPlanService _subscriptionPlanService;
        private readonly IPaymentService _paymentService;
        private readonly ILogger<BuySubscriptionController> _logger;

        public BuySubscriptionController(
            IBuySubscriptionService buySubscriptionService,
            ILogger<BuySubscriptionController> logger,
            ISubscriptionPlanService subscriptionPlanService,
            IPaymentService paymentService)
        {
            _buySubscriptionService = buySubscriptionService;
            _logger = logger;
            _subscriptionPlanService = subscriptionPlanService;
            _paymentService = paymentService;
        }

        [HttpPost("purchase")]
        [SubscriptionAndRoleAuthorize]
        public async Task<IActionResult> StartBuyingSubscription([FromQuery] Guid planId)
        {
            var plan = await _subscriptionPlanService.GetByIdAsync(planId);
            if(plan == null || planId == Guid.Empty)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Invalid subscription plan ID"
                });
            }
            if(plan.IsBuyable == false)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "This subscription plan is not available for purchase"
                });
            }
            var userId = (Guid)HttpContext.Items["UserId"]!;

            try
            {
                var result = await _buySubscriptionService.StartSubscriptionPurchaseAsync(userId, planId);
                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Data = new { orderCode = result.Item1, QrCodeUrl = result.Item2 },
                    Message = "Subscription purchase initiated successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting subscription purchase for plan {PlanId}", planId);
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Failed to start subscription purchase"
                });
            }
        }

        [HttpPost("payment-success")]
        [SubscriptionAndRoleAuthorize]
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
        [SubscriptionAndRoleAuthorize]
        public async Task<IActionResult> PaymentCancel([FromQuery] long orderCode)
        {
            try
            {
                await _buySubscriptionService.HandlePaymentCancelAsync(orderCode);
                await _paymentService.CancelPaymentLinkAsync(orderCode, "User cancelled the payment");
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
