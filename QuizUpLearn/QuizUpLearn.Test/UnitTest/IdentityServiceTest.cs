using AutoMapper;
using BusinessLogic.DTOs;
using BusinessLogic.DTOs.SubscriptionPlanDtos;
using BusinessLogic.Interfaces;
using BusinessLogic.MappingProfile;
using BusinessLogic.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Repository.Entities;
using Repository.Interfaces;
using Repository.Models;

namespace QuizUpLearn.Test.UnitTest
{
    public class IdentityServiceTest : BaseServiceTest
    {
        private readonly Mock<IAccountRepo> _mockAccountRepo;
        private readonly Mock<IUserRepo> _mockUserRepo;
        private readonly Mock<IOtpVerificationRepo> _mockOtpRepo;
        private readonly Mock<IMailerSendService> _mockMailer;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<ISubscriptionService> _mockSubscriptionService;
        private readonly Mock<ISubscriptionPlanService> _mockSubscriptionPlanService;
        private readonly IMapper _mapper;
        private readonly IdentityService _identityService;

        public IdentityServiceTest()
        {
            _mockAccountRepo = new Mock<IAccountRepo>();
            _mockUserRepo = new Mock<IUserRepo>();
            _mockOtpRepo = new Mock<IOtpVerificationRepo>();
            _mockMailer = new Mock<IMailerSendService>();
            _mockConfiguration = new Mock<IConfiguration>();
            _mockSubscriptionService = new Mock<ISubscriptionService>();
            _mockSubscriptionPlanService = new Mock<ISubscriptionPlanService>();

            // Setup real AutoMapper with the actual mapping profile
            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<MappingProfile>();
            }, new NullLoggerFactory());
            _mapper = mapperConfig.CreateMapper();

            // Setup default configuration
            _mockConfiguration.Setup(c => c["MailerSend:FromEmail"])
                .Returns("test@example.com");
            _mockConfiguration.Setup(c => c["MailerSend:FromName"])
                .Returns("QuizUpLearn");

