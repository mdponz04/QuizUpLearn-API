using BusinessLogic.DTOs;
using BusinessLogic.DTOs.SubscriptionPlanDtos;
using BusinessLogic.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using QuizUpLearn.API.Controllers;
using QuizUpLearn.API.Models;

namespace QuizUpLearn.Test.IntegrationTest
{
    public class BuySubscriptionControllerTest : BaseControllerTest
    {
        private readonly Mock<IBuySubscriptionService> _buySubscriptionServiceMock;
        private readonly Mock<ISubscriptionPlanService> _subscriptionPlanServiceMock;
        private readonly Mock<IPaymentService> _paymentServiceMock;
        private readonly Mock<IPaymentTransactionService> _paymentTransactionServiceMock;
        private readonly Mock<ILogger<BuySubscriptionController>> _loggerMock;

        public BuySubscriptionControllerTest()
        {
            _buySubscriptionServiceMock = new Mock<IBuySubscriptionService>();
            _subscriptionPlanServiceMock = new Mock<ISubscriptionPlanService>();
            _paymentServiceMock = new Mock<IPaymentService>();
            _paymentTransactionServiceMock = new Mock<IPaymentTransactionService>();
            _loggerMock = new Mock<ILogger<BuySubscriptionController>>();
        }

        [Fact]
        public async Task StartBuyingSubscription_ReturnsOk_WhenPlanIsValidAndBuyable()
        {
            // Arrange
            var planId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var dto = new BuySubscriptionDto
            {
                PlanId = planId,
                SuccessUrl = "https://success.url",
                CanceledUrl = "https://cancel.url"
            };

            _subscriptionPlanServiceMock
                .Setup(x => x.GetByIdAsync(planId))
                .ReturnsAsync(new ResponseSubscriptionPlanDto { Id = planId, IsBuyable = true, IsActive = true });

            _buySubscriptionServiceMock
                .Setup(x => x.StartSubscriptionPurchaseAsync(userId, planId, dto.SuccessUrl, dto.CanceledUrl))
                .ReturnsAsync((123L, "https://qr.url"));

            var controller = new BuySubscriptionController(
                _buySubscriptionServiceMock.Object,
                _loggerMock.Object,
                _subscriptionPlanServiceMock.Object,
                _paymentServiceMock.Object
            );

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            controller.HttpContext.Items["UserId"] = userId;

            // Act
            var result = await controller.StartBuyingSubscription(dto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<object>>(okResult.Value);
            Assert.True(response.Success);
            Assert.Equal("Subscription purchase initiated successfully", response.Message);
        }

        [Fact]
        public async Task StartBuyingSubscription_ReturnsBadRequest_WhenPlanIdEmpty()
        {
            var planId = Guid.Empty;
            var dto = new BuySubscriptionDto
            {
                PlanId = planId,
                SuccessUrl = "https://success.url",
                CanceledUrl = "https://cancel.url"
            };

            _subscriptionPlanServiceMock
                .Setup(x => x.GetByIdAsync(planId))
                .ReturnsAsync((ResponseSubscriptionPlanDto?)null);

            var controller = new BuySubscriptionController(
                _buySubscriptionServiceMock.Object,
                _loggerMock.Object,
                _subscriptionPlanServiceMock.Object,
                _paymentServiceMock.Object
            );

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            controller.HttpContext.Items["UserId"] = Guid.NewGuid();

            // Act
            var result = await controller.StartBuyingSubscription(dto);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<object>>(badRequest.Value);
            Assert.False(response.Success);
            Assert.Equal("Invalid subscription plan ID", response.Message);
        }

        [Fact]
        public async Task StartBuyingSubscription_ReturnsBadRequest_WhenPlanIsNotActive()
        {
            var planId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var dto = new BuySubscriptionDto
            {
                PlanId = planId,
                SuccessUrl = "https://success.url",
                CanceledUrl = "https://cancel.url"
            };

            _subscriptionPlanServiceMock
                .Setup(x => x.GetByIdAsync(planId))
                .ReturnsAsync(new ResponseSubscriptionPlanDto { Id = planId, IsActive = false, IsBuyable = true });

            var controller = new BuySubscriptionController(
                _buySubscriptionServiceMock.Object,
                _loggerMock.Object,
                _subscriptionPlanServiceMock.Object,
                _paymentServiceMock.Object
            );

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            controller.HttpContext.Items["UserId"] = userId;

            var result = await controller.StartBuyingSubscription(dto);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<object>>(badRequest.Value);
            Assert.False(response.Success);
            Assert.Equal("This subscription plan is not available for purchase", response.Message);
            
            _buySubscriptionServiceMock.Verify(x => x.StartSubscriptionPurchaseAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task StartBuyingSubscription_ReturnsBadRequest_WhenPlanIsNotBuyable()
        {
            var planId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var dto = new BuySubscriptionDto
            {
                PlanId = planId,
                SuccessUrl = "https://success.url",
                CanceledUrl = "https://cancel.url"
            };

            _subscriptionPlanServiceMock
                .Setup(x => x.GetByIdAsync(planId))
                .ReturnsAsync(new ResponseSubscriptionPlanDto { Id = planId, IsActive = true, IsBuyable = false });

            var controller = new BuySubscriptionController(
                _buySubscriptionServiceMock.Object,
                _loggerMock.Object,
                _subscriptionPlanServiceMock.Object,
                _paymentServiceMock.Object
            );

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            controller.HttpContext.Items["UserId"] = userId;

            var result = await controller.StartBuyingSubscription(dto);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<object>>(badRequest.Value);
            Assert.False(response.Success);
            Assert.Equal("This subscription plan is not available for purchase", response.Message);
            
            _buySubscriptionServiceMock.Verify(x => x.StartSubscriptionPurchaseAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }
    }
}
