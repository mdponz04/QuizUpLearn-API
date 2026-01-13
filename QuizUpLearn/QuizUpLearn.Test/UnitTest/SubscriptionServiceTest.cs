using AutoMapper;
using BusinessLogic.DTOs;
using BusinessLogic.DTOs.SubscriptionDtos;
using BusinessLogic.DTOs.SubscriptionPlanDtos;
using BusinessLogic.Interfaces;
using BusinessLogic.MappingProfile;
using BusinessLogic.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Repository.Entities;
using Repository.Interfaces;

namespace QuizUpLearn.Test.UnitTest
{
    public class SubscriptionServiceTest : BaseControllerTest
    {
        private readonly Mock<ISubscriptionRepo> _mockSubscriptionRepo;
        private readonly Mock<ISubscriptionPlanService> _mockSubscriptionPlanService;
        private readonly IMapper _mapper;
        private readonly SubscriptionService _subscriptionService;

        public SubscriptionServiceTest()
        {
            _mockSubscriptionRepo = new Mock<ISubscriptionRepo>();
            _mockSubscriptionPlanService = new Mock<ISubscriptionPlanService>();

            // Setup real AutoMapper with the actual mapping profile
            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<MappingProfile>();
            }, new NullLoggerFactory());
            _mapper = mapperConfig.CreateMapper();