            _identityService = new IdentityService(
                _mockAccountRepo.Object,
                _mockUserRepo.Object,
                _mockOtpRepo.Object,
                _mockMailer.Object,
                _mockConfiguration.Object,
                _mapper,
                _mockSubscriptionService.Object,
                _mockSubscriptionPlanService.Object);
        }

        [Fact]
        public async Task RegisterAsync_WithValidData_ShouldReturnResponseAccountDto()
        {
            // Arrange
            var registerDto = new RegisterRequestDto
            {
                Email = "test@example.com",
                Password = "Password123!"
            };

            var createdAccount = new Account
            {
                Id = Guid.NewGuid(),
                Email = registerDto.Email.ToLowerInvariant().Trim(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password),
                UserId = Guid.NewGuid(),
                RoleId = Guid.NewGuid(),
                IsActive = true,
                IsEmailVerified = false,
                CreatedAt = DateTime.UtcNow
            };

            var freePlan = new BusinessLogic.DTOs.SubscriptionPlanDtos.ResponseSubscriptionPlanDto
            {
                Id = Guid.NewGuid(),
                DurationDays = 30
            };

            _mockAccountRepo.Setup(r => r.GetByEmailAsync(registerDto.Email.ToLowerInvariant().Trim()))
                .ReturnsAsync((Account?)null);
            _mockAccountRepo.Setup(r => r.CreateAsync(It.IsAny<Account>()))
                .ReturnsAsync(createdAccount);
            _mockSubscriptionPlanService.Setup(s => s.GetFreeSubscriptionPlanAsync())
                .ReturnsAsync(freePlan);
            _mockSubscriptionService.Setup(s => s.CreateAsync(It.IsAny<BusinessLogic.DTOs.SubscriptionDtos.RequestSubscriptionDto>()))
                .ReturnsAsync(new BusinessLogic.DTOs.SubscriptionDtos.ResponseSubscriptionDto());

            // Act
            var result = await _identityService.RegisterAsync(registerDto);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(createdAccount.Id);
            result.Email.Should().Be(createdAccount.Email);

            _mockAccountRepo.Verify(r => r.GetByEmailAsync(registerDto.Email.ToLowerInvariant().Trim()), Times.Once);
            _mockAccountRepo.Verify(r => r.CreateAsync(It.Is<Account>(a =>
                a.Email == registerDto.Email.ToLowerInvariant().Trim())), Times.Once);
        }

        [Fact]
        public async Task RegisterAsync_WithExistingEmail_ShouldThrowException()
        {
            // Arrange
            var registerDto = new RegisterRequestDto
            {
                Email = "existing@example.com",
                Password = "Password123!"
            };

            var existingAccount = new Account
            {
                Id = Guid.NewGuid(),
                Email = registerDto.Email.ToLowerInvariant().Trim(),
                PasswordHash = "existing_hash"
            };

            _mockAccountRepo.Setup(r => r.GetByEmailAsync(registerDto.Email.ToLowerInvariant().Trim()))
                .ReturnsAsync(existingAccount);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await _identityService.RegisterAsync(registerDto));

            _mockAccountRepo.Verify(r => r.CreateAsync(It.IsAny<Account>()), Times.Never);
        }

        [Fact]
        public async Task LoginAsync_WithValidCredentials_ShouldReturnLoginResponse()
        {
            // Arrange
            var loginDto = new LoginRequestDto
            {
                Email = "test@example.com",
                Password = "Password123!"
            };

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(loginDto.Password);
            var account = new Account
            {
                Id = Guid.NewGuid(),
                Email = loginDto.Email.ToLowerInvariant().Trim(),
                PasswordHash = passwordHash,
                UserId = Guid.NewGuid(),
                RoleId = Guid.NewGuid(),
                IsActive = true,
                IsEmailVerified = true,
                CreatedAt = DateTime.UtcNow
            };

            _mockAccountRepo.Setup(r => r.GetByEmailAsync(loginDto.Email.ToLowerInvariant().Trim()))
                .ReturnsAsync(account);
            _mockAccountRepo.Setup(r => r.UpdateAsync(account.Id, It.IsAny<Account>()))
                .ReturnsAsync((Guid id, Account a) => a);

            // Act
            var result = await _identityService.LoginAsync(loginDto);

            // Assert
            result.Should().NotBeNull();
            result!.Account.Should().NotBeNull();
            result.Account.Id.Should().Be(account.Id);
            result.Account.Email.Should().Be(account.Email);

            _mockAccountRepo.Verify(r => r.GetByEmailAsync(loginDto.Email.ToLowerInvariant().Trim()), Times.Once);
            _mockAccountRepo.Verify(r => r.UpdateAsync(account.Id, It.IsAny<Account>()), Times.Once);
        }

        [Fact]
        public async Task LoginAsync_WithInvalidEmail_ShouldReturnNull()
        {
            // Arrange
            var loginDto = new LoginRequestDto
            {
                Email = "nonexistent@example.com",
                Password = "Password123!"
            };

            _mockAccountRepo.Setup(r => r.GetByEmailAsync(loginDto.Email.ToLowerInvariant().Trim()))
                .ReturnsAsync((Account?)null);

            // Act
            var result = await _identityService.LoginAsync(loginDto);

            // Assert
            result.Should().BeNull();
            _mockAccountRepo.Verify(r => r.UpdateAsync(It.IsAny<Guid>(), It.IsAny<Account>()), Times.Never);
        }

        [Fact]
        public async Task LoginAsync_WithInvalidPassword_ShouldReturnNull()
        {
            // Arrange
            var loginDto = new LoginRequestDto
            {
                Email = "test@example.com",
                Password = "WrongPassword"
            };

            var account = new Account
            {
                Id = Guid.NewGuid(),
                Email = loginDto.Email.ToLowerInvariant().Trim(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword"),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _mockAccountRepo.Setup(r => r.GetByEmailAsync(loginDto.Email.ToLowerInvariant().Trim()))
                .ReturnsAsync(account);

            // Act
            var result = await _identityService.LoginAsync(loginDto);

            // Assert
            result.Should().BeNull();
            _mockAccountRepo.Verify(r => r.UpdateAsync(It.IsAny<Guid>(), It.IsAny<Account>()), Times.Never);
        }

        [Fact]
        public async Task LoginAsync_WithInactiveAccount_ShouldReturnNull()
        {
            // Arrange
            var loginDto = new LoginRequestDto
            {
                Email = "test@example.com",
                Password = "Password123!"
            };

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(loginDto.Password);
            var account = new Account
            {
                Id = Guid.NewGuid(),
                Email = loginDto.Email.ToLowerInvariant().Trim(),
                PasswordHash = passwordHash,
                IsActive = false, // Inactive
                CreatedAt = DateTime.UtcNow
            };

            _mockAccountRepo.Setup(r => r.GetByEmailAsync(loginDto.Email.ToLowerInvariant().Trim()))
                .ReturnsAsync(account);

            // Act
            var result = await _identityService.LoginAsync(loginDto);

            // Assert
            result.Should().BeNull();
            _mockAccountRepo.Verify(r => r.UpdateAsync(It.IsAny<Guid>(), It.IsAny<Account>()), Times.Never);
        }

        [Fact]
        public async Task LoginAsync_WithDeletedAccount_ShouldReturnNull()
        {
            // Arrange
            var loginDto = new LoginRequestDto
            {
                Email = "test@example.com",
                Password = "Password123!"
            };

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(loginDto.Password);
            var account = new Account
            {
                Id = Guid.NewGuid(),
                Email = loginDto.Email.ToLowerInvariant().Trim(),
                PasswordHash = passwordHash,
                IsActive = true,
                DeletedAt = DateTime.UtcNow, // Deleted
                CreatedAt = DateTime.UtcNow
            };

            _mockAccountRepo.Setup(r => r.GetByEmailAsync(loginDto.Email.ToLowerInvariant().Trim()))
                .ReturnsAsync(account);

            // Act
            var result = await _identityService.LoginAsync(loginDto);

            // Assert
            result.Should().BeNull();
            _mockAccountRepo.Verify(r => r.UpdateAsync(It.IsAny<Guid>(), It.IsAny<Account>()), Times.Never);
        }

        [Fact]
        public async Task InitiateResetPasswordAsync_WithValidEmail_ShouldReturnTrue()
        {
            // Arrange
            var dto = new ResetPasswordInitiateRequestDto
            {
                Email = "test@example.com"
            };

            var account = new Account
            {
                Id = Guid.NewGuid(),
                Email = dto.Email.ToLowerInvariant().Trim(),
                PasswordHash = "dummy_hash",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _mockAccountRepo.Setup(r => r.GetByEmailAsync(dto.Email.ToLowerInvariant().Trim()))
                .ReturnsAsync(account);
            _mockOtpRepo.Setup(r => r.InvalidateAllActiveOtpsAsync(dto.Email.ToLowerInvariant().Trim(), "RESET_PASSWORD"))
                .Returns(Task.CompletedTask);
            _mockOtpRepo.Setup(r => r.CreateAsync(It.IsAny<OtpVerification>()))
                .ReturnsAsync((OtpVerification o) => o);
            _mockMailer.Setup(m => m.SendEmailAsync(It.IsAny<Repository.Models.MailerSendEmail>()))
                .ReturnsAsync((object?)null);

            // Act
            var result = await _identityService.InitiateResetPasswordAsync(dto);

            // Assert
            result.Should().BeTrue();
            _mockOtpRepo.Verify(r => r.InvalidateAllActiveOtpsAsync(dto.Email.ToLowerInvariant().Trim(), "RESET_PASSWORD"), Times.Once);
            _mockOtpRepo.Verify(r => r.CreateAsync(It.Is<OtpVerification>(o =>
                o.Email == dto.Email.ToLowerInvariant().Trim() &&
                o.Purpose == "RESET_PASSWORD" &&
                !o.IsUsed)), Times.Once);
        }

        [Fact]
        public async Task InitiateResetPasswordAsync_WithNonExistentEmail_ShouldReturnTrue()
        {
            // Arrange
            var dto = new ResetPasswordInitiateRequestDto
            {
                Email = "nonexistent@example.com"
            };

            _mockAccountRepo.Setup(r => r.GetByEmailAsync(dto.Email.ToLowerInvariant().Trim()))
                .ReturnsAsync((Account?)null);

            // Act
            var result = await _identityService.InitiateResetPasswordAsync(dto);

            // Assert
            result.Should().BeTrue(); // Returns true even if email doesn't exist (security)
            _mockOtpRepo.Verify(r => r.CreateAsync(It.IsAny<OtpVerification>()), Times.Never);
        }

        [Fact]
        public async Task VerifyResetPasswordOtpAsync_WithValidOtp_ShouldReturnTrue()
        {
            // Arrange
            var dto = new ResetPasswordVerifyRequestDto
            {
                Email = "test@example.com",
                OtpCode = "123456"
            };

            var otp = new OtpVerification
            {
                Id = Guid.NewGuid(),
                Email = dto.Email.ToLowerInvariant().Trim(),
                OTPCode = dto.OtpCode.Trim(),
                Purpose = "RESET_PASSWORD",
                IsUsed = false,
                ExpiresAt = DateTime.UtcNow.AddMinutes(10)
            };

            _mockOtpRepo.Setup(r => r.GetValidOtpAsync(
                dto.Email.ToLowerInvariant().Trim(),
                "RESET_PASSWORD",
                dto.OtpCode.Trim()))
                .ReturnsAsync(otp);

            // Act
            var result = await _identityService.VerifyResetPasswordOtpAsync(dto);

            // Assert
            result.Should().BeTrue();
            _mockOtpRepo.Verify(r => r.GetValidOtpAsync(
                dto.Email.ToLowerInvariant().Trim(),
                "RESET_PASSWORD",
                dto.OtpCode.Trim()), Times.Once);
        }

        [Fact]
        public async Task VerifyResetPasswordOtpAsync_WithInvalidOtp_ShouldReturnFalse()
        {
            // Arrange
            var dto = new ResetPasswordVerifyRequestDto
            {
                Email = "test@example.com",
                OtpCode = "000000"
            };

            _mockOtpRepo.Setup(r => r.GetValidOtpAsync(
                dto.Email.ToLowerInvariant().Trim(),
                "RESET_PASSWORD",
                dto.OtpCode.Trim()))
                .ReturnsAsync((OtpVerification?)null);

            // Act
            var result = await _identityService.VerifyResetPasswordOtpAsync(dto);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task ConfirmResetPasswordAsync_WithValidOtp_ShouldReturnTrue()
        {
            // Arrange
            var dto = new ResetPasswordConfirmRequestDto
            {
                Email = "test@example.com",
                OtpCode = "123456",
                NewPassword = "NewPassword123!"
            };

            var otp = new OtpVerification
            {
                Id = Guid.NewGuid(),
                Email = dto.Email.ToLowerInvariant().Trim(),
                OTPCode = dto.OtpCode.Trim(),
                Purpose = "RESET_PASSWORD",
                IsUsed = false,
                ExpiresAt = DateTime.UtcNow.AddMinutes(10)
            };

            _mockOtpRepo.Setup(r => r.GetValidOtpAsync(
                dto.Email.ToLowerInvariant().Trim(),
                "RESET_PASSWORD",
                dto.OtpCode.Trim()))
                .ReturnsAsync(otp);
            _mockOtpRepo.Setup(r => r.MarkUsedAsync(otp.Id))
                .Returns(Task.CompletedTask);
            _mockAccountRepo.Setup(r => r.UpdatePasswordByEmailAsync(
                dto.Email.ToLowerInvariant().Trim(),
                It.IsAny<string>()))
                .ReturnsAsync(true);

            // Act
            var result = await _identityService.ConfirmResetPasswordAsync(dto);

            // Assert
            result.Should().BeTrue();
            _mockOtpRepo.Verify(r => r.MarkUsedAsync(otp.Id), Times.Once);
            _mockAccountRepo.Verify(r => r.UpdatePasswordByEmailAsync(
                dto.Email.ToLowerInvariant().Trim(),
                It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task ConfirmResetPasswordAsync_WithInvalidOtp_ShouldReturnFalse()
        {
            // Arrange
            var dto = new ResetPasswordConfirmRequestDto
            {
                Email = "test@example.com",
                OtpCode = "000000",
                NewPassword = "NewPassword123!"
            };

            _mockOtpRepo.Setup(r => r.GetValidOtpAsync(
                dto.Email.ToLowerInvariant().Trim(),
                "RESET_PASSWORD",
                dto.OtpCode.Trim()))
                .ReturnsAsync((OtpVerification?)null);

            // Act
            var result = await _identityService.ConfirmResetPasswordAsync(dto);

            // Assert
            result.Should().BeFalse();
            _mockOtpRepo.Verify(r => r.MarkUsedAsync(It.IsAny<Guid>()), Times.Never);
            _mockAccountRepo.Verify(r => r.UpdatePasswordByEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task LoginAsync_ShouldUpdateLastLoginAt()
        {
            // Arrange
            var loginDto = new LoginRequestDto
            {
                Email = "test@example.com",
                Password = "Password123!"
            };

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(loginDto.Password);
            var account = new Account
            {
                Id = Guid.NewGuid(),
                Email = loginDto.Email.ToLowerInvariant().Trim(),
                PasswordHash = passwordHash,
                UserId = Guid.NewGuid(),
                RoleId = Guid.NewGuid(),
                IsActive = true,
                IsEmailVerified = true,
                CreatedAt = DateTime.UtcNow
            };

            _mockAccountRepo.Setup(r => r.GetByEmailAsync(loginDto.Email.ToLowerInvariant().Trim()))
                .ReturnsAsync(account);
            _mockAccountRepo.Setup(r => r.UpdateAsync(account.Id, It.IsAny<Account>()))
                .ReturnsAsync((Guid id, Account a) => a);

            // Act
            var result = await _identityService.LoginAsync(loginDto);

            // Assert
            result.Should().NotBeNull();
            _mockAccountRepo.Verify(r => r.UpdateAsync(account.Id, It.Is<Account>(a =>
                a.LastLoginAt.HasValue)), Times.Once);
        }

        [Fact]
        public async Task RegisterAsync_ShouldCreateFreeSubscription()
        {
            // Arrange
            var registerDto = new RegisterRequestDto
            {
                Email = "test@example.com",
                Password = "Password123!"
            };

            var createdAccount = new Account
            {
                Id = Guid.NewGuid(),
                Email = registerDto.Email.ToLowerInvariant().Trim(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password),
                UserId = Guid.NewGuid(),
                RoleId = Guid.NewGuid(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var freePlan = new BusinessLogic.DTOs.SubscriptionPlanDtos.ResponseSubscriptionPlanDto
            {
                Id = Guid.NewGuid(),
                DurationDays = 30
            };

            _mockAccountRepo.Setup(r => r.GetByEmailAsync(registerDto.Email.ToLowerInvariant().Trim()))
                .ReturnsAsync((Account?)null);
            _mockAccountRepo.Setup(r => r.CreateAsync(It.IsAny<Account>()))
                .ReturnsAsync(createdAccount);
            _mockSubscriptionPlanService.Setup(s => s.GetFreeSubscriptionPlanAsync())
                .ReturnsAsync(freePlan);
            _mockSubscriptionService.Setup(s => s.CreateAsync(It.IsAny<BusinessLogic.DTOs.SubscriptionDtos.RequestSubscriptionDto>()))
                .ReturnsAsync(new BusinessLogic.DTOs.SubscriptionDtos.ResponseSubscriptionDto());

            // Act
            var result = await _identityService.RegisterAsync(registerDto);

            // Assert
            result.Should().NotBeNull();
            _mockSubscriptionPlanService.Verify(s => s.GetFreeSubscriptionPlanAsync(), Times.Once);
            _mockSubscriptionService.Verify(s => s.CreateAsync(It.Is<BusinessLogic.DTOs.SubscriptionDtos.RequestSubscriptionDto>(d =>
                d.UserId == createdAccount.UserId &&
                d.SubscriptionPlanId == freePlan.Id)), Times.Once);
        }
    }
}

