using AutoMapper;
using BusinessLogic.DTOs;
using BusinessLogic.Interfaces;
using BusinessLogic.MappingProfile;
using BusinessLogic.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Repository.Entities;
using Repository.Interfaces;
using BCrypt.Net;

namespace QuizUpLearn.Test.UnitTest
{
    public class AccountServiceTest : BaseServiceTest
    {
        private readonly Mock<IAccountRepo> _mockAccountRepo;
        private readonly IMapper _mapper;
        private readonly AccountService _accountService;

        public AccountServiceTest()
        {
            _mockAccountRepo = new Mock<IAccountRepo>();

            // Setup real AutoMapper with the actual mapping profile
            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<MappingProfile>();
            }, new NullLoggerFactory());
            _mapper = mapperConfig.CreateMapper();

            _accountService = new AccountService(
                _mockAccountRepo.Object,
                _mapper);
        }

        [Fact]
        public async Task CreateAsync_WithValidData_ShouldReturnResponseAccountDto()
        {
            // Arrange
            var requestDto = new RequestAccountDto
            {
                Email = "test@example.com",
                Password = "plain_password"
            };

            var createdAccount = new Account
            {
                Id = Guid.NewGuid(),
                Email = requestDto.Email,
                PasswordHash = BCrypt.HashPassword(requestDto.Password),
                UserId = Guid.NewGuid(),
                RoleId = Guid.NewGuid(),
                IsEmailVerified = false,
                IsActive = true,
                IsBanned = false,
                LoginAttempts = 0,
                CreatedAt = DateTime.UtcNow
            };

            _mockAccountRepo.Setup(r => r.CreateAsync(It.IsAny<Account>()))
                .ReturnsAsync(createdAccount);

            // Act
            var result = await _accountService.CreateAsync(requestDto);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(createdAccount.Id);
            result.Email.Should().Be(requestDto.Email);

            _mockAccountRepo.Verify(r => r.CreateAsync(It.Is<Account>(a =>
                a.Email == requestDto.Email &&
                !string.IsNullOrEmpty(a.PasswordHash) &&
                BCrypt.Verify(requestDto.Password, a.PasswordHash))), Times.Once);
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllAccounts()
        {
            // Arrange
            var accounts = new List<Account>
            {
                new Account
                {
                    Id = Guid.NewGuid(),
                    Email = "user1@example.com",
                    PasswordHash = "hash1",
                    UserId = Guid.NewGuid(),
                    RoleId = Guid.NewGuid(),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new Account
                {
                    Id = Guid.NewGuid(),
                    Email = "user2@example.com",
                    PasswordHash = "hash2",
                    UserId = Guid.NewGuid(),
                    RoleId = Guid.NewGuid(),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                }
            };

            _mockAccountRepo.Setup(r => r.GetAllAsync(false))
                .ReturnsAsync(accounts);

            // Act
            var result = await _accountService.GetAllAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            _mockAccountRepo.Verify(r => r.GetAllAsync(false), Times.Once);
        }

        [Fact]
        public async Task GetAllAsync_WithIncludeDeleted_ShouldReturnAllAccountsIncludingDeleted()
        {
            // Arrange
            var accounts = new List<Account>
            {
                new Account
                {
                    Id = Guid.NewGuid(),
                    Email = "user1@example.com",
                    PasswordHash = "hash1",
                    UserId = Guid.NewGuid(),
                    RoleId = Guid.NewGuid(),
                    CreatedAt = DateTime.UtcNow,
                    DeletedAt = DateTime.UtcNow
                }
            };

            _mockAccountRepo.Setup(r => r.GetAllAsync(true))
                .ReturnsAsync(accounts);

            // Act
            var result = await _accountService.GetAllAsync(true);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            _mockAccountRepo.Verify(r => r.GetAllAsync(true), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_WithValidId_ShouldReturnResponseAccountDto()
        {
            // Arrange
            var accountId = Guid.NewGuid();
            var account = new Account
            {
                Id = accountId,
                Email = "test@example.com",
                PasswordHash = "hashed_password",
                UserId = Guid.NewGuid(),
                RoleId = Guid.NewGuid(),
                IsEmailVerified = true,
                IsActive = true,
                IsBanned = false,
                LoginAttempts = 0,
                LastLoginAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            _mockAccountRepo.Setup(r => r.GetByIdAsync(accountId))
                .ReturnsAsync(account);

            // Act
            var result = await _accountService.GetByIdAsync(accountId);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(accountId);
            result.Email.Should().Be(account.Email);
            result.IsEmailVerified.Should().Be(account.IsEmailVerified);
            result.IsActive.Should().Be(account.IsActive);

            _mockAccountRepo.Verify(r => r.GetByIdAsync(accountId), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_WithNonExistentId_ShouldReturnNull()
        {
            // Arrange
            var accountId = Guid.NewGuid();
            _mockAccountRepo.Setup(r => r.GetByIdAsync(accountId))
                .ReturnsAsync((Account?)null);

            // Act
            var result = await _accountService.GetByIdAsync(accountId);

            // Assert
            result.Should().BeNull();
            _mockAccountRepo.Verify(r => r.GetByIdAsync(accountId), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_WithValidData_ShouldReturnUpdatedResponseAccountDto()
        {
            // Arrange
            var accountId = Guid.NewGuid();
            var requestDto = new RequestAccountDto
            {
                Email = "updated@example.com",
                Password = "new_password"
            };

            var existingAccount = new Account
            {
                Id = accountId,
                Email = "old@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("old_password"),
                UserId = Guid.NewGuid(),
                RoleId = Guid.NewGuid(),
                IsEmailVerified = true,
                IsActive = true,
                IsBanned = false,
                LoginAttempts = 0,
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                UpdatedAt = DateTime.UtcNow
            };

            var updatedAccount = new Account
            {
                Id = accountId,
                Email = requestDto.Email,
                PasswordHash = BCrypt.HashPassword(requestDto.Password),
                UserId = existingAccount.UserId,
                RoleId = existingAccount.RoleId,
                IsEmailVerified = true,
                IsActive = true,
                IsBanned = false,
                LoginAttempts = 0,
                CreatedAt = existingAccount.CreatedAt,
                UpdatedAt = DateTime.UtcNow
            };

            _mockAccountRepo.Setup(r => r.GetByIdAsync(accountId))
                .ReturnsAsync(existingAccount);
            _mockAccountRepo.Setup(r => r.UpdateAsync(accountId, It.IsAny<Account>()))
                .ReturnsAsync(updatedAccount);

            // Act
            var result = await _accountService.UpdateAsync(accountId, requestDto);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(accountId);
            result.Email.Should().Be(requestDto.Email);

            _mockAccountRepo.Verify(r => r.UpdateAsync(accountId, It.Is<Account>(a =>
                a.Email == requestDto.Email &&
                !string.IsNullOrEmpty(a.PasswordHash) &&
                BCrypt.Verify(requestDto.Password, a.PasswordHash))), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_WithNonExistentId_ShouldReturnNull()
        {
            // Arrange
            var accountId = Guid.NewGuid();
            var requestDto = new RequestAccountDto
            {
                Email = "test@example.com",
                Password = "password"
            };

            _mockAccountRepo.Setup(r => r.GetByIdAsync(accountId))
                .ReturnsAsync((Account?)null);

            // Act
            var result = await _accountService.UpdateAsync(accountId, requestDto);

            // Assert
            result.Should().BeNull();
            _mockAccountRepo.Verify(r => r.GetByIdAsync(accountId), Times.Once);
            _mockAccountRepo.Verify(r => r.UpdateAsync(It.IsAny<Guid>(), It.IsAny<Account>()), Times.Never);
        }

        [Fact]
        public async Task SoftDeleteAsync_WithValidId_ShouldReturnTrue()
        {
            // Arrange
            var accountId = Guid.NewGuid();
            _mockAccountRepo.Setup(r => r.SoftDeleteAsync(accountId))
                .ReturnsAsync(true);

            // Act
            var result = await _accountService.SoftDeleteAsync(accountId);

            // Assert
            result.Should().BeTrue();
            _mockAccountRepo.Verify(r => r.SoftDeleteAsync(accountId), Times.Once);
        }

        [Fact]
        public async Task RestoreAsync_WithValidId_ShouldReturnTrue()
        {
            // Arrange
            var accountId = Guid.NewGuid();
            _mockAccountRepo.Setup(r => r.RestoreAsync(accountId))
                .ReturnsAsync(true);

            // Act
            var result = await _accountService.RestoreAsync(accountId);

            // Assert
            result.Should().BeTrue();
            _mockAccountRepo.Verify(r => r.RestoreAsync(accountId), Times.Once);
        }
    }
}