            _subscriptionService = new SubscriptionService(
                _mockSubscriptionRepo.Object,
                _mapper,
                _mockSubscriptionPlanService.Object);
        }

        [Fact]
        public async Task GetAllAsync_WithValidPagination_ShouldReturnPagedResponse()
        {
            // Arrange
            var pagination = new PaginationRequestDto
            {
                Page = 1,
                PageSize = 10
            };

            var subscriptions = new List<Subscription>
            {
                new Subscription
                {
                    Id = Guid.NewGuid(),
                    UserId = Guid.NewGuid(),
                    SubscriptionPlanId = Guid.NewGuid(),
                    EndDate = DateTime.UtcNow.AddDays(30),
                    CreatedAt = DateTime.UtcNow
                },
                new Subscription
                {
                    Id = Guid.NewGuid(),
                    UserId = Guid.NewGuid(),
                    SubscriptionPlanId = Guid.NewGuid(),
                    EndDate = DateTime.UtcNow.AddDays(60),
                    CreatedAt = DateTime.UtcNow
                }
            };

            _mockSubscriptionRepo.Setup(r => r.GetAllAsync())
                .ReturnsAsync(subscriptions);

            // Act
            var result = await _subscriptionService.GetAllAsync(pagination);

            // Assert
            result.Should().NotBeNull();
            result.Data.Should().NotBeNull();
            result.Data.Should().HaveCount(2);
            result.Pagination.Should().NotBeNull();
            result.Pagination.CurrentPage.Should().Be(1);
            result.Pagination.PageSize.Should().Be(10);

            // Verify the mapped data
            result.Data.Should().AllSatisfy(dto =>
            {
                dto.Id.Should().NotBeEmpty();
                dto.UserId.Should().NotBeEmpty();
                dto.SubscriptionPlanId.Should().NotBeEmpty();
                dto.EndDate.Should().NotBeNull();
                dto.CreatedAt.Should().NotBe(default);
            });

            _mockSubscriptionRepo.Verify(r => r.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task GetAllAsync_WithNullPagination_ShouldUseDefaultPagination()
        {
            // Arrange
            var subscriptions = new List<Subscription>
            {
                new Subscription
                {
                    Id = Guid.NewGuid(),
                    UserId = Guid.NewGuid(),
                    SubscriptionPlanId = Guid.NewGuid(),
                    EndDate = DateTime.UtcNow.AddDays(30),
                    CreatedAt = DateTime.UtcNow
                }
            };

            _mockSubscriptionRepo.Setup(r => r.GetAllAsync())
                .ReturnsAsync(subscriptions);

            // Act
            var result = await _subscriptionService.GetAllAsync(null);

            // Assert
            result.Should().NotBeNull();
            result.Data.Should().NotBeNull();
            result.Data.Should().HaveCount(1);
            result.Pagination.Should().NotBeNull();

            _mockSubscriptionRepo.Verify(r => r.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task GetByPlanIdAsync_WithValidPlanId_ShouldReturnSubscriptions()
        {
            // Arrange
            var planId = Guid.NewGuid();
            var subscriptions = new List<Subscription>
            {
                new Subscription
                {
                    Id = Guid.NewGuid(),
                    UserId = Guid.NewGuid(),
                    SubscriptionPlanId = planId,
                    EndDate = DateTime.UtcNow.AddDays(30),
                    CreatedAt = DateTime.UtcNow
                },
                new Subscription
                {
                    Id = Guid.NewGuid(),
                    UserId = Guid.NewGuid(),
                    SubscriptionPlanId = planId,
                    EndDate = DateTime.UtcNow.AddDays(60),
                    CreatedAt = DateTime.UtcNow
                }
            };

            _mockSubscriptionRepo.Setup(r => r.GetByPlanIdAsync(planId))
                .ReturnsAsync(subscriptions);

            // Act
            var result = await _subscriptionService.GetByPlanIdAsync(planId);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.Should().AllSatisfy(dto =>
            {
                dto.SubscriptionPlanId.Should().Be(planId);
                dto.Id.Should().NotBeEmpty();
                dto.UserId.Should().NotBeEmpty();
            });

            _mockSubscriptionRepo.Verify(r => r.GetByPlanIdAsync(planId), Times.Once);
        }

        [Fact]
        public async Task GetByUserIdAsync_WithValidUserId_ShouldReturnSubscription()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var subscription = new Subscription
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                SubscriptionPlanId = Guid.NewGuid(),
                EndDate = DateTime.UtcNow.AddDays(30),
                CreatedAt = DateTime.UtcNow
            };

            _mockSubscriptionRepo.Setup(r => r.GetByUserIdAsync(userId))
                .ReturnsAsync(subscription);

            // Act
            var result = await _subscriptionService.GetByUserIdAsync(userId);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(subscription.Id);
            result.UserId.Should().Be(userId);
            result.SubscriptionPlanId.Should().Be(subscription.SubscriptionPlanId);
            result.EndDate.Should().Be(subscription.EndDate);
            result.CreatedAt.Should().Be(subscription.CreatedAt);

            _mockSubscriptionRepo.Verify(r => r.GetByUserIdAsync(userId), Times.Once);
        }

        [Fact]
        public async Task GetByUserIdAsync_WithExpiredSubscription_ShouldUpdateToFreePlan()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var freePlanId = Guid.NewGuid();
            var expiredSubscription = new Subscription
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                SubscriptionPlanId = Guid.NewGuid(),
                EndDate = DateTime.UtcNow.AddDays(-1), // Expired
                CreatedAt = DateTime.UtcNow.AddDays(-31)
            };

            var freePlan = new ResponseSubscriptionPlanDto
            {
                Id = freePlanId,
                Name = "Free Plan",
                DurationDays = 30,
                Price = 0
            };

            var updatedSubscription = new Subscription
            {
                Id = expiredSubscription.Id,
                UserId = userId,
                SubscriptionPlanId = freePlanId,
                EndDate = DateTime.UtcNow.AddDays(30),
                CreatedAt = expiredSubscription.CreatedAt,
                UpdatedAt = DateTime.UtcNow
            };

            _mockSubscriptionRepo.Setup(r => r.GetByUserIdAsync(userId))
                .ReturnsAsync(expiredSubscription);
            _mockSubscriptionPlanService.Setup(s => s.GetFreeSubscriptionPlanAsync())
                .ReturnsAsync(freePlan);
            _mockSubscriptionRepo.Setup(r => r.UpdateAsync(expiredSubscription.Id, It.IsAny<Subscription>()))
                .ReturnsAsync(updatedSubscription);

            // Act
            var result = await _subscriptionService.GetByUserIdAsync(userId);

            // Assert
            result.Should().NotBeNull();
            result!.UserId.Should().Be(userId);
            result.SubscriptionPlanId.Should().Be(freePlanId);

            _mockSubscriptionRepo.Verify(r => r.GetByUserIdAsync(userId), Times.Once);
            _mockSubscriptionPlanService.Verify(s => s.GetFreeSubscriptionPlanAsync(), Times.Once);
            _mockSubscriptionRepo.Verify(r => r.UpdateAsync(expiredSubscription.Id, It.IsAny<Subscription>()), Times.Once);
        }

        [Fact]
        public async Task GetByUserIdAsync_WithNonExistentUser_ShouldReturnNull()
        {
            // Arrange
            var userId = Guid.NewGuid();

            _mockSubscriptionRepo.Setup(r => r.GetByUserIdAsync(userId))
                .ReturnsAsync((Subscription?)null);

            // Act
            var result = await _subscriptionService.GetByUserIdAsync(userId);

            // Assert
            result.Should().BeNull();
            _mockSubscriptionRepo.Verify(r => r.GetByUserIdAsync(userId), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_WithValidId_ShouldReturnSubscription()
        {
            // Arrange
            var subscriptionId = Guid.NewGuid();
            var subscription = new Subscription
            {
                Id = subscriptionId,
                UserId = Guid.NewGuid(),
                SubscriptionPlanId = Guid.NewGuid(),
                EndDate = DateTime.UtcNow.AddDays(30),
                CreatedAt = DateTime.UtcNow
            };

            _mockSubscriptionRepo.Setup(r => r.GetByIdAsync(subscriptionId))
                .ReturnsAsync(subscription);

            // Act
            var result = await _subscriptionService.GetByIdAsync(subscriptionId);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(subscriptionId);
            result.UserId.Should().Be(subscription.UserId);
            result.SubscriptionPlanId.Should().Be(subscription.SubscriptionPlanId);
            result.EndDate.Should().Be(subscription.EndDate);

            _mockSubscriptionRepo.Verify(r => r.GetByIdAsync(subscriptionId), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_WithNonExistentId_ShouldReturnNull()
        {
            // Arrange
            var subscriptionId = Guid.NewGuid();

            _mockSubscriptionRepo.Setup(r => r.GetByIdAsync(subscriptionId))
                .ReturnsAsync((Subscription?)null);

            // Act
            var result = await _subscriptionService.GetByIdAsync(subscriptionId);

            // Assert
            result.Should().BeNull();
            _mockSubscriptionRepo.Verify(r => r.GetByIdAsync(subscriptionId), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_WithValidDto_ShouldReturnCreatedSubscription()
        {
            // Arrange
            var requestDto = new RequestSubscriptionDto
            {
                UserId = Guid.NewGuid(),
                SubscriptionPlanId = Guid.NewGuid(),
                EndDate = DateTime.UtcNow.AddDays(30)
            };

            var createdSubscription = new Subscription
            {
                Id = Guid.NewGuid(),
                UserId = requestDto.UserId!.Value,
                SubscriptionPlanId = requestDto.SubscriptionPlanId!.Value,
                EndDate = requestDto.EndDate,
                CreatedAt = DateTime.UtcNow
            };

            _mockSubscriptionRepo.Setup(r => r.CreateAsync(It.IsAny<Subscription>()))
                .ReturnsAsync(createdSubscription);

            // Act
            var result = await _subscriptionService.CreateAsync(requestDto);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(createdSubscription.Id);
            result.UserId.Should().Be(requestDto.UserId!.Value);
            result.SubscriptionPlanId.Should().Be(requestDto.SubscriptionPlanId!.Value);
            result.EndDate.Should().Be(requestDto.EndDate);
            result.CreatedAt.Should().Be(createdSubscription.CreatedAt);

            _mockSubscriptionRepo.Verify(r => r.CreateAsync(It.IsAny<Subscription>()), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_WithValidIdAndDto_ShouldReturnUpdatedSubscription()
        {
            // Arrange
            var id = Guid.NewGuid();
            var requestDto = new RequestSubscriptionDto
            {
                UserId = Guid.NewGuid(),
                SubscriptionPlanId = Guid.NewGuid(),
                EndDate = DateTime.UtcNow.AddDays(60)
            };

            var updatedSubscription = new Subscription
            {
                Id = id,
                UserId = requestDto.UserId!.Value,
                SubscriptionPlanId = requestDto.SubscriptionPlanId!.Value,
                EndDate = requestDto.EndDate,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow
            };

            _mockSubscriptionRepo.Setup(r => r.UpdateAsync(id, It.IsAny<Subscription>()))
                .ReturnsAsync(updatedSubscription);

            // Act
            var result = await _subscriptionService.UpdateAsync(id, requestDto);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(id);
            result.UserId.Should().Be(requestDto.UserId!.Value);
            result.SubscriptionPlanId.Should().Be(requestDto.SubscriptionPlanId!.Value);
            result.EndDate.Should().Be(requestDto.EndDate);
            result.UpdatedAt.Should().Be(updatedSubscription.UpdatedAt);

            _mockSubscriptionRepo.Verify(r => r.UpdateAsync(id, It.IsAny<Subscription>()), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_WithNonExistentId_ShouldReturnNull()
        {
            // Arrange
            var id = Guid.NewGuid();
            var requestDto = new RequestSubscriptionDto
            {
                UserId = Guid.NewGuid(),
                SubscriptionPlanId = Guid.NewGuid(),
                EndDate = DateTime.UtcNow.AddDays(60)
            };

            _mockSubscriptionRepo.Setup(r => r.UpdateAsync(id, It.IsAny<Subscription>()))
                .ReturnsAsync((Subscription?)null);

            // Act
            var result = await _subscriptionService.UpdateAsync(id, requestDto);

            // Assert
            result.Should().BeNull();
            _mockSubscriptionRepo.Verify(r => r.UpdateAsync(id, It.IsAny<Subscription>()), Times.Once);
        }

        [Fact]
        public async Task CalculateRemainingUsageByUserId_WithValidUserId_ShouldReturnUpdatedSubscription()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var usedQuantity = 5;
            var subscriptionId = Guid.NewGuid();

            var existingSubscription = new Subscription
            {
                Id = subscriptionId,
                UserId = userId,
                SubscriptionPlanId = Guid.NewGuid(),
                EndDate = DateTime.UtcNow.AddDays(30),
                CreatedAt = DateTime.UtcNow
            };

            var updatedSubscription = new Subscription
            {
                Id = subscriptionId,
                UserId = userId,
                SubscriptionPlanId = existingSubscription.SubscriptionPlanId,
                EndDate = existingSubscription.EndDate,
                CreatedAt = existingSubscription.CreatedAt,
                UpdatedAt = DateTime.UtcNow
            };

            _mockSubscriptionRepo.Setup(r => r.GetByUserIdAsync(userId))
                .ReturnsAsync(existingSubscription);
            _mockSubscriptionRepo.Setup(r => r.CalculateRemainingUsageByUserId(userId, usedQuantity))
                .ReturnsAsync(updatedSubscription);

            // Act
            var result = await _subscriptionService.CalculateRemainingUsageByUserId(userId, usedQuantity);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(subscriptionId);
            result.UserId.Should().Be(userId);
            result.UpdatedAt.Should().Be(updatedSubscription.UpdatedAt);

            _mockSubscriptionRepo.Verify(r => r.CalculateRemainingUsageByUserId(userId, usedQuantity), Times.Once);
        }

        [Fact]
        public async Task CalculateRemainingUsageByUserId_WithNonExistentUser_ShouldReturnNull()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var usedQuantity = 5;

            _mockSubscriptionRepo.Setup(r => r.CalculateRemainingUsageByUserId(userId, usedQuantity))
                .ReturnsAsync((Subscription?)null);

            // Act
            var result = await _subscriptionService.CalculateRemainingUsageByUserId(userId, usedQuantity);

            // Assert
            result.Should().BeNull();
            _mockSubscriptionRepo.Verify(r => r.CalculateRemainingUsageByUserId(userId, usedQuantity), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_WithValidId_ShouldReturnTrue()
        {
            // Arrange
            var subscriptionId = Guid.NewGuid();

            _mockSubscriptionRepo.Setup(r => r.DeleteAsync(subscriptionId))
                .ReturnsAsync(true);

            // Act
            var result = await _subscriptionService.DeleteAsync(subscriptionId);

            // Assert
            result.Should().BeTrue();
            _mockSubscriptionRepo.Verify(r => r.DeleteAsync(subscriptionId), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_WithNonExistentId_ShouldReturnFalse()
        {
            // Arrange
            var subscriptionId = Guid.NewGuid();

            _mockSubscriptionRepo.Setup(r => r.DeleteAsync(subscriptionId))
                .ReturnsAsync(false);

            // Act
            var result = await _subscriptionService.DeleteAsync(subscriptionId);

            // Assert
            result.Should().BeFalse();
            _mockSubscriptionRepo.Verify(r => r.DeleteAsync(subscriptionId), Times.Once);
        }

        [Fact]
        public async Task GetAllAsync_WithEmptyRepository_ShouldReturnEmptyPagedResponse()
        {
            // Arrange
            var pagination = new PaginationRequestDto
            {
                Page = 1,
                PageSize = 10
            };

            _mockSubscriptionRepo.Setup(r => r.GetAllAsync())
                .ReturnsAsync(new List<Subscription>());

            // Act
            var result = await _subscriptionService.GetAllAsync(pagination);

            // Assert
            result.Should().NotBeNull();
            result.Data.Should().NotBeNull();
            result.Data.Should().BeEmpty();
            result.Pagination.TotalCount.Should().Be(0);

            _mockSubscriptionRepo.Verify(r => r.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task GetByPlanIdAsync_WithNonExistentId_ShouldReturnEmptyCollection()
        {
            // Arrange
            var planId = Guid.NewGuid();

            _mockSubscriptionRepo.Setup(r => r.GetByPlanIdAsync(planId))
                .ReturnsAsync(new List<Subscription>());

            // Act
            var result = await _subscriptionService.GetByPlanIdAsync(planId);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();

            _mockSubscriptionRepo.Verify(r => r.GetByPlanIdAsync(planId), Times.Once);
        }
    }
}