using AutoMapper;
using BusinessLogic.DTOs;
using BusinessLogic.DTOs.SubscriptionPlanDtos;
using BusinessLogic.Services;
using FluentAssertions;
using Moq;
using Repository.Entities;
using Repository.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace QuizUpLearn.Test.UnitTest
{
    public class SubscriptionPlanServiceTest : BaseServiceTest
    {
        private readonly Mock<ISubscriptionPlanRepo> _mockRepo;
        private readonly Mock<IMapper> _mockMapper;
        private readonly SubscriptionPlanService _subscriptionPlanService;

        public SubscriptionPlanServiceTest()
        {
            _mockRepo = new Mock<ISubscriptionPlanRepo>();
            _mockMapper = new Mock<IMapper>();

            _subscriptionPlanService = new SubscriptionPlanService(
                _mockRepo.Object,
                _mockMapper.Object);
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

            var subscriptionPlans = new List<SubscriptionPlan>
            {
                new SubscriptionPlan
                {
                    Id = Guid.NewGuid(),
                    Name = "Basic Plan",
                    Price = 99000,
                    DurationDays = 30,
                    CanAccessPremiumContent = false,
                    CanAccessAiFeatures = false,
                    IsActive = true,
                    IsBuyable = true,
                    CreatedAt = DateTime.UtcNow
                },
                new SubscriptionPlan
                {
                    Id = Guid.NewGuid(),
                    Name = "Premium Plan",
                    Price = 199000,
                    DurationDays = 30,
                    CanAccessPremiumContent = true,
                    CanAccessAiFeatures = true,
                    IsActive = true,
                    IsBuyable = true,
                    CreatedAt = DateTime.UtcNow
                }
            };

            var responseDtos = subscriptionPlans.Select(sp => new ResponseSubscriptionPlanDto
            {
                Id = sp.Id,
                Name = sp.Name,
                Price = sp.Price,
                DurationDays = sp.DurationDays,
                CanAccessPremiumContent = sp.CanAccessPremiumContent,
                CanAccessAiFeatures = sp.CanAccessAiFeatures,
                IsActive = sp.IsActive,
                IsBuyable = sp.IsBuyable,
                CreatedAt = sp.CreatedAt
            }).ToList();

            _mockRepo.Setup(r => r.GetAllAsync())
                .ReturnsAsync(subscriptionPlans);
            _mockMapper.Setup(m => m.Map<List<ResponseSubscriptionPlanDto>>(It.IsAny<IEnumerable<SubscriptionPlan>>()))
                .Returns(responseDtos);

            // Act
            var result = await _subscriptionPlanService.GetAllAsync(pagination);

            // Assert
            result.Should().NotBeNull();
            result.Data.Should().NotBeNull();
            result.Data.Should().HaveCount(2);
            result.Pagination.Should().NotBeNull();
            result.Pagination.CurrentPage.Should().Be(1);
            result.Pagination.PageSize.Should().Be(10);

            _mockRepo.Verify(r => r.GetAllAsync(), Times.Once);
            _mockMapper.Verify(m => m.Map<List<ResponseSubscriptionPlanDto>>(It.IsAny<IEnumerable<SubscriptionPlan>>()), Times.Once);
        }

        [Fact]
        public async Task GetAllAsync_WithNullPagination_ShouldUseDefaultPagination()
        {
            // Arrange
            var subscriptionPlans = new List<SubscriptionPlan>
            {
                new SubscriptionPlan
                {
                    Id = Guid.NewGuid(),
                    Name = "Free Plan",
                    Price = 0,
                    DurationDays = 365,
                    CanAccessPremiumContent = false,
                    CanAccessAiFeatures = false,
                    IsActive = true,
                    IsBuyable = false,
                    CreatedAt = DateTime.UtcNow
                }
            };

            var responseDtos = new List<ResponseSubscriptionPlanDto>
            {
                new ResponseSubscriptionPlanDto
                {
                    Id = subscriptionPlans[0].Id,
                    Name = subscriptionPlans[0].Name,
                    Price = subscriptionPlans[0].Price,
                    DurationDays = subscriptionPlans[0].DurationDays,
                    CanAccessPremiumContent = subscriptionPlans[0].CanAccessPremiumContent,
                    CanAccessAiFeatures = subscriptionPlans[0].CanAccessAiFeatures,
                    IsActive = subscriptionPlans[0].IsActive,
                    IsBuyable = subscriptionPlans[0].IsBuyable,
                    CreatedAt = subscriptionPlans[0].CreatedAt
                }
            };

            _mockRepo.Setup(r => r.GetAllAsync())
                .ReturnsAsync(subscriptionPlans);
            _mockMapper.Setup(m => m.Map<List<ResponseSubscriptionPlanDto>>(It.IsAny<IEnumerable<SubscriptionPlan>>()))
                .Returns(responseDtos);

            // Act
            var result = await _subscriptionPlanService.GetAllAsync(null);

            // Assert
            result.Should().NotBeNull();
            result.Data.Should().NotBeNull();
            result.Data.Should().HaveCount(1);
            result.Pagination.Should().NotBeNull();

            _mockRepo.Verify(r => r.GetAllAsync(), Times.Once);
            _mockMapper.Verify(m => m.Map<List<ResponseSubscriptionPlanDto>>(It.IsAny<IEnumerable<SubscriptionPlan>>()), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_WithValidId_ShouldReturnSubscriptionPlan()
        {
            // Arrange
            var subscriptionPlanId = Guid.NewGuid();
            var subscriptionPlan = new SubscriptionPlan
            {
                Id = subscriptionPlanId,
                Name = "Premium Plan",
                Price = 299000,
                DurationDays = 90,
                CanAccessPremiumContent = true,
                CanAccessAiFeatures = true,
                IsActive = true,
                IsBuyable = true,
                CreatedAt = DateTime.UtcNow
            };

            var responseDto = new ResponseSubscriptionPlanDto
            {
                Id = subscriptionPlan.Id,
                Name = subscriptionPlan.Name,
                Price = subscriptionPlan.Price,
                DurationDays = subscriptionPlan.DurationDays,
                CanAccessPremiumContent = subscriptionPlan.CanAccessPremiumContent,
                CanAccessAiFeatures = subscriptionPlan.CanAccessAiFeatures,
                IsActive = subscriptionPlan.IsActive,
                IsBuyable = subscriptionPlan.IsBuyable,
                CreatedAt = subscriptionPlan.CreatedAt
            };

            _mockRepo.Setup(r => r.GetByIdAsync(subscriptionPlanId))
                .ReturnsAsync(subscriptionPlan);
            _mockMapper.Setup(m => m.Map<ResponseSubscriptionPlanDto>(subscriptionPlan))
                .Returns(responseDto);

            // Act
            var result = await _subscriptionPlanService.GetByIdAsync(subscriptionPlanId);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(subscriptionPlanId);
            result.Name.Should().Be("Premium Plan");
            result.Price.Should().Be(299000);
            result.DurationDays.Should().Be(90);
            result.CanAccessPremiumContent.Should().BeTrue();
            result.CanAccessAiFeatures.Should().BeTrue();
            result.IsActive.Should().BeTrue();
            result.IsBuyable.Should().BeTrue();

            _mockRepo.Verify(r => r.GetByIdAsync(subscriptionPlanId), Times.Once);
            _mockMapper.Verify(m => m.Map<ResponseSubscriptionPlanDto>(subscriptionPlan), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_WithNonExistentId_ShouldReturnNull()
        {
            // Arrange
            var subscriptionPlanId = Guid.NewGuid();

            _mockRepo.Setup(r => r.GetByIdAsync(subscriptionPlanId))
                .ReturnsAsync((SubscriptionPlan?)null);

            // Act
            var result = await _subscriptionPlanService.GetByIdAsync(subscriptionPlanId);

            // Assert
            result.Should().BeNull();

            _mockRepo.Verify(r => r.GetByIdAsync(subscriptionPlanId), Times.Once);
            _mockMapper.Verify(m => m.Map<ResponseSubscriptionPlanDto>(It.IsAny<SubscriptionPlan>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_WithValidDto_ShouldReturnCreatedSubscriptionPlan()
        {
            // Arrange
            var requestDto = new RequestSubscriptionPlanDto
            {
                Name = "Enterprise Plan",
                Price = 499000,
                DurationDays = 365,
                CanAccessPremiumContent = true,
                CanAccessAiFeatures = true,
                IsActive = true,
                IsBuyable = true
            };

            var subscriptionPlan = new SubscriptionPlan
            {
                Name = requestDto.Name,
                Price = requestDto.Price,
                DurationDays = requestDto.DurationDays,
                CanAccessPremiumContent = requestDto.CanAccessPremiumContent,
                CanAccessAiFeatures = requestDto.CanAccessAiFeatures,
                IsActive = requestDto.IsActive,
                IsBuyable = requestDto.IsBuyable
            };

            var createdSubscriptionPlan = new SubscriptionPlan
            {
                Id = Guid.NewGuid(),
                Name = subscriptionPlan.Name,
                Price = subscriptionPlan.Price,
                DurationDays = subscriptionPlan.DurationDays,
                CanAccessPremiumContent = subscriptionPlan.CanAccessPremiumContent,
                CanAccessAiFeatures = subscriptionPlan.CanAccessAiFeatures,
                IsActive = subscriptionPlan.IsActive,
                IsBuyable = subscriptionPlan.IsBuyable,
                CreatedAt = DateTime.UtcNow
            };

            var responseDto = new ResponseSubscriptionPlanDto
            {
                Id = createdSubscriptionPlan.Id,
                Name = createdSubscriptionPlan.Name,
                Price = createdSubscriptionPlan.Price,
                DurationDays = createdSubscriptionPlan.DurationDays,
                CanAccessPremiumContent = createdSubscriptionPlan.CanAccessPremiumContent,
                CanAccessAiFeatures = createdSubscriptionPlan.CanAccessAiFeatures,
                IsActive = createdSubscriptionPlan.IsActive,
                IsBuyable = createdSubscriptionPlan.IsBuyable,
                CreatedAt = createdSubscriptionPlan.CreatedAt
            };

            _mockMapper.Setup(m => m.Map<SubscriptionPlan>(requestDto))
                .Returns(subscriptionPlan);
            _mockRepo.Setup(r => r.CreateAsync(subscriptionPlan))
                .ReturnsAsync(createdSubscriptionPlan);
            _mockMapper.Setup(m => m.Map<ResponseSubscriptionPlanDto>(createdSubscriptionPlan))
                .Returns(responseDto);

            // Act
            var result = await _subscriptionPlanService.CreateAsync(requestDto);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(createdSubscriptionPlan.Id);
            result.Name.Should().Be("Enterprise Plan");
            result.Price.Should().Be(499000);
            result.DurationDays.Should().Be(365);
            result.CanAccessPremiumContent.Should().BeTrue();
            result.CanAccessAiFeatures.Should().BeTrue();
            result.IsActive.Should().BeTrue();
            result.IsBuyable.Should().BeTrue();

            _mockMapper.Verify(m => m.Map<SubscriptionPlan>(requestDto), Times.Once);
            _mockRepo.Verify(r => r.CreateAsync(subscriptionPlan), Times.Once);
            _mockMapper.Verify(m => m.Map<ResponseSubscriptionPlanDto>(createdSubscriptionPlan), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_WithValidIdAndDto_ShouldReturnUpdatedSubscriptionPlan()
        {
            // Arrange
            var subscriptionPlanId = Guid.NewGuid();
            var requestDto = new RequestSubscriptionPlanDto
            {
                Name = "Updated Premium Plan",
                Price = 250000,
                DurationDays = 60,
                CanAccessPremiumContent = true,
                CanAccessAiFeatures = false,
                IsActive = true,
                IsBuyable = true
            };

            var existingSubscriptionPlan = new SubscriptionPlan
            {
                Id = subscriptionPlanId,
                Name = "Premium Plan",
                Price = 199000,
                DurationDays = 30,
                CanAccessPremiumContent = false,
                CanAccessAiFeatures = false,
                IsActive = true,
                IsBuyable = true,
                CreatedAt = DateTime.UtcNow.AddDays(-10)
            };

            var updatedSubscriptionPlan = new SubscriptionPlan
            {
                Id = subscriptionPlanId,
                Name = requestDto.Name,
                Price = requestDto.Price,
                DurationDays = requestDto.DurationDays,
                CanAccessPremiumContent = requestDto.CanAccessPremiumContent,
                CanAccessAiFeatures = requestDto.CanAccessAiFeatures,
                IsActive = requestDto.IsActive,
                IsBuyable = requestDto.IsBuyable,
                CreatedAt = existingSubscriptionPlan.CreatedAt,
                UpdatedAt = DateTime.UtcNow
            };

            var responseDto = new ResponseSubscriptionPlanDto
            {
                Id = updatedSubscriptionPlan.Id,
                Name = updatedSubscriptionPlan.Name,
                Price = updatedSubscriptionPlan.Price,
                DurationDays = updatedSubscriptionPlan.DurationDays,
                CanAccessPremiumContent = updatedSubscriptionPlan.CanAccessPremiumContent,
                CanAccessAiFeatures = updatedSubscriptionPlan.CanAccessAiFeatures,
                IsActive = updatedSubscriptionPlan.IsActive,
                IsBuyable = updatedSubscriptionPlan.IsBuyable,
                CreatedAt = updatedSubscriptionPlan.CreatedAt,
                UpdatedAt = updatedSubscriptionPlan.UpdatedAt
            };

            _mockRepo.Setup(r => r.GetByIdAsync(subscriptionPlanId))
                .ReturnsAsync(existingSubscriptionPlan);
            _mockRepo.Setup(r => r.UpdateAsync(subscriptionPlanId, It.IsAny<SubscriptionPlan>()))
                .ReturnsAsync(updatedSubscriptionPlan);
            _mockMapper.Setup(m => m.Map<ResponseSubscriptionPlanDto>(updatedSubscriptionPlan))
                .Returns(responseDto);

            // Act
            var result = await _subscriptionPlanService.UpdateAsync(subscriptionPlanId, requestDto);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(subscriptionPlanId);
            result.Name.Should().Be("Updated Premium Plan");
            result.Price.Should().Be(250000);
            result.DurationDays.Should().Be(60);
            result.CanAccessPremiumContent.Should().BeTrue();
            result.CanAccessAiFeatures.Should().BeFalse();
            result.IsActive.Should().BeTrue();
            result.IsBuyable.Should().BeTrue();
            result.UpdatedAt.Should().NotBeNull();

            _mockRepo.Verify(r => r.GetByIdAsync(subscriptionPlanId), Times.Once);
            _mockRepo.Verify(r => r.UpdateAsync(subscriptionPlanId, It.IsAny<SubscriptionPlan>()), Times.Once);
            _mockMapper.Verify(m => m.Map<ResponseSubscriptionPlanDto>(updatedSubscriptionPlan), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_WithNonExistentId_ShouldReturnNull()
        {
            // Arrange
            var subscriptionPlanId = Guid.NewGuid();
            var requestDto = new RequestSubscriptionPlanDto
            {
                Name = "Updated Plan",
                Price = 150000,
                DurationDays = 30,
                CanAccessPremiumContent = true,
                CanAccessAiFeatures = true,
                IsActive = true,
                IsBuyable = true
            };

            _mockRepo.Setup(r => r.GetByIdAsync(subscriptionPlanId))
                .ReturnsAsync((SubscriptionPlan?)null);

            // Act
            var result = await _subscriptionPlanService.UpdateAsync(subscriptionPlanId, requestDto);

            // Assert
            result.Should().BeNull();

            _mockRepo.Verify(r => r.GetByIdAsync(subscriptionPlanId), Times.Once);
            _mockRepo.Verify(r => r.UpdateAsync(It.IsAny<Guid>(), It.IsAny<SubscriptionPlan>()), Times.Never);
            _mockMapper.Verify(m => m.Map<ResponseSubscriptionPlanDto>(It.IsAny<SubscriptionPlan>()), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_WithValidId_ShouldReturnTrue()
        {
            // Arrange
            var subscriptionPlanId = Guid.NewGuid();

            _mockRepo.Setup(r => r.DeleteAsync(subscriptionPlanId))
                .ReturnsAsync(true);

            // Act
            var result = await _subscriptionPlanService.DeleteAsync(subscriptionPlanId);

            // Assert
            result.Should().BeTrue();

            _mockRepo.Verify(r => r.DeleteAsync(subscriptionPlanId), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_WithNonExistentId_ShouldReturnFalse()
        {
            // Arrange
            var subscriptionPlanId = Guid.NewGuid();

            _mockRepo.Setup(r => r.DeleteAsync(subscriptionPlanId))
                .ReturnsAsync(false);

            // Act
            var result = await _subscriptionPlanService.DeleteAsync(subscriptionPlanId);

            // Assert
            result.Should().BeFalse();

            _mockRepo.Verify(r => r.DeleteAsync(subscriptionPlanId), Times.Once);
        }

        [Fact]
        public async Task GetFreeSubscriptionPlanAsync_ShouldReturnFreeSubscriptionPlan()
        {
            // Arrange
            var freeSubscriptionPlan = new SubscriptionPlan
            {
                Id = Guid.NewGuid(),
                Name = "Free Plan",
                Price = 0,
                DurationDays = 365,
                CanAccessPremiumContent = false,
                CanAccessAiFeatures = false,
                IsActive = true,
                IsBuyable = false,
                CreatedAt = DateTime.UtcNow
            };

            var responseDto = new ResponseSubscriptionPlanDto
            {
                Id = freeSubscriptionPlan.Id,
                Name = freeSubscriptionPlan.Name,
                Price = freeSubscriptionPlan.Price,
                DurationDays = freeSubscriptionPlan.DurationDays,
                CanAccessPremiumContent = freeSubscriptionPlan.CanAccessPremiumContent,
                CanAccessAiFeatures = freeSubscriptionPlan.CanAccessAiFeatures,
                IsActive = freeSubscriptionPlan.IsActive,
                IsBuyable = freeSubscriptionPlan.IsBuyable,
                CreatedAt = freeSubscriptionPlan.CreatedAt
            };

            _mockRepo.Setup(r => r.GetFreeSubscriptionPlan())
                .ReturnsAsync(freeSubscriptionPlan);
            _mockMapper.Setup(m => m.Map<ResponseSubscriptionPlanDto>(freeSubscriptionPlan))
                .Returns(responseDto);

            // Act
            var result = await _subscriptionPlanService.GetFreeSubscriptionPlanAsync();

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(freeSubscriptionPlan.Id);
            result.Name.Should().Be("Free Plan");
            result.Price.Should().Be(0);
            result.DurationDays.Should().Be(365);
            result.CanAccessPremiumContent.Should().BeFalse();
            result.CanAccessAiFeatures.Should().BeFalse();
            result.IsActive.Should().BeTrue();
            result.IsBuyable.Should().BeFalse();

            _mockRepo.Verify(r => r.GetFreeSubscriptionPlan(), Times.Once);
            _mockMapper.Verify(m => m.Map<ResponseSubscriptionPlanDto>(freeSubscriptionPlan), Times.Once);
        }

        [Fact]
        public async Task GetAllAsync_WithInvalidPagination_ShouldThrowArgumentException()
        {
            // Arrange
            var pagination = new PaginationRequestDto
            {
                Page = -1,
                PageSize = 200
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await _subscriptionPlanService.GetAllAsync(pagination));
        }

        [Fact]
        public async Task GetByIdAsync_WithEmptyGuid_ShouldThrowArgumentException()
        {
            // Arrange
            var invalidId = Guid.Empty;

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await _subscriptionPlanService.GetByIdAsync(invalidId));
        }

        [Fact]
        public async Task CreateAsync_WithInvalidPrice_ShouldThrowValidationException()
        {
            // Arrange
            var invalidDto = new RequestSubscriptionPlanDto
            {
                Name = "plan test name",
                Price = -1000,
                DurationDays = 30
            };

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(async () =>
                await _subscriptionPlanService.CreateAsync(invalidDto));
        }
        [Fact]
        public async Task CreateAsync_WithInvalidDurationDays_ShouldThrowValidationException()
        {
            // Arrange
            var invalidDto = new RequestSubscriptionPlanDto
            {
                Name = "plan test name",
                Price = 10000,
                DurationDays = -1
            };

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(async () =>
                await _subscriptionPlanService.CreateAsync(invalidDto));
        }
        [Fact]
        public async Task CreateAsync_WithNullName_ShouldThrowValidationException()
        {
            // Arrange
            var invalidDto = new RequestSubscriptionPlanDto
            {
                Name = null,
                Price = 10000,
                DurationDays = 10
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await _subscriptionPlanService.CreateAsync(invalidDto));
        }
        [Fact]
        public async Task CreateAsync_WithNullDto_ShouldThrowValidationException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await _subscriptionPlanService.CreateAsync(null!));
        }
        [Fact]
        public async Task UpdateAsync_WithInvalidDto_ShouldThrowValidationException()
        {
            // Arrange
            var subscriptionPlanId = Guid.NewGuid();
            var invalidDto = new RequestSubscriptionPlanDto
            {
                Name = "",
                Price = -500,
                DurationDays = -10
            };

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(async () =>
                await _subscriptionPlanService.UpdateAsync(subscriptionPlanId, invalidDto));
        }

        [Fact]
        public async Task DeleteAsync_WithEmptyGuid_ShouldThrowArgumentException()
        {
            // Arrange
            var invalidId = Guid.Empty;

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await _subscriptionPlanService.DeleteAsync(invalidId));
        }
    }
}